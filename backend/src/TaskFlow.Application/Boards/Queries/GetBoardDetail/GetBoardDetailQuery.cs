using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Common;
using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Domain.Entities;

namespace TaskFlow.Application.Boards.Queries.GetBoardDetail;

public record LabelDto(Guid Id, string Name, string Color);
public record CardDto(
    Guid Id,
    string Title,
    string? Description,
    int Order,
    Guid? AssigneeId,
    string? AssigneeName,
    DateTime? DueDateUtc,
    List<Guid> LabelIds);
public record BoardListDto(Guid Id, string Title, int Order, List<CardDto> Cards);
public record BoardDetailDto(
    Guid Id,
    string Title,
    string? Description,
    List<BoardListDto> Lists,
    List<LabelDto> Labels,
    List<MemberSummaryDto> Members);

public record MemberSummaryDto(Guid UserId, string DisplayName, BoardRole Role);

public record GetBoardDetailQuery(Guid BoardId) : IRequest<BoardDetailDto>;

public class GetBoardDetailQueryHandler : IRequestHandler<GetBoardDetailQuery, BoardDetailDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetBoardDetailQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<BoardDetailDto> Handle(GetBoardDetailQuery request, CancellationToken cancellationToken)
    {
        await BoardAuthorization.EnsureCanViewAsync(_context, request.BoardId, _currentUser.UserId, cancellationToken);

        var board = await _context.Boards
            .Where(b => b.Id == request.BoardId)
            .Include(b => b.Owner)
            .Include(b => b.Labels)
            .Include(b => b.Members).ThenInclude(m => m.User)
            .Include(b => b.Lists).ThenInclude(l => l.Cards).ThenInclude(c => c.Assignee)
            .Include(b => b.Lists).ThenInclude(l => l.Cards).ThenInclude(c => c.CardLabels)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new KeyNotFoundException($"Board {request.BoardId} not found.");

        var lists = board.Lists
            .OrderBy(l => l.Order)
            .Select(l => new BoardListDto(
                l.Id,
                l.Title,
                l.Order,
                l.Cards.OrderBy(c => c.Order)
                    .Select(c => new CardDto(
                        c.Id,
                        c.Title,
                        c.Description,
                        c.Order,
                        c.AssigneeId,
                        c.Assignee?.DisplayName,
                        c.DueDateUtc,
                        c.CardLabels.Select(cl => cl.LabelId).ToList()))
                    .ToList()))
            .ToList();

        var labels = board.Labels
            .OrderBy(l => l.Name)
            .Select(l => new LabelDto(l.Id, l.Name, l.Color))
            .ToList();

        var members = board.Members
            .Select(m => new MemberSummaryDto(m.UserId, m.User.DisplayName, m.Role))
            .ToList();

        if (members.All(m => m.UserId != board.OwnerId))
        {
            members.Insert(0, new MemberSummaryDto(board.OwnerId, board.Owner.DisplayName, BoardRole.Admin));
        }

        return new BoardDetailDto(board.Id, board.Title, board.Description, lists, labels, members);
    }
}
