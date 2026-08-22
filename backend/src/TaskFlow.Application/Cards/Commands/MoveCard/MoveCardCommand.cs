using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Common;
using TaskFlow.Application.Common.Interfaces;

namespace TaskFlow.Application.Cards.Commands.MoveCard;

public record MoveCardCommand(Guid BoardId, Guid CardId, Guid TargetListId, int NewOrder) : IRequest;

public class MoveCardCommandValidator : AbstractValidator<MoveCardCommand>
{
    public MoveCardCommandValidator()
    {
        RuleFor(x => x.BoardId).NotEmpty();
        RuleFor(x => x.CardId).NotEmpty();
        RuleFor(x => x.TargetListId).NotEmpty();
        RuleFor(x => x.NewOrder).GreaterThanOrEqualTo(0);
    }
}

public class MoveCardCommandHandler : IRequestHandler<MoveCardCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IBoardNotifier _notifier;

    public MoveCardCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        IBoardNotifier notifier)
    {
        _context = context;
        _currentUser = currentUser;
        _notifier = notifier;
    }

    public async Task Handle(MoveCardCommand request, CancellationToken cancellationToken)
    {
        await BoardAuthorization.EnsureCanEditAsync(_context, request.BoardId, _currentUser.UserId, cancellationToken);

        var card = await _context.Cards
            .Include(c => c.BoardList)
            .FirstOrDefaultAsync(c => c.Id == request.CardId && c.BoardList.BoardId == request.BoardId, cancellationToken)
            ?? throw new KeyNotFoundException($"Card {request.CardId} not found.");

        var targetListOk = await _context.BoardLists
            .AnyAsync(l => l.Id == request.TargetListId && l.BoardId == request.BoardId, cancellationToken);
        if (!targetListOk)
            throw new KeyNotFoundException($"Target list {request.TargetListId} not found.");

        var sourceListId = card.BoardListId;

        // Remove from source ordering, then insert into target at NewOrder.
        if (sourceListId != request.TargetListId)
        {
            var sourceSiblings = await _context.Cards
                .Where(c => c.BoardListId == sourceListId && c.Id != card.Id)
                .OrderBy(c => c.Order)
                .ToListAsync(cancellationToken);
            for (var i = 0; i < sourceSiblings.Count; i++)
            {
                sourceSiblings[i].Order = i;
                sourceSiblings[i].UpdatedAtUtc = DateTime.UtcNow;
            }
        }

        var targetSiblings = await _context.Cards
            .Where(c => c.BoardListId == request.TargetListId && c.Id != card.Id)
            .OrderBy(c => c.Order)
            .ToListAsync(cancellationToken);

        var insertAt = Math.Clamp(request.NewOrder, 0, targetSiblings.Count);
        card.BoardListId = request.TargetListId;
        targetSiblings.Insert(insertAt, card);

        for (var i = 0; i < targetSiblings.Count; i++)
        {
            targetSiblings[i].Order = i;
            targetSiblings[i].UpdatedAtUtc = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);

        await _notifier.NotifyCardMovedAsync(
            request.BoardId, request.CardId, request.TargetListId, insertAt, cancellationToken);
    }
}
