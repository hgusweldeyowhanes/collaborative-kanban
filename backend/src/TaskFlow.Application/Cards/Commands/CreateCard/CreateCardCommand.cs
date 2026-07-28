using FluentValidation;
using MediatR;
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

    public CreateCardCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(CreateCardCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            throw new UnauthorizedAccessException("No authenticated user.");

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

        return card.Id;
    }
}
