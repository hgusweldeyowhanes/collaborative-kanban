using FluentValidation;
using MediatR;
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

    public CreateListCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(CreateListCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            throw new UnauthorizedAccessException("No authenticated user.");

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

        return list.Id;
    }
}
