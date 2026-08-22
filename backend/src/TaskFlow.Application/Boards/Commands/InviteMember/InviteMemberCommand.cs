using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Common;
using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Domain.Entities;

namespace TaskFlow.Application.Boards.Commands.InviteMember;

public record InviteMemberCommand(Guid BoardId, string Email, BoardRole Role) : IRequest<Guid>;

public class InviteMemberCommandValidator : AbstractValidator<InviteMemberCommand>
{
    public InviteMemberCommandValidator()
    {
        RuleFor(x => x.BoardId).NotEmpty();
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Role).IsInEnum();
    }
}

public class InviteMemberCommandHandler : IRequestHandler<InviteMemberCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IBoardNotifier _notifier;

    public InviteMemberCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        IBoardNotifier notifier)
    {
        _context = context;
        _currentUser = currentUser;
        _notifier = notifier;
    }

    public async Task<Guid> Handle(InviteMemberCommand request, CancellationToken cancellationToken)
    {
        await BoardAuthorization.EnsureAdminAsync(_context, request.BoardId, _currentUser.UserId, cancellationToken);

        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email, cancellationToken)
            ?? throw new KeyNotFoundException($"No user registered with email {request.Email}.");

        var exists = await _context.BoardMembers
            .AnyAsync(m => m.BoardId == request.BoardId && m.UserId == user.Id, cancellationToken);

        if (exists)
            throw new InvalidOperationException("User is already a board member.");

        var board = await _context.Boards.FirstAsync(b => b.Id == request.BoardId, cancellationToken);
        if (board.OwnerId == user.Id)
            throw new InvalidOperationException("Board owner is already a member.");

        var member = new BoardMember
        {
            BoardId = request.BoardId,
            UserId = user.Id,
            Role = request.Role == BoardRole.Admin ? BoardRole.Admin : request.Role
        };

        _context.BoardMembers.Add(member);
        await _context.SaveChangesAsync(cancellationToken);
        await _notifier.NotifyBoardChangedAsync(request.BoardId, "MemberAdded", cancellationToken);
        return member.Id;
    }
}
