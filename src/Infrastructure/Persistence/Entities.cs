using Azure;
using Azure.Data.Tables;
using ScrumPoker.Domain;

namespace ScrumPoker.Infrastructure.Persistence;

public class SessionEntity : ITableEntity
{
    public string PartitionKey { get; set; } = "SESSION";
    public string RowKey { get; set; } = default!;
    public DateTime CreatedUtc { get; set; }
    public string DeckCsv { get; set; } = string.Empty;
    public string ETagValue { get; set; } = string.Empty;
    public ETag ETag { get => new(ETagValue); set => ETagValue = value.ToString(); }
    public DateTimeOffset? Timestamp { get; set; }
}

public static class SessionEntityMapper
{
    public static SessionEntity ToEntity(Session s) => new()
    {
        RowKey = s.Code,
        CreatedUtc = s.CreatedUtc,
        DeckCsv = string.Join(',', s.Deck)
    };
}

public static class ParticipantEntityMapper
{
    public static ParticipantEntity ToEntity(Participant p, string sessionCode) => new()
    {
        PartitionKey = sessionCode,
        RowKey = p.Id.ToString(),
        DisplayName = p.DisplayName,
        IsHost = p.IsHost,
        JoinedUtc = p.JoinedUtc
    };

    public static Participant ToDomain(ParticipantEntity e) => Participant.Hydrate(Guid.Parse(e.RowKey), e.DisplayName, e.JoinedUtc, e.IsHost);
}

public static class WorkItemEntityMapper
{
    public static WorkItemEntity ToEntity(WorkItem w, string sessionCode) => new()
    {
        PartitionKey = sessionCode,
        RowKey = w.Id.ToString(),
        Title = w.Title,
        State = w.State.ToString(),
        CreatedUtc = w.CreatedUtc,
        RevealedUtc = w.RevealedUtc,
        FinalizedUtc = w.FinalizedUtc,
        FinalEstimate = w.FinalEstimate
    };

    public static WorkItem ToDomain(WorkItemEntity e, IEnumerable<Estimate>? estimates) =>
        WorkItem.Hydrate(Guid.Parse(e.RowKey), e.Title, e.CreatedUtc,
            Enum.TryParse<WorkItemState>(e.State, out var st) ? st : WorkItemState.ActiveEstimating,
            e.RevealedUtc, e.FinalizedUtc, e.FinalEstimate, estimates);
}

public static class EstimateEntityMapper
{
    public static EstimateEntity ToEntity(Guid workItemId, Estimate est, string sessionCode) => new()
    {
        PartitionKey = sessionCode,
        RowKey = workItemId + ":" + est.ParticipantId,
        WorkItemId = workItemId.ToString(),
        ParticipantId = est.ParticipantId.ToString(),
        Value = est.Value,
        SubmittedUtc = est.SubmittedUtc
    };

    public static (Guid workItemId, Estimate estimate) ToDomain(EstimateEntity e) =>
        (Guid.Parse(e.WorkItemId), Estimate.Hydrate(Guid.Parse(e.ParticipantId), e.Value, e.SubmittedUtc));
}

public class ParticipantEntity : ITableEntity
{
    public string PartitionKey { get; set; } = default!;
    public string RowKey { get; set; } = default!;
    public string DisplayName { get; set; } = string.Empty;
    public bool IsHost { get; set; }
    public DateTime JoinedUtc { get; set; }
    public ETag ETag { get; set; }
    public DateTimeOffset? Timestamp { get; set; }
}

public class WorkItemEntity : ITableEntity
{
    public string PartitionKey { get; set; } = default!;
    public string RowKey { get; set; } = default!;
    public string Title { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public DateTime CreatedUtc { get; set; }
    public DateTime? RevealedUtc { get; set; }
    public DateTime? FinalizedUtc { get; set; }
    public string? FinalEstimate { get; set; }
    public ETag ETag { get; set; }
    public DateTimeOffset? Timestamp { get; set; }
}

public class EstimateEntity : ITableEntity
{
    public string PartitionKey { get; set; } = default!;
    public string RowKey { get; set; } = default!;
    public string WorkItemId { get; set; } = string.Empty;
    public string ParticipantId { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public DateTime SubmittedUtc { get; set; }
    public ETag ETag { get; set; }
    public DateTimeOffset? Timestamp { get; set; }
}
