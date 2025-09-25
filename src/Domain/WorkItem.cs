namespace ScrumPoker.Domain;

public class WorkItem
{
    public Guid Id { get; }
    public string Title { get; }
    public DateTime CreatedUtc { get; }
    private readonly List<Estimate> _estimates = new();
    public IReadOnlyCollection<Estimate> Estimates => _estimates.AsReadOnly();

    public WorkItemState State { get; private set; } = WorkItemState.ActiveEstimating; // Simplified: start directly in estimating for MVP
    public DateTime? RevealedUtc { get; private set; }
    public DateTime? FinalizedUtc { get; private set; }
    public string? FinalEstimate { get; private set; }

    private WorkItem(Guid id, string title, DateTime createdUtc)
    {
        Id = id;
        Title = title;
        CreatedUtc = createdUtc;
    }

    public static WorkItem Create(string title, DateTime? createdUtc = null)
    {
        if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("Title required", nameof(title));
        var trimmed = title.Trim();
        if (trimmed.Length > 140) throw new ArgumentException("Title too long", nameof(title));
        return new WorkItem(Guid.NewGuid(), trimmed, createdUtc ?? DateTime.UtcNow);
    }

    public Estimate AddOrUpdateEstimate(Guid participantId, string value)
    {
        if (State != WorkItemState.ActiveEstimating && State != WorkItemState.Revealed)
            throw new InvalidOperationException("Cannot add estimate in current state");

        var existing = _estimates.FirstOrDefault(e => e.ParticipantId == participantId);
        if (existing is null)
        {
            var estimate = Estimate.Create(participantId, value);
            _estimates.Add(estimate);
            return estimate;
        }
        existing.Update(value);
        return existing;
    }

    public void Reveal()
    {
        if (State != WorkItemState.ActiveEstimating) return; // idempotent for tests
        State = WorkItemState.Revealed;
        RevealedUtc = DateTime.UtcNow;
    }

    public void Finalize(string value)
    {
        if (State != WorkItemState.Revealed)
            throw new InvalidOperationException("Cannot finalize unless revealed");
        if (value == "?") throw new ArgumentException("Final estimate cannot be '?' ");
        if (!Deck.IsValid(value)) throw new ArgumentException("Invalid final estimate value");
        FinalEstimate = value;
        State = WorkItemState.Finalized;
        FinalizedUtc = DateTime.UtcNow;
    }

    public void Restart()
    {
        if (State != WorkItemState.Revealed) return; // allow only from revealed per data model
        _estimates.Clear();
        FinalEstimate = null;
        RevealedUtc = null;
        FinalizedUtc = null;
        State = WorkItemState.ActiveEstimating;
    }

    public static WorkItem Hydrate(Guid id, string title, DateTime createdUtc, WorkItemState state,
        DateTime? revealedUtc, DateTime? finalizedUtc, string? finalEstimate, IEnumerable<Estimate>? estimates)
    {
        var wi = new WorkItem(id, title, createdUtc)
        {
            State = state,
            RevealedUtc = revealedUtc,
            FinalizedUtc = finalizedUtc,
            FinalEstimate = finalEstimate
        };
        if (estimates != null) wi._estimates.AddRange(estimates);
        return wi;
    }
}

public enum WorkItemState
{
    ActiveEstimating,
    Revealed,
    Finalized
}
