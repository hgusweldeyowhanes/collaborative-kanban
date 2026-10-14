using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Common;
using TaskFlow.Application.Common.Interfaces;

namespace TaskFlow.Application.Boards.Queries.GetBoardActivities;

public record ActivityDto(
    Guid Id,
    Guid BoardId,
    Guid ActorUserId,
    string ActorName,
    Guid? CardId,
    string Action,
    string Summary,
    DateTime CreatedAtUtc);

public record GetBoardActivitiesQuery(Guid BoardId, int Take = 50) : IRequest<List<ActivityDto>>;

public class GetBoardActivitiesQueryHandler : IRequestHandler<GetBoardActivitiesQuery, List<ActivityDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetBoardActivitiesQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<List<ActivityDto>> Handle(GetBoardActivitiesQuery request, CancellationToken cancellationToken)
    {
        await BoardAuthorization.EnsureCanViewAsync(_context, request.BoardId, _currentUser.UserId, cancellationToken);

        var take = Math.Clamp(request.Take, 1, 100);

        return await _context.BoardActivities
            .AsNoTracking()
            .Where(a => a.BoardId == request.BoardId)
            .OrderByDescending(a => a.CreatedAtUtc)
            .Take(take)
            .Select(a => new ActivityDto(
                a.Id,
                a.BoardId,
                a.ActorUserId,
                a.Actor.DisplayName,
                a.CardId,
                a.Action,
                a.Summary,
                a.CreatedAtUtc))
            .ToListAsync(cancellationToken);
    }
}
