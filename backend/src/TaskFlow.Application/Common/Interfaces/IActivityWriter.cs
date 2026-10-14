namespace TaskFlow.Application.Common.Interfaces;

public interface IActivityWriter
{
    Task<ActivityNotifyDto> WriteAsync(
        Guid boardId,
        Guid actorUserId,
        string action,
        string summary,
        Guid? cardId = null,
        CancellationToken cancellationToken = default);
}
