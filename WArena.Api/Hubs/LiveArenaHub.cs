using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace WArena.Api.Hubs;

[Authorize]
public class LiveArenaHub : Hub
{
    public Task JoinRoom(string code) => Groups.AddToGroupAsync(Context.ConnectionId, code.ToUpperInvariant());
    public Task LeaveRoom(string code) => Groups.RemoveFromGroupAsync(Context.ConnectionId, code.ToUpperInvariant());
}
