using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Common;
using TaskFlow.Application.Common.Interfaces;

namespace TaskFlow.Application.Cards.Queries.GetCardChecklists;

public record ChecklistItemDto(Guid Id, string Title, bool IsDone, int Order);
public record ChecklistDto(Guid Id, string Title, int Order, List<ChecklistItemDto> Items);

public record GetCardChecklistsQuery(Guid BoardId, Guid CardId) : IRequest<List<ChecklistDto>>;

public class GetCardChecklistsQueryHandler : IRequestHandler<GetCardChecklistsQuery, List<ChecklistDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetCardChecklistsQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<List<ChecklistDto>> Handle(GetCardChecklistsQuery request, CancellationToken cancellationToken)
    {
        await BoardAuthorization.EnsureCanViewAsync(_context, request.BoardId, _currentUser.UserId, cancellationToken);

        var cardOk = await _context.Cards
            .AnyAsync(c => c.Id == request.CardId && c.BoardList.BoardId == request.BoardId, cancellationToken);
        if (!cardOk)
            throw new KeyNotFoundException($"Card {request.CardId} not found.");

        return await _context.CardChecklists
            .AsNoTracking()
            .Where(c => c.CardId == request.CardId)
            .OrderBy(c => c.Order)
            .Select(c => new ChecklistDto(
                c.Id,
                c.Title,
                c.Order,
                c.Items.OrderBy(i => i.Order)
                    .Select(i => new ChecklistItemDto(i.Id, i.Title, i.IsDone, i.Order))
                    .ToList()))
            .ToListAsync(cancellationToken);
    }
}
