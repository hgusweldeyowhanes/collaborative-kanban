using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
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
    private readonly IBoardNotifier _notifier;

    public MoveCardCommandHandler(IApplicationDbContext context, IBoardNotifier notifier)
    {
        _context = context;
        _notifier = notifier;
    }

    public async Task Handle(MoveCardCommand request, CancellationToken cancellationToken)
    {
        var card = await _context.Cards
            .FirstOrDefaultAsync(c => c.Id == request.CardId, cancellationToken)
            ?? throw new KeyNotFoundException($"Card {request.CardId} not found.");

        card.BoardListId = request.TargetListId;
        card.Order = request.NewOrder;
        card.UpdatedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        // Broadcast to every client currently viewing this board so their
        // UI updates without a manual refresh.
        await _notifier.NotifyCardMovedAsync(
            request.BoardId, request.CardId, request.TargetListId, request.NewOrder, cancellationToken);
    }
}
