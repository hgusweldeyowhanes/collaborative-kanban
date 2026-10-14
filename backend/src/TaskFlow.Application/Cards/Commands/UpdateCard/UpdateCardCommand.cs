using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Common;
using TaskFlow.Application.Common.Exceptions;
using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Domain.Entities;

namespace TaskFlow.Application.Cards.Commands.UpdateCard;

public record UpdateCardCommand(
    Guid BoardId,
    Guid CardId,
    string Title,
    string? Description,
    Guid? AssigneeId,
    DateTime? DueDateUtc,
    List<Guid>? LabelIds,
    byte[]? ExpectedRowVersion
) : IRequest;

public class UpdateCardCommandValidator : AbstractValidator<UpdateCardCommand>
{
    public UpdateCardCommandValidator()
    {
        RuleFor(x => x.BoardId).NotEmpty();
        RuleFor(x => x.CardId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(4000);
    }
}

public class UpdateCardCommandHandler : IRequestHandler<UpdateCardCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IBoardNotifier _notifier;
    private readonly IActivityWriter _activity;

    public UpdateCardCommandHandler(
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

    public async Task Handle(UpdateCardCommand request, CancellationToken cancellationToken)
    {
        await BoardAuthorization.EnsureCanEditAsync(_context, request.BoardId, _currentUser.UserId, cancellationToken);

        var card = await _context.Cards
            .Include(c => c.CardLabels)
            .Include(c => c.BoardList)
            .FirstOrDefaultAsync(c => c.Id == request.CardId && c.BoardList.BoardId == request.BoardId, cancellationToken)
            ?? throw new KeyNotFoundException($"Card {request.CardId} not found.");

        if (request.AssigneeId is Guid assigneeId)
        {
            var assigneeOk = await _context.Boards.AnyAsync(
                b => b.Id == request.BoardId &&
                     (b.OwnerId == assigneeId || b.Members.Any(m => m.UserId == assigneeId)),
                cancellationToken);
            if (!assigneeOk)
                throw new InvalidOperationException("Assignee must be a board member.");
        }

        _context.ApplyExpectedCardRowVersion(card, request.ExpectedRowVersion);

        card.Title = request.Title;
        card.Description = request.Description;
        card.AssigneeId = request.AssigneeId;
        card.DueDateUtc = request.DueDateUtc;
        card.UpdatedAtUtc = DateTime.UtcNow;

        if (request.LabelIds is not null)
        {
            var validLabelIds = await _context.BoardLabels
                .Where(l => l.BoardId == request.BoardId && request.LabelIds.Contains(l.Id))
                .Select(l => l.Id)
                .ToListAsync(cancellationToken);

            card.CardLabels.Clear();
            foreach (var labelId in validLabelIds.Distinct())
            {
                card.CardLabels.Add(new CardLabel { CardId = card.Id, LabelId = labelId });
            }
        }

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConcurrencyConflictException();
        }

        var activity = await _activity.WriteAsync(
            request.BoardId,
            _currentUser.UserId,
            "CardUpdated",
            $"updated “{card.Title}”",
            card.Id,
            cancellationToken);
        await _notifier.NotifyActivityAddedAsync(request.BoardId, activity, cancellationToken);
        await _notifier.NotifyBoardChangedAsync(request.BoardId, "CardUpdated", cancellationToken);
    }
}
