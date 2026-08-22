using FluentValidation;
using MediatR;
using TaskFlow.Application.Common;
using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Domain.Entities;

namespace TaskFlow.Application.Boards.Commands.CreateList;

public record CreateListCommand(Guid BoardId, string Title) : IRequest<Guid>;

public class CreateListCommandValidator : AbstractValidator<CreateListCommand>
{
    public CreateListCommandValidator()
    {
        RuleFor(x => x.BoardId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(100);
    }
}

public class CreateListCommandHandler : IRequestHandler<CreateListCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IBoardNotifier _notifier;

    public CreateListCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        IBoardNotifier notifier)
    {
        _context = context;
        _currentUser = currentUser;
        _notifier = notifier;
    }

    public async Task<Guid> Handle(CreateListCommand request, CancellationToken cancellationToken)
    {
        await BoardAuthorization.EnsureCanEditAsync(_context, request.BoardId, _currentUser.UserId, cancellationToken);

        var maxOrder = _context.BoardLists
            .Where(l => l.BoardId == request.BoardId)
            .Select(l => (int?)l.Order)
            .Max() ?? -1;

        var list = new BoardList
        {
            BoardId = request.BoardId,
            Title = request.Title,
            Order = maxOrder + 1,
        };

        _context.BoardLists.Add(list);
        await _context.SaveChangesAsync(cancellationToken);
        await _notifier.NotifyBoardChangedAsync(request.BoardId, "ListCreated", cancellationToken);

        return list.Id;
    }
}
