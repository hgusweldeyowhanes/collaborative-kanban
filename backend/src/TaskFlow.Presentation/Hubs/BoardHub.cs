using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace TaskFlow.Presentation.Hubs;

[Authorize]
public class BoardHub : Hub
{
    // Clients call this right after connecting to scope broadcasts to
    // just the board they're viewing (group name = board id).
    public async Task JoinBoard(string boardId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(boardId));
    }

    public async Task LeaveBoard(string boardId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(boardId));
    }

    public static string GroupName(string boardId) => $"board-{boardId}";
}
