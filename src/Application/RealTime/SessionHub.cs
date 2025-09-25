using Microsoft.AspNetCore.SignalR;

namespace ScrumPoker.RealTime;

/// <summary>
/// SignalR hub for session real-time updates placed in the Application layer so both Server and Application services can reference it.
/// </summary>
public class SessionHub : Hub
{
    public async Task JoinSessionGroup(string sessionCode)
    {
        if (string.IsNullOrWhiteSpace(sessionCode)) return;
        await Groups.AddToGroupAsync(Context.ConnectionId, sessionCode.ToUpperInvariant());
    }

    public async Task LeaveSessionGroup(string sessionCode)
    {
        if (string.IsNullOrWhiteSpace(sessionCode)) return;
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, sessionCode.ToUpperInvariant());
    }
}