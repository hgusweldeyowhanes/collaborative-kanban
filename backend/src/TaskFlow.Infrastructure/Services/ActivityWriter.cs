using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Domain.Entities;

namespace TaskFlow.Infrastructure.Services;

public class ActivityWriter : IActivityWriter
{
    private readonly IApplicationDbContext _context;

    public ActivityWriter(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task WriteAsync(
        Guid boardId,
        Guid actorUserId,
        string action,
        string summary,
        Guid? cardId = null,
        CancellationToken cancellationToken = default)
    {
        _context.BoardActivities.Add(new BoardActivity
        {
            BoardId = boardId,
            ActorUserId = actorUserId,
            CardId = cardId,
            Action = action,
            Summary = summary,
            CreatedAtUtc = DateTime.UtcNow
        });
        await _context.SaveChangesAsync(cancellationToken);
    }
}
