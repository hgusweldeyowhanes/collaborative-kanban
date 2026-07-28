using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Common.Interfaces;

namespace TaskFlow.Application.Boards.Queries.GetBoardDetail;

public record CardDto(Guid Id, string Title, string? Description, int Order, Guid? AssigneeId);
public record BoardListDto(Guid Id, string Title, int Order, List<CardDto> Cards);
public record BoardDetailDto(Guid Id, string Title, string? Description, List<BoardListDto> Lists);

public record GetBoardDetailQuery(Guid BoardId) : IRequest<BoardDetailDto>;

public class GetBoardDetailQueryHandler : IRequestHandler<GetBoardDetailQuery, BoardDetailDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetBoardDetailQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<BoardDetailDto> Handle(GetBoardDetailQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;

        var board = await _context.Boards
            .Where(b => b.Id == request.BoardId &&
                        (b.OwnerId == userId || b.Members.Any(m => m.UserId == userId)))
            .Include(b => b.Lists)
                .ThenInclude(l => l.Cards)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new KeyNotFoundException($"Board {request.BoardId} not found.");

        var lists = board.Lists
            .OrderBy(l => l.Order)
            .Select(l => new BoardListDto(
                l.Id,
                l.Title,
                l.Order,
                l.Cards.OrderBy(c => c.Order)
                       .Select(c => new CardDto(c.Id, c.Title, c.Description, c.Order, c.AssigneeId))
                       .ToList()))
            .ToList();

        return new BoardDetailDto(board.Id, board.Title, board.Description, lists);
    }
}
