using System.Collections.Concurrent;
using ScrumPoker.Domain;

namespace ScrumPoker.Infrastructure;

public class InMemorySessionRepository : ISessionRepository
{
    private readonly ConcurrentDictionary<string, Session> _sessions = new();

    public Task AddAsync(Session session)
    {
        _sessions[session.Code] = session;
        return Task.CompletedTask;
    }

    public Task<Session?> GetAsync(string code)
    {
        _sessions.TryGetValue(code, out var session);
        return Task.FromResult(session);
    }

    public Task<bool> ExistsAsync(string code) => Task.FromResult(_sessions.ContainsKey(code));

    public Task AddParticipantAsync(string code, Participant participant) => Task.CompletedTask;
    public Task AddWorkItemAsync(string code, WorkItem workItem) => Task.CompletedTask;
    public Task UpsertEstimateAsync(string code, Guid workItemId, Estimate estimate) => Task.CompletedTask;
    public Task UpdateWorkItemStateAsync(string code, Guid workItemId, WorkItemState state, string? finalEstimate, DateTime? revealedUtc, DateTime? finalizedUtc) => Task.CompletedTask;
    public Task ClearWorkItemEstimatesAsync(string code, Guid workItemId) => Task.CompletedTask;
    public Task ResetRevealedWorkItemsAsync(string code) => Task.CompletedTask;
}
