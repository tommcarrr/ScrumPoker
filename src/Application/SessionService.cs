using ScrumPoker.Domain;
using Microsoft.AspNetCore.SignalR;
using ScrumPoker.RealTime;

namespace ScrumPoker.Application;

public interface ISessionService
{
    Task<SessionSnapshot> CreateSessionAsync();
    Task<SessionSnapshot?> GetSessionAsync(string code);
    Task<SessionSnapshot?> JoinAsync(string code, string displayName);
    Task<SessionSnapshot?> AddWorkItemAsync(string code, string title);
    Task<SessionSnapshot?> SubmitEstimateAsync(string code, Guid workItemId, Guid participantId, string value);
    Task<SessionSnapshot?> RevealAsync(string code, Guid workItemId);
    Task<SessionSnapshot?> FinalizeAsync(string code, Guid workItemId, string value);
    Task<SessionSnapshot?> RestartAsync(string code);
}

public class SessionService : ISessionService
{
    private readonly ISessionRepository _repo;
    private readonly IHubContext<SessionHub>? _hub; // optional for tests without SignalR

    public SessionService(ISessionRepository repo, IHubContext<SessionHub>? hub = null)
    {
        _repo = repo;
        _hub = hub;
    }

    public async Task<SessionSnapshot> CreateSessionAsync()
    {
        var session = Session.Create();
        await _repo.AddAsync(session);
        var snapshot = ToSnapshot(session);
        await BroadcastSnapshotAsync(snapshot, "created");
        return snapshot;
    }

    public async Task<SessionSnapshot?> GetSessionAsync(string code)
    {
        var session = await _repo.GetAsync(code);
        return session is null ? null : ToSnapshot(session);
    }

    public async Task<SessionSnapshot?> JoinAsync(string code, string displayName)
    {
        var session = await _repo.GetAsync(code);
        if (session is null) return null;
        session.AddParticipant(displayName);
        var p = session.Participants.Last();
        await _repo.AddParticipantAsync(code, p);
        var snapshot = ToSnapshot(session);
        await BroadcastSnapshotAsync(snapshot, "participantJoined");
        return snapshot;
    }

    public async Task<SessionSnapshot?> AddWorkItemAsync(string code, string title)
    {
        var session = await _repo.GetAsync(code);
        if (session is null) return null;
        var wi = session.AddWorkItem(title);
        await _repo.AddWorkItemAsync(code, wi);
        var snapshot = ToSnapshot(session);
        await BroadcastSnapshotAsync(snapshot, "workItemAdded");
        return snapshot;
    }

    public async Task<SessionSnapshot?> SubmitEstimateAsync(string code, Guid workItemId, Guid participantId, string value)
    {
        var session = await _repo.GetAsync(code);
        if (session is null) return null;
        session.AddOrUpdateEstimate(workItemId, participantId, value);
        var wi = session.WorkItems.First(w => w.Id == workItemId);
        var est = wi.Estimates.First(e => e.ParticipantId == participantId);
        await _repo.UpsertEstimateAsync(code, workItemId, est);
        var snapshot = ToSnapshot(session);
        await BroadcastSnapshotAsync(snapshot, "estimateSubmitted");
        return snapshot;
    }

    public async Task<SessionSnapshot?> RevealAsync(string code, Guid workItemId)
    {
        var session = await _repo.GetAsync(code);
        if (session is null) return null;
        session.Reveal(workItemId);
        var wi = session.WorkItems.First(w => w.Id == workItemId);
        await _repo.UpdateWorkItemStateAsync(code, workItemId, wi.State, wi.FinalEstimate, wi.RevealedUtc, wi.FinalizedUtc);
        var snapshot = ToSnapshot(session);
        await BroadcastSnapshotAsync(snapshot, "revealed");
        return snapshot;
    }

    public async Task<SessionSnapshot?> FinalizeAsync(string code, Guid workItemId, string value)
    {
        var session = await _repo.GetAsync(code);
        if (session is null) return null;
        session.Finalize(workItemId, value);
        var wi = session.WorkItems.First(w => w.Id == workItemId);
        await _repo.UpdateWorkItemStateAsync(code, workItemId, wi.State, wi.FinalEstimate, wi.RevealedUtc, wi.FinalizedUtc);
        var snapshot = ToSnapshot(session);
        await BroadcastSnapshotAsync(snapshot, "finalized");
        return snapshot;
    }

    public async Task<SessionSnapshot?> RestartAsync(string code)
    {
        var session = await _repo.GetAsync(code);
        if (session is null) return null;
        foreach (var wi in session.WorkItems.Where(w => w.State == WorkItemState.Revealed))
        {
            session.Restart(wi.Id);
            await _repo.ClearWorkItemEstimatesAsync(code, wi.Id);
            var updated = session.WorkItems.First(w => w.Id == wi.Id);
            await _repo.UpdateWorkItemStateAsync(code, wi.Id, updated.State, updated.FinalEstimate, updated.RevealedUtc, updated.FinalizedUtc);
        }
        await _repo.ResetRevealedWorkItemsAsync(code); // safety
        var snapshot = ToSnapshot(session);
        await BroadcastSnapshotAsync(snapshot, "restart");
        return snapshot;
    }

    private static SessionSnapshot ToSnapshot(Session s) => new(
        s.Code,
        s.Deck,
        s.CreatedUtc,
        s.Participants.Select(p => new ParticipantDto(p.Id, p.DisplayName, p.JoinedUtc, p.IsHost)).ToList(),
        s.WorkItems.Select(w => new WorkItemDto(w.Id, w.Title, w.CreatedUtc, w.State.ToString(), w.RevealedUtc, w.FinalizedUtc, w.FinalEstimate,
            w.State == WorkItemState.Revealed || w.State == WorkItemState.Finalized
                ? w.Estimates.Select(e => new { participantId = e.ParticipantId, value = e.Value, submittedUtc = e.SubmittedUtc })
                : Enumerable.Empty<object>())).ToList());

    private Task BroadcastSnapshotAsync(SessionSnapshot snapshot, string reason)
    {
        if (_hub is null) return Task.CompletedTask;
        return _hub.Clients.Group(snapshot.Code.ToUpperInvariant()).SendAsync("sessionUpdated", new
        {
            reason,
            session = snapshot
        });
    }
}
