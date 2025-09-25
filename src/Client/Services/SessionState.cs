namespace ScrumPoker.Client.Services;

public class SessionState : IAsyncDisposable, IDisposable
{
    private readonly SessionHubClient _hub;
    private bool _disposed;

    public SessionHubClient.SessionSnapshotDto? Current { get; private set; }
    public string? CurrentSessionCode => Current?.Code;
    public string? SelfDisplayName { get; private set; }
    public Guid? SelfParticipantId { get; private set; }

    public event Action? Changed;

    public SessionState(SessionHubClient hub)
    {
        _hub = hub;
        _hub.SessionUpdated += OnSessionUpdated;
    }

    private void OnSessionUpdated(SessionHubClient.SessionUpdatedMessage msg)
    {
        Current = msg.Session;
        if (SelfParticipantId is null && SelfDisplayName is not null && Current is not null)
        {
            var match = Current.Participants.FirstOrDefault(p => string.Equals(p.DisplayName, SelfDisplayName, StringComparison.OrdinalIgnoreCase));
            if (match is not null)
                SelfParticipantId = match.Id;
        }
        Changed?.Invoke();
    }

    public async Task JoinAsync(string code)
    {
        await _hub.JoinSessionAsync(code);
    }

    public void SetSnapshot(SessionHubClient.SessionSnapshotDto snapshot)
    {
        Current = snapshot;
        if (SelfParticipantId is null && SelfDisplayName is not null)
        {
            var match = snapshot.Participants.FirstOrDefault(p => string.Equals(p.DisplayName, SelfDisplayName, StringComparison.OrdinalIgnoreCase));
            if (match is not null) SelfParticipantId = match.Id;
        }
        Changed?.Invoke();
    }

    public void SetSelfDisplayName(string displayName)
    {
        SelfDisplayName = displayName;
        if (Current is not null && SelfParticipantId is null)
        {
            var match = Current.Participants.FirstOrDefault(p => string.Equals(p.DisplayName, displayName, StringComparison.OrdinalIgnoreCase));
            if (match is not null) SelfParticipantId = match.Id;
        }
    }

    public async Task LeaveAsync()
    {
        if (CurrentSessionCode is not null)
            await _hub.LeaveSessionAsync(CurrentSessionCode);
        Current = null;
        SelfParticipantId = null;
        Changed?.Invoke();
    }

    public ValueTask DisposeAsync()
    {
        if (_disposed) return ValueTask.CompletedTask;
        _hub.SessionUpdated -= OnSessionUpdated;
        _disposed = true;
        return ValueTask.CompletedTask;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _hub.SessionUpdated -= OnSessionUpdated;
        _disposed = true;
    }
}