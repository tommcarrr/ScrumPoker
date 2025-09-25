using Azure.Data.Tables;
using Azure;
using ScrumPoker.Domain;

namespace ScrumPoker.Infrastructure.Persistence;

public class TableSessionRepository : ISessionRepository
{
    private readonly ITableClientFactory _factory;
    private const string SessionsTable = "Sessions";
    private const string ParticipantsTable = "Participants";
    private const string WorkItemsTable = "WorkItems";
    private const string EstimatesTable = "Estimates";
    private const int MaxConcurrencyAttempts = 3;

    public TableSessionRepository(ITableClientFactory factory) => _factory = factory;

    public async Task AddAsync(Session session)
    {
        var client = _factory.GetClient(SessionsTable);
        var entity = SessionEntityMapper.ToEntity(session);
        await client.UpsertEntityAsync(entity);
    }

    public async Task<Session?> GetAsync(string code)
    {
        var sessClient = _factory.GetClient(SessionsTable);
        SessionEntity? sessEntity = null;
        try { var resp = await sessClient.GetEntityAsync<SessionEntity>("SESSION", code); sessEntity = resp.Value; }
        catch (RequestFailedException ex) when (ex.Status == 404) { return null; }

        var partClient = _factory.GetClient(ParticipantsTable);
        var participants = new List<Participant>();
        await foreach (var pe in partClient.QueryAsync<ParticipantEntity>(p => p.PartitionKey == code))
        { try { participants.Add(ParticipantEntityMapper.ToDomain(pe)); } catch { } }

        var wiClient = _factory.GetClient(WorkItemsTable);
        var workItems = new Dictionary<Guid, WorkItem>();
        await foreach (var we in wiClient.QueryAsync<WorkItemEntity>(w => w.PartitionKey == code))
        { try { var wi = WorkItemEntityMapper.ToDomain(we, null); workItems[wi.Id] = wi; } catch { } }

        var estClient = _factory.GetClient(EstimatesTable);
        await foreach (var ee in estClient.QueryAsync<EstimateEntity>(e => e.PartitionKey == code))
        {
            try
            {
                var (wid, est) = EstimateEntityMapper.ToDomain(ee);
                if (workItems.TryGetValue(wid, out var wi))
                { wi.AddOrUpdateEstimate(est.ParticipantId, est.Value); }
            }
            catch { }
        }

        var deck = (sessEntity.DeckCsv ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return Session.Hydrate(code, sessEntity.CreatedUtc, deck, participants, workItems.Values);
    }

    public async Task<bool> ExistsAsync(string code)
    {
        var client = _factory.GetClient(SessionsTable);
        try { var resp = await client.GetEntityAsync<SessionEntity>("SESSION", code); return resp.HasValue; }
        catch (RequestFailedException ex) when (ex.Status == 404) { return false; }
    }

    public async Task AddParticipantAsync(string code, Participant participant)
    {
        var client = _factory.GetClient(ParticipantsTable);
        var entity = ParticipantEntityMapper.ToEntity(participant, code);
        await client.UpsertEntityAsync(entity);
    }

    public async Task AddWorkItemAsync(string code, WorkItem workItem)
    {
        var client = _factory.GetClient(WorkItemsTable);
        var entity = WorkItemEntityMapper.ToEntity(workItem, code);
        await client.UpsertEntityAsync(entity);
    }

    public async Task UpsertEstimateAsync(string code, Guid workItemId, Estimate estimate)
    {
        var client = _factory.GetClient(EstimatesTable);
        var rowKey = workItemId + ":" + estimate.ParticipantId;
        await ExecuteWithRetryAsync(async attempt =>
        {
            var entity = EstimateEntityMapper.ToEntity(workItemId, estimate, code);
            try { await client.AddEntityAsync(entity); return; }
            catch (RequestFailedException ex) when (ex.Status == 409) { }

            EstimateEntity existing;
            try { var resp = await client.GetEntityAsync<EstimateEntity>(code, rowKey); existing = resp.Value; }
            catch (RequestFailedException ex) when (ex.Status == 404)
            { if (attempt == MaxConcurrencyAttempts) throw; throw new RequestFailedException(412, "Retry create after delete race"); }
            existing.Value = estimate.Value;
            existing.SubmittedUtc = estimate.SubmittedUtc;
            try { await client.UpdateEntityAsync(existing, existing.ETag, TableUpdateMode.Replace); }
            catch (RequestFailedException ex) when (ex.Status == 412) { throw; }
        });
    }

    public async Task UpdateWorkItemStateAsync(string code, Guid workItemId, WorkItemState state, string? finalEstimate, DateTime? revealedUtc, DateTime? finalizedUtc)
    {
        var client = _factory.GetClient(WorkItemsTable);
        await ExecuteWithRetryAsync(async attempt =>
        {
            WorkItemEntity entity;
            try { var resp = await client.GetEntityAsync<WorkItemEntity>(code, workItemId.ToString()); entity = resp.Value; }
            catch (RequestFailedException ex) when (ex.Status == 404) { return; }
            entity.State = state.ToString();
            entity.FinalEstimate = finalEstimate;
            entity.RevealedUtc = revealedUtc;
            entity.FinalizedUtc = finalizedUtc;
            try { await client.UpdateEntityAsync(entity, entity.ETag, TableUpdateMode.Replace); }
            catch (RequestFailedException ex) when (ex.Status == 412) { throw; }
        });
    }

    public async Task ClearWorkItemEstimatesAsync(string code, Guid workItemId)
    {
        var client = _factory.GetClient(EstimatesTable);
        await foreach (var est in client.QueryAsync<EstimateEntity>(e => e.PartitionKey == code && e.WorkItemId == workItemId.ToString()))
        {
            try { await client.DeleteEntityAsync(est.PartitionKey, est.RowKey); }
            catch (RequestFailedException ex) when (ex.Status == 404 || ex.Status == 412) { }
        }
    }

    public async Task ResetRevealedWorkItemsAsync(string code)
    {
        var wiClient = _factory.GetClient(WorkItemsTable);
        var estClient = _factory.GetClient(EstimatesTable);
        await foreach (var cursor in wiClient.QueryAsync<WorkItemEntity>(w => w.PartitionKey == code && w.State == WorkItemState.Revealed.ToString()))
        {
            await ExecuteWithRetryAsync(async attempt =>
            {
                WorkItemEntity current;
                try { var resp = await wiClient.GetEntityAsync<WorkItemEntity>(code, cursor.RowKey); current = resp.Value; }
                catch (RequestFailedException ex) when (ex.Status == 404) { return; }
                if (current.State != WorkItemState.Revealed.ToString()) return;
                current.State = WorkItemState.ActiveEstimating.ToString();
                current.FinalEstimate = null;
                current.RevealedUtc = null;
                current.FinalizedUtc = null;
                try { await wiClient.UpdateEntityAsync(current, current.ETag, TableUpdateMode.Replace); }
                catch (RequestFailedException ex) when (ex.Status == 412) { throw; }
                await foreach (var est in estClient.QueryAsync<EstimateEntity>(e => e.PartitionKey == code && e.WorkItemId == cursor.RowKey))
                { try { await estClient.DeleteEntityAsync(est.PartitionKey, est.RowKey); } catch (RequestFailedException ex) when (ex.Status == 404 || ex.Status == 412) { } }
            });
        }
    }

    private static async Task ExecuteWithRetryAsync(Func<int, Task> attemptFunc)
    {
        for (var attempt = 1; attempt <= MaxConcurrencyAttempts; attempt++)
        {
            try { await attemptFunc(attempt); return; }
            catch (RequestFailedException ex) when (ex.Status == 412 && attempt < MaxConcurrencyAttempts)
            { var delayMs = attempt switch { 1 => 20, 2 => 60, _ => 150 }; await Task.Delay(delayMs); continue; }
            catch (RequestFailedException ex) when (ex.Status == 412 && attempt == MaxConcurrencyAttempts)
            { throw new ConcurrencyConflictException("Optimistic concurrency retries exhausted"); }
        }
    }
}
