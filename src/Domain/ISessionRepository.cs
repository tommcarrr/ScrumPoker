namespace ScrumPoker.Domain;

public interface ISessionRepository
{
    Task AddAsync(Session session);
    Task<Session?> GetAsync(string code);
    Task<bool> ExistsAsync(string code);
    Task AddParticipantAsync(string code, Participant participant);
    Task AddWorkItemAsync(string code, WorkItem workItem);
    Task UpsertEstimateAsync(string code, Guid workItemId, Estimate estimate);
    Task UpdateWorkItemStateAsync(string code, Guid workItemId, WorkItemState state, string? finalEstimate, DateTime? revealedUtc, DateTime? finalizedUtc);
    Task ClearWorkItemEstimatesAsync(string code, Guid workItemId);
    Task ResetRevealedWorkItemsAsync(string code);
}
