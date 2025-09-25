namespace ScrumPoker.Domain;

public class Participant
{
    public Guid Id { get; }
    public string DisplayName { get; }
    public DateTime JoinedUtc { get; }
    public bool IsHost { get; }

    private Participant(Guid id, string displayName, DateTime joinedUtc, bool isHost)
    {
        Id = id;
        DisplayName = displayName;
        JoinedUtc = joinedUtc;
        IsHost = isHost;
    }

    public static Participant Create(string displayName, bool isHost = false, DateTime? joinedUtc = null)
    {
        if (string.IsNullOrWhiteSpace(displayName)) throw new ArgumentException("Display name required", nameof(displayName));
        var trimmed = displayName.Trim();
        if (trimmed.Length > 40) throw new ArgumentException("Display name too long", nameof(displayName));
        return new Participant(Guid.NewGuid(), trimmed, joinedUtc ?? DateTime.UtcNow, isHost);
    }

    public static Participant Hydrate(Guid id, string displayName, DateTime joinedUtc, bool isHost)
        => new Participant(id, displayName, joinedUtc, isHost);
}
