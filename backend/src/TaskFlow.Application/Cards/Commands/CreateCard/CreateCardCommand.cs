using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Common;
using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Domain.Entities;

namespace TaskFlow.Application.Cards.Commands.CreateCard;

public record CreateCardCommand(Guid BoardId, Guid ListId, string Title) : IRequest<Guid>;

public class CreateCardCommandValidator : AbstractValidator<CreateCardCommand>
{
    public CreateCardCommandValidator()
    {
        RuleFor(x => x.BoardId).NotEmpty();
        RuleFor(x => x.ListId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
    }
}

public class CreateCardCommandHandler : IRequestHandler<CreateCardCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IBoardNotifier _notifier;
    private readonly IActivityWriter _activity;

    public CreateCardCommandHandler(
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

    public async Task<Guid> Handle(CreateCardCommand request, CancellationToken cancellationToken)
    {
        await BoardAuthorization.EnsureCanEditAsync(_context, request.BoardId, _currentUser.UserId, cancellationToken);

        var listOk = await _context.BoardLists
            .AnyAsync(l => l.Id == request.ListId && l.BoardId == request.BoardId, cancellationToken);
        if (!listOk)
            throw new KeyNotFoundException($"List {request.ListId} not found on board.");

        var maxOrder = _context.Cards
            .Where(c => c.BoardListId == request.ListId)
            .Select(c => (int?)c.Order)
            .Max() ?? -1;

        var card = new Card
        {
            BoardListId = request.ListId,
            Title = request.Title,
            Order = maxOrder + 1,
        };

        _context.Cards.Add(card);
        await _context.SaveChangesAsync(cancellationToken);
        var activity = await _activity.WriteAsync(
            request.BoardId, _currentUser.UserId!, "CardCreated", $"created “{card.Title}”", card.Id, cancellationToken);
        await _notifier.NotifyActivityAddedAsync(request.BoardId, activity, cancellationToken);
        await _notifier.NotifyBoardChangedAsync(request.BoardId, "CardCreated", cancellationToken);

        return card.Id;
    }
}
