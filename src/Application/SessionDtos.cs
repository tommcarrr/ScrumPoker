namespace ScrumPoker.Application;

public record ParticipantDto(Guid Id, string DisplayName, DateTime JoinedUtc, bool IsHost);
public record WorkItemDto(Guid Id, string Title, DateTime CreatedUtc, string State, DateTime? RevealedUtc, DateTime? FinalizedUtc, string? FinalEstimate, IEnumerable<object> Estimates);
public record SessionSnapshot(string Code, IEnumerable<string> Deck, DateTime CreatedUtc, IEnumerable<ParticipantDto> Participants, IEnumerable<WorkItemDto> WorkItems);
