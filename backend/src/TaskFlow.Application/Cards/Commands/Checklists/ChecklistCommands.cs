using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Common;
using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Application.Cards.Queries.GetCardChecklists;
using TaskFlow.Domain.Entities;

namespace TaskFlow.Application.Cards.Commands.Checklists;

public record CreateChecklistCommand(Guid BoardId, Guid CardId, string Title) : IRequest<ChecklistDto>;
public record AddChecklistItemCommand(Guid BoardId, Guid CardId, Guid ChecklistId, string Title) : IRequest<ChecklistItemDto>;
public record ToggleChecklistItemCommand(Guid BoardId, Guid CardId, Guid ItemId) : IRequest<ChecklistItemDto>;
public record DeleteChecklistCommand(Guid BoardId, Guid CardId, Guid ChecklistId) : IRequest;
public record DeleteChecklistItemCommand(Guid BoardId, Guid CardId, Guid ItemId) : IRequest;

public class CreateChecklistCommandValidator : AbstractValidator<CreateChecklistCommand>
{
    public CreateChecklistCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
    }
}

public class AddChecklistItemCommandValidator : AbstractValidator<AddChecklistItemCommand>
{
    public AddChecklistItemCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
    }
}

public class CreateChecklistCommandHandler : IRequestHandler<CreateChecklistCommand, ChecklistDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IBoardNotifier _notifier;

    public CreateChecklistCommandHandler(
        IApplicationDbContext context, ICurrentUserService currentUser, IBoardNotifier notifier)
    {
        _context = context;
        _currentUser = currentUser;
        _notifier = notifier;
    }

    public async Task<ChecklistDto> Handle(CreateChecklistCommand request, CancellationToken cancellationToken)
    {
        await BoardAuthorization.EnsureCanEditAsync(_context, request.BoardId, _currentUser.UserId, cancellationToken);
        await EnsureCardOnBoard(request.BoardId, request.CardId, cancellationToken);

        var maxOrder = await _context.CardChecklists
            .Where(c => c.CardId == request.CardId)
            .Select(c => (int?)c.Order)
            .MaxAsync(cancellationToken) ?? -1;

        var checklist = new CardChecklist
        {
            CardId = request.CardId,
            Title = request.Title.Trim(),
            Order = maxOrder + 1
        };
        _context.CardChecklists.Add(checklist);
        await _context.SaveChangesAsync(cancellationToken);
        await _notifier.NotifyBoardChangedAsync(request.BoardId, "ChecklistUpdated", cancellationToken);

        return new ChecklistDto(checklist.Id, checklist.Title, checklist.Order, []);
    }

    private async Task EnsureCardOnBoard(Guid boardId, Guid cardId, CancellationToken ct)
    {
        var ok = await _context.Cards.AnyAsync(c => c.Id == cardId && c.BoardList.BoardId == boardId, ct);
        if (!ok) throw new KeyNotFoundException($"Card {cardId} not found.");
    }
}

public class AddChecklistItemCommandHandler : IRequestHandler<AddChecklistItemCommand, ChecklistItemDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IBoardNotifier _notifier;

    public AddChecklistItemCommandHandler(
        IApplicationDbContext context, ICurrentUserService currentUser, IBoardNotifier notifier)
    {
        _context = context;
        _currentUser = currentUser;
        _notifier = notifier;
    }

    public async Task<ChecklistItemDto> Handle(AddChecklistItemCommand request, CancellationToken cancellationToken)
    {
        await BoardAuthorization.EnsureCanEditAsync(_context, request.BoardId, _currentUser.UserId, cancellationToken);

        var checklist = await _context.CardChecklists
            .Include(c => c.Card).ThenInclude(c => c.BoardList)
            .FirstOrDefaultAsync(
                c => c.Id == request.ChecklistId && c.CardId == request.CardId && c.Card.BoardList.BoardId == request.BoardId,
                cancellationToken)
            ?? throw new KeyNotFoundException("Checklist not found.");

        var maxOrder = await _context.ChecklistItems
            .Where(i => i.ChecklistId == checklist.Id)
            .Select(i => (int?)i.Order)
            .MaxAsync(cancellationToken) ?? -1;

        var item = new ChecklistItem
        {
            ChecklistId = checklist.Id,
            Title = request.Title.Trim(),
            Order = maxOrder + 1
        };
        _context.ChecklistItems.Add(item);
        await _context.SaveChangesAsync(cancellationToken);
        await _notifier.NotifyBoardChangedAsync(request.BoardId, "ChecklistUpdated", cancellationToken);

        return new ChecklistItemDto(item.Id, item.Title, item.IsDone, item.Order);
    }
}

public class ToggleChecklistItemCommandHandler : IRequestHandler<ToggleChecklistItemCommand, ChecklistItemDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IBoardNotifier _notifier;

    public ToggleChecklistItemCommandHandler(
        IApplicationDbContext context, ICurrentUserService currentUser, IBoardNotifier notifier)
    {
        _context = context;
        _currentUser = currentUser;
        _notifier = notifier;
    }

    public async Task<ChecklistItemDto> Handle(ToggleChecklistItemCommand request, CancellationToken cancellationToken)
    {
        await BoardAuthorization.EnsureCanEditAsync(_context, request.BoardId, _currentUser.UserId, cancellationToken);

        var item = await _context.ChecklistItems
            .Include(i => i.Checklist).ThenInclude(c => c.Card).ThenInclude(c => c.BoardList)
            .FirstOrDefaultAsync(
                i => i.Id == request.ItemId &&
                     i.Checklist.CardId == request.CardId &&
                     i.Checklist.Card.BoardList.BoardId == request.BoardId,
                cancellationToken)
            ?? throw new KeyNotFoundException("Checklist item not found.");

        item.IsDone = !item.IsDone;
        item.UpdatedAtUtc = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
        await _notifier.NotifyBoardChangedAsync(request.BoardId, "ChecklistUpdated", cancellationToken);

        return new ChecklistItemDto(item.Id, item.Title, item.IsDone, item.Order);
    }
}

public class DeleteChecklistCommandHandler : IRequestHandler<DeleteChecklistCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IBoardNotifier _notifier;

    public DeleteChecklistCommandHandler(
        IApplicationDbContext context, ICurrentUserService currentUser, IBoardNotifier notifier)
    {
        _context = context;
        _currentUser = currentUser;
        _notifier = notifier;
    }

    public async Task Handle(DeleteChecklistCommand request, CancellationToken cancellationToken)
    {
        await BoardAuthorization.EnsureCanEditAsync(_context, request.BoardId, _currentUser.UserId, cancellationToken);

        var checklist = await _context.CardChecklists
            .Include(c => c.Card).ThenInclude(c => c.BoardList)
            .FirstOrDefaultAsync(
                c => c.Id == request.ChecklistId && c.CardId == request.CardId && c.Card.BoardList.BoardId == request.BoardId,
                cancellationToken)
            ?? throw new KeyNotFoundException("Checklist not found.");

        _context.CardChecklists.Remove(checklist);
        await _context.SaveChangesAsync(cancellationToken);
        await _notifier.NotifyBoardChangedAsync(request.BoardId, "ChecklistUpdated", cancellationToken);
    }
}

public class DeleteChecklistItemCommandHandler : IRequestHandler<DeleteChecklistItemCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IBoardNotifier _notifier;

    public DeleteChecklistItemCommandHandler(
        IApplicationDbContext context, ICurrentUserService currentUser, IBoardNotifier notifier)
    {
        _context = context;
        _currentUser = currentUser;
        _notifier = notifier;
    }

    public async Task Handle(DeleteChecklistItemCommand request, CancellationToken cancellationToken)
    {
        await BoardAuthorization.EnsureCanEditAsync(_context, request.BoardId, _currentUser.UserId, cancellationToken);

        var item = await _context.ChecklistItems
            .Include(i => i.Checklist).ThenInclude(c => c.Card).ThenInclude(c => c.BoardList)
            .FirstOrDefaultAsync(
                i => i.Id == request.ItemId &&
                     i.Checklist.CardId == request.CardId &&
                     i.Checklist.Card.BoardList.BoardId == request.BoardId,
                cancellationToken)
            ?? throw new KeyNotFoundException("Checklist item not found.");

        _context.ChecklistItems.Remove(item);
        await _context.SaveChangesAsync(cancellationToken);
        await _notifier.NotifyBoardChangedAsync(request.BoardId, "ChecklistUpdated", cancellationToken);
    }
}
