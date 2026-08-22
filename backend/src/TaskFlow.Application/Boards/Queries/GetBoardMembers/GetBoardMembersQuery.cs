using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Common;
using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Domain.Entities;

namespace TaskFlow.Application.Boards.Queries.GetBoardMembers;

public record BoardMemberDto(Guid UserId, string Email, string DisplayName, BoardRole Role);

public record GetBoardMembersQuery(Guid BoardId) : IRequest<List<BoardMemberDto>>;

public class GetBoardMembersQueryHandler : IRequestHandler<GetBoardMembersQuery, List<BoardMemberDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetBoardMembersQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<List<BoardMemberDto>> Handle(GetBoardMembersQuery request, CancellationToken cancellationToken)
    {
        await BoardAuthorization.EnsureCanViewAsync(_context, request.BoardId, _currentUser.UserId, cancellationToken);

        var board = await _context.Boards
            .AsNoTracking()
            .Where(b => b.Id == request.BoardId)
            .Select(b => new { b.OwnerId, OwnerEmail = b.Owner.Email, OwnerName = b.Owner.DisplayName })
            .FirstAsync(cancellationToken);

        var members = await _context.BoardMembers
            .AsNoTracking()
            .Where(m => m.BoardId == request.BoardId)
            .Select(m => new BoardMemberDto(m.UserId, m.User.Email, m.User.DisplayName, m.Role))
            .ToListAsync(cancellationToken);

        if (members.All(m => m.UserId != board.OwnerId))
        {
            members.Insert(0, new BoardMemberDto(board.OwnerId, board.OwnerEmail, board.OwnerName, BoardRole.Admin));
        }

        return members.OrderByDescending(m => m.Role).ThenBy(m => m.DisplayName).ToList();
    }
}
