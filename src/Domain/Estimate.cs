namespace ScrumPoker.Domain;

public class Estimate
{
    public Guid ParticipantId { get; }
    public string Value { get; private set; }
    public DateTime SubmittedUtc { get; private set; }

    private Estimate(Guid participantId, string value, DateTime submittedUtc)
    {
        ParticipantId = participantId;
        Value = value;
        SubmittedUtc = submittedUtc;
    }

    public static Estimate Create(Guid participantId, string value, DateTime? submittedUtc = null)
    {
        if (!Deck.IsValid(value)) throw new ArgumentException("Invalid deck value", nameof(value));
        return new Estimate(participantId, value, submittedUtc ?? DateTime.UtcNow);
    }

    public void Update(string value)
    {
        if (!Deck.IsValid(value)) throw new ArgumentException("Invalid deck value", nameof(value));
        Value = value;
        SubmittedUtc = DateTime.UtcNow;
    }

    public static Estimate Hydrate(Guid participantId, string value, DateTime submittedUtc)
        => new Estimate(participantId, value, submittedUtc);
}
