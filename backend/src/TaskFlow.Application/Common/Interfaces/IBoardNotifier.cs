namespace TaskFlow.Application.Common.Interfaces;

// Implemented in the API layer (wraps SignalR IHubContext).
// Keeps Application ignorant of SignalR, same pattern as IApplicationDbContext.
public interface IBoardNotifier
{
    Task NotifyCardMovedAsync(Guid boardId, Guid cardId, Guid targetListId, int newOrder, CancellationToken ct = default);
    Task NotifyBoardChangedAsync(Guid boardId, string changeType, CancellationToken ct = default);
}
