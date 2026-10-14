namespace TaskFlow.Application.Common.Interfaces;

public interface IActivityWriter
{
    Task WriteAsync(
        Guid boardId,
        Guid actorUserId,
        string action,
        string summary,
        Guid? cardId = null,
        CancellationToken cancellationToken = default);
}
