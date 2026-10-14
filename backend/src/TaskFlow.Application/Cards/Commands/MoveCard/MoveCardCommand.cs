using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Common;
using TaskFlow.Application.Common.Exceptions;
using TaskFlow.Application.Common.Interfaces;

namespace TaskFlow.Application.Cards.Commands.MoveCard;

public record MoveCardCommand(
    Guid BoardId,
    Guid CardId,
    Guid TargetListId,
    int NewOrder,
    byte[]? ExpectedRowVersion = null) : IRequest;

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
    private readonly IActivityWriter _activity;

    public MoveCardCommandHandler(
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

    public async Task Handle(MoveCardCommand request, CancellationToken cancellationToken)
    {
        await BoardAuthorization.EnsureCanEditAsync(_context, request.BoardId, _currentUser.UserId, cancellationToken);

        var card = await _context.Cards
            .Include(c => c.BoardList)
            .FirstOrDefaultAsync(c => c.Id == request.CardId && c.BoardList.BoardId == request.BoardId, cancellationToken)
            ?? throw new KeyNotFoundException($"Card {request.CardId} not found.");

        var targetList = await _context.BoardLists
            .FirstOrDefaultAsync(l => l.Id == request.TargetListId && l.BoardId == request.BoardId, cancellationToken)
            ?? throw new KeyNotFoundException($"Target list {request.TargetListId} not found.");

        var sourceListId = card.BoardListId;
        var title = card.Title;

        _context.ApplyExpectedCardRowVersion(card, request.ExpectedRowVersion);

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

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConcurrencyConflictException();
        }

        var summary = sourceListId == request.TargetListId
            ? $"reordered “{title}”"
            : $"moved “{title}” to {targetList.Title}";
        var activity = await _activity.WriteAsync(
            request.BoardId,
            _currentUser.UserId,
            "CardMoved",
            summary,
            card.Id,
            cancellationToken);
        await _notifier.NotifyActivityAddedAsync(request.BoardId, activity, cancellationToken);
        await _notifier.NotifyCardMovedAsync(
            request.BoardId, request.CardId, request.TargetListId, insertAt, cancellationToken);
    }
}