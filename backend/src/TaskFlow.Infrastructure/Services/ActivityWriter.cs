using Microsoft.EntityFrameworkCore;
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

    public async Task<ActivityNotifyDto> WriteAsync(
        Guid boardId,
        Guid actorUserId,
        string action,
        string summary,
        Guid? cardId = null,
        CancellationToken cancellationToken = default)
    {
        var actorName = await _context.Users
            .Where(u => u.Id == actorUserId)
            .Select(u => u.DisplayName)
            .FirstOrDefaultAsync(cancellationToken) ?? "Someone";

        var activity = new BoardActivity
        {
            BoardId = boardId,
            ActorUserId = actorUserId,
            CardId = cardId,
            Action = action,
            Summary = summary,
            CreatedAtUtc = DateTime.UtcNow
        };
        _context.BoardActivities.Add(activity);
        await _context.SaveChangesAsync(cancellationToken);

        return new ActivityNotifyDto(
            activity.Id,
            boardId,
            actorUserId,
            actorName,
            cardId,
            action,
            summary,
            activity.CreatedAtUtc);
    }
}