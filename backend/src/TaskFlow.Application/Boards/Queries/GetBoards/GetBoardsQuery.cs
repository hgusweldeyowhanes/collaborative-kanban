using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Common.Interfaces;

namespace TaskFlow.Application.Boards.Queries.GetBoards;

public record BoardSummaryDto(Guid Id, string Title, string? Description, DateTime CreatedAtUtc);

public record GetBoardsQuery : IRequest<List<BoardSummaryDto>>;

public class GetBoardsQueryHandler : IRequestHandler<GetBoardsQuery, List<BoardSummaryDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetBoardsQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<List<BoardSummaryDto>> Handle(GetBoardsQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;

        return await _context.Boards
            .Where(b => b.OwnerId == userId || b.Members.Any(m => m.UserId == userId))
            .OrderByDescending(b => b.CreatedAtUtc)
            .Select(b => new BoardSummaryDto(b.Id, b.Title, b.Description, b.CreatedAtUtc))
            .ToListAsync(cancellationToken);
    }
}
