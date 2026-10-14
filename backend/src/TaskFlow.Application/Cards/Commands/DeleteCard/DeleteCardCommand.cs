using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Common;
using TaskFlow.Application.Common.Interfaces;

namespace TaskFlow.Application.Cards.Commands.DeleteCard;

public record DeleteCardCommand(Guid BoardId, Guid CardId) : IRequest;

public class DeleteCardCommandValidator : AbstractValidator<DeleteCardCommand>
{
    public DeleteCardCommandValidator()
    {
        RuleFor(x => x.BoardId).NotEmpty();
        RuleFor(x => x.CardId).NotEmpty();
    }
}

public class DeleteCardCommandHandler : IRequestHandler<DeleteCardCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IBoardNotifier _notifier;
    private readonly IActivityWriter _activity;

    public DeleteCardCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        IBoardNotifier notifier,
        IActivityWriter activity)
    {
        _context = context;
        _currentUser = currentUser;
        _notifier = notifier;
        _activity = activity;
    }

    public async Task Handle(DeleteCardCommand request, CancellationToken cancellationToken)
    {
        await BoardAuthorization.EnsureCanEditAsync(_context, request.BoardId, _currentUser.UserId, cancellationToken);

        var card = await _context.Cards
            .Include(c => c.BoardList)
            .FirstOrDefaultAsync(c => c.Id == request.CardId && c.BoardList.BoardId == request.BoardId, cancellationToken)
            ?? throw new KeyNotFoundException($"Card {request.CardId} not found.");

        var title = card.Title;
        var cardId = card.Id;
        _context.Cards.Remove(card);
        await _context.SaveChangesAsync(cancellationToken);
        var activity = await _activity.WriteAsync(
            request.BoardId, _currentUser.UserId!, "CardDeleted", $"deleted “{title}”", cardId, cancellationToken);
        await _notifier.NotifyActivityAddedAsync(request.BoardId, activity, cancellationToken);
        await _notifier.NotifyBoardChangedAsync(request.BoardId, "CardDeleted", cancellationToken);
    }
}
