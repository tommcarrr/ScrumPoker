namespace ScrumPoker.Domain;

public class Session
{
    private static readonly string[] DefaultDeck = new[] { "0", "1", "2", "3", "5", "8", "13", "20", "40", "100", "?" };

    public string Code { get; }
    public DateTime CreatedUtc { get; }
    public IReadOnlyList<string> Deck { get; }
    private readonly List<Participant> _participants = new();
    public IReadOnlyCollection<Participant> Participants => _participants.AsReadOnly();
    private readonly List<WorkItem> _workItems = new();
    public IReadOnlyCollection<WorkItem> WorkItems => _workItems.AsReadOnly();

    private Session(string code, DateTime createdUtc, IReadOnlyList<string> deck)
    {
        Code = code;
        CreatedUtc = createdUtc;
        Deck = deck;
    }

    public static Session Create(string? code = null, IEnumerable<string>? deck = null, DateTime? createdUtc = null)
    {
        var sessionCode = code ?? GenerateCode();
        var usedDeck = (deck?.ToArray() ?? DefaultDeck);
        if (usedDeck.Length == 0) throw new ArgumentException("Deck cannot be empty", nameof(deck));
        var timestamp = createdUtc ?? DateTime.UtcNow;
        return new Session(sessionCode, timestamp, usedDeck);
    }

    public Participant AddParticipant(string displayName)
    {
        var exists = _participants.Any(p => string.Equals(p.DisplayName, displayName.Trim(), StringComparison.OrdinalIgnoreCase));
        if (exists) throw new InvalidOperationException("Duplicate participant display name");
        var participant = Participant.Create(displayName, isHost: _participants.Count == 0);
        _participants.Add(participant);
        return participant;
    }

    public WorkItem AddWorkItem(string title)
    {
        var item = WorkItem.Create(title);
        _workItems.Add(item);
        return item;
    }

    public void AddOrUpdateEstimate(Guid workItemId, Guid participantId, string value)
    {
        var wi = _workItems.FirstOrDefault(w => w.Id == workItemId);
        if (wi is null) throw new InvalidOperationException("Work item not found");
        wi.AddOrUpdateEstimate(participantId, value);
    }

    public void Reveal(Guid workItemId)
    {
        var wi = _workItems.FirstOrDefault(w => w.Id == workItemId);
        if (wi is null) throw new InvalidOperationException("Work item not found");
        wi.Reveal();
    }

    public void Finalize(Guid workItemId, string value)
    {
        var wi = _workItems.FirstOrDefault(w => w.Id == workItemId);
        if (wi is null) throw new InvalidOperationException("Work item not found");
        wi.Finalize(value);
    }

    public void Restart(Guid workItemId)
    {
        var wi = _workItems.FirstOrDefault(w => w.Id == workItemId);
        if (wi is null) throw new InvalidOperationException("Work item not found");
        wi.Restart();
    }

    private static string GenerateCode()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789"; // 36 chars
        var rng = Random.Shared;
        Span<char> buffer = stackalloc char[6];
        for (var i = 0; i < buffer.Length; i++)
        {
            buffer[i] = chars[rng.Next(chars.Length)];
        }
        return new string(buffer);
    }

    public static Session Hydrate(string code, DateTime createdUtc, IEnumerable<string> deck,
        IEnumerable<Participant>? participants = null,
        IEnumerable<WorkItem>? workItems = null)
    {
        var s = new Session(code, createdUtc, deck.ToArray());
        if (participants != null) s._participants.AddRange(participants);
        if (workItems != null) s._workItems.AddRange(workItems);
        return s;
    }
}
