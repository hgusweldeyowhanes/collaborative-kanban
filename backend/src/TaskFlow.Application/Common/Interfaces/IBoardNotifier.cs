namespace TaskFlow.Application.Common.Interfaces;

public record ActivityNotifyDto(
    Guid Id,
    Guid BoardId,
    Guid ActorUserId,
    string ActorName,
    Guid? CardId,
    string Action,
    string Summary,
    DateTime CreatedAtUtc);

// Implemented in the API layer (wraps SignalR IHubContext).
// Keeps Application ignorant of SignalR, same pattern as IApplicationDbContext.
public interface IBoardNotifier
{
    Task NotifyCardMovedAsync(Guid boardId, Guid cardId, Guid targetListId, int newOrder, CancellationToken ct = default);
    Task NotifyBoardChangedAsync(Guid boardId, string changeType, CancellationToken ct = default);
    Task NotifyActivityAddedAsync(Guid boardId, ActivityNotifyDto activity, CancellationToken ct = default);
}
