using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Cards.Commands.AddComment;
using TaskFlow.Application.Common;
using TaskFlow.Application.Common.Interfaces;

namespace TaskFlow.Application.Cards.Queries.GetCardComments;

public record GetCardCommentsQuery(Guid BoardId, Guid CardId) : IRequest<List<CommentDto>>;

public class GetCardCommentsQueryHandler : IRequestHandler<GetCardCommentsQuery, List<CommentDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetCardCommentsQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<List<CommentDto>> Handle(GetCardCommentsQuery request, CancellationToken cancellationToken)
    {
        await BoardAuthorization.EnsureCanViewAsync(_context, request.BoardId, _currentUser.UserId, cancellationToken);

        var cardExists = await _context.Cards
            .AnyAsync(c => c.Id == request.CardId && c.BoardList.BoardId == request.BoardId, cancellationToken);
        if (!cardExists)
            throw new KeyNotFoundException($"Card {request.CardId} not found.");

        return await _context.CardComments
            .AsNoTracking()
            .Where(c => c.CardId == request.CardId)
            .OrderBy(c => c.CreatedAtUtc)
            .Select(c => new CommentDto(c.Id, c.AuthorId, c.Author.DisplayName, c.Body, c.CreatedAtUtc))
            .ToListAsync(cancellationToken);
    }
}
