using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.Components;

namespace ScrumPoker.Client.Services;

public class SessionHubClient : IAsyncDisposable, IDisposable
{
    private readonly NavigationManager _nav;
    private HubConnection? _connection;
    private bool _disposed;

    public event Action<SessionUpdatedMessage>? SessionUpdated;

    public SessionHubClient(NavigationManager nav) => _nav = nav;

    public async Task EnsureConnectedAsync()
    {
        if (_connection is not null) return;
        var baseUri = _nav.BaseUri.TrimEnd('/');
        _connection = new HubConnectionBuilder()
            .WithUrl(baseUri + "/hubs/session")
            .WithAutomaticReconnect()
            .Build();

        _connection.On<SessionUpdatedPayload>("sessionUpdated", payload =>
        {
            SessionUpdated?.Invoke(new SessionUpdatedMessage(payload.reason, payload.session));
        });

        await _connection.StartAsync();
    }

    public async Task JoinSessionAsync(string code)
    {
        await EnsureConnectedAsync();
        await _connection!.InvokeAsync("JoinSessionGroup", code);
    }

    public async Task LeaveSessionAsync(string code)
    {
        if (_connection is null) return;
        await _connection.InvokeAsync("LeaveSessionGroup", code);
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        if (_connection is not null)
        {
            _connection.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }
    }

    public record SessionUpdatedMessage(string Reason, SessionSnapshotDto Session);
    public record SessionUpdatedPayload(string reason, SessionSnapshotDto session);
    public record SessionSnapshotDto(string Code, List<string> Deck, DateTime CreatedUtc, List<ParticipantDto> Participants, List<WorkItemDto> WorkItems);
    public record ParticipantDto(Guid Id, string DisplayName, DateTime JoinedUtc, bool IsHost);
    public record WorkItemDto(Guid Id, string Title, DateTime CreatedUtc, string State, DateTime? RevealedUtc, DateTime? FinalizedUtc, string? FinalEstimate, IEnumerable<object> Estimates);
}