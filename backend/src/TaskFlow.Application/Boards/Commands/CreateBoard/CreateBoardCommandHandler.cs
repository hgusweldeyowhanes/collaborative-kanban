using MediatR;
using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Domain.Entities;

namespace TaskFlow.Application.Boards.Commands.CreateBoard;

public class CreateBoardCommandHandler : IRequestHandler<CreateBoardCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public CreateBoardCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(CreateBoardCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            throw new UnauthorizedAccessException("No authenticated user.");

        var board = new Board
        {
            Title = request.Title,
            Description = request.Description,
            OwnerId = _currentUser.UserId.Value
        };

        // Owner is automatically an Admin member of their own board.
        board.Members.Add(new BoardMember
        {
            UserId = _currentUser.UserId.Value,
            Role = BoardRole.Admin
        });

        _context.Boards.Add(board);
        await _context.SaveChangesAsync(cancellationToken);

        return board.Id;
    }
}
