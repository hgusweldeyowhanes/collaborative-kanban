using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Common;
using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Domain.Entities;

namespace TaskFlow.Application.Cards.Commands.AddComment;

public record CommentDto(Guid Id, Guid AuthorId, string AuthorName, string Body, DateTime CreatedAtUtc);

public record AddCommentCommand(Guid BoardId, Guid CardId, string Body) : IRequest<CommentDto>;

public class AddCommentCommandValidator : AbstractValidator<AddCommentCommand>
{
    public AddCommentCommandValidator()
    {
        RuleFor(x => x.BoardId).NotEmpty();
        RuleFor(x => x.CardId).NotEmpty();
        RuleFor(x => x.Body).NotEmpty().MaximumLength(2000);
    }
}

public class AddCommentCommandHandler : IRequestHandler<AddCommentCommand, CommentDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IBoardNotifier _notifier;
    private readonly IActivityWriter _activity;

    public AddCommentCommandHandler(
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

    public async Task<CommentDto> Handle(AddCommentCommand request, CancellationToken cancellationToken)
    {
        await BoardAuthorization.EnsureCanEditAsync(_context, request.BoardId, _currentUser.UserId, cancellationToken);

        var userId = _currentUser.UserId!.Value;

        var cardExists = await _context.Cards
            .AnyAsync(c => c.Id == request.CardId && c.BoardList.BoardId == request.BoardId, cancellationToken);
        if (!cardExists)
            throw new KeyNotFoundException($"Card {request.CardId} not found.");

        var author = await _context.Users.FirstAsync(u => u.Id == userId, cancellationToken);

        var comment = new CardComment
        {
            CardId = request.CardId,
            AuthorId = userId,
            Body = request.Body.Trim()
        };

        _context.CardComments.Add(comment);
        await _context.SaveChangesAsync(cancellationToken);
        var activity = await _activity.WriteAsync(
            request.BoardId, userId, "CommentAdded", $"commented on a card", request.CardId, cancellationToken);
        await _notifier.NotifyActivityAddedAsync(request.BoardId, activity, cancellationToken);
        await _notifier.NotifyBoardChangedAsync(request.BoardId, "CommentAdded", cancellationToken);

        return new CommentDto(comment.Id, author.Id, author.DisplayName, comment.Body, comment.CreatedAtUtc);
    }
}
