using Microsoft.AspNetCore.SignalR;
using TaskFlow.Application.Common.Interfaces;

namespace TaskFlow.Presentation.Hubs;

public class SignalRBoardNotifier : IBoardNotifier
{
    private readonly IHubContext<BoardHub> _hubContext;

    public SignalRBoardNotifier(IHubContext<BoardHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public Task NotifyCardMovedAsync(Guid boardId, Guid cardId, Guid targetListId, int newOrder, CancellationToken ct = default)
    {
        return _hubContext.Clients
            .Group(BoardHub.GroupName(boardId.ToString()))
            .SendAsync("CardMoved", new { boardId, cardId, targetListId, newOrder }, ct);
    }

    public Task NotifyBoardChangedAsync(Guid boardId, string changeType, CancellationToken ct = default)
    {
        return _hubContext.Clients
            .Group(BoardHub.GroupName(boardId.ToString()))
            .SendAsync("BoardChanged", new { boardId, changeType }, ct);
    }
}
