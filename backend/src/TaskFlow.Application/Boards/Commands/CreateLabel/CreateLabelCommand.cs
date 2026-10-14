using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Common;
using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Domain.Entities;

namespace TaskFlow.Application.Boards.Commands.CreateLabel;

public record CreateLabelCommand(Guid BoardId, string Name, string Color) : IRequest<Guid>;

public class CreateLabelCommandValidator : AbstractValidator<CreateLabelCommand>
{
    public CreateLabelCommandValidator()
    {
        RuleFor(x => x.BoardId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Color).NotEmpty().MaximumLength(20);
    }
}

public class CreateLabelCommandHandler : IRequestHandler<CreateLabelCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IBoardNotifier _notifier;
    private readonly IActivityWriter _activity;

    public CreateLabelCommandHandler(
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

    public async Task<Guid> Handle(CreateLabelCommand request, CancellationToken cancellationToken)
    {
        await BoardAuthorization.EnsureCanEditAsync(_context, request.BoardId, _currentUser.UserId, cancellationToken);

        var label = new BoardLabel
        {
            BoardId = request.BoardId,
            Name = request.Name.Trim(),
            Color = request.Color.Trim()
        };

        _context.BoardLabels.Add(label);
        await _context.SaveChangesAsync(cancellationToken);
        var activity = await _activity.WriteAsync(
            request.BoardId, _currentUser.UserId!, "LabelCreated", $"created label “{label.Name}”", null, cancellationToken);
        await _notifier.NotifyActivityAddedAsync(request.BoardId, activity, cancellationToken);
        await _notifier.NotifyBoardChangedAsync(request.BoardId, "LabelCreated", cancellationToken);
        return label.Id;
    }
}
