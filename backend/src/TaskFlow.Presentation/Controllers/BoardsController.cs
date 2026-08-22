using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskFlow.Application.Boards.Commands.CreateBoard;
using TaskFlow.Application.Boards.Commands.CreateLabel;
using TaskFlow.Application.Boards.Commands.CreateList;
using TaskFlow.Application.Boards.Commands.InviteMember;
using TaskFlow.Application.Boards.Queries.GetBoardDetail;
using TaskFlow.Application.Boards.Queries.GetBoardMembers;
using TaskFlow.Application.Boards.Queries.GetBoards;
using TaskFlow.Application.Cards.Commands.AddComment;
using TaskFlow.Application.Cards.Commands.CreateCard;
using TaskFlow.Application.Cards.Commands.DeleteCard;
using TaskFlow.Application.Cards.Commands.MoveCard;
using TaskFlow.Application.Cards.Commands.UpdateCard;
using TaskFlow.Application.Cards.Queries.GetCardComments;
using TaskFlow.Domain.Entities;

namespace TaskFlow.Presentation.Controllers;

[ApiController]
[Authorize]
[Route("api/boards")]
public class BoardsController : ControllerBase
{
    private readonly IMediator _mediator;

    public BoardsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<List<BoardSummaryDto>>> GetBoards(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetBoardsQuery(), ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BoardDetailDto>> GetBoard(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetBoardDetailQuery(id), ct);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<Guid>> CreateBoard(CreateBoardCommand command, CancellationToken ct)
    {
        var id = await _mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetBoard), new { id }, id);
    }

    [HttpPost("{id:guid}/lists")]
    public async Task<ActionResult<Guid>> CreateList(Guid id, [FromBody] CreateListBody body, CancellationToken ct)
    {
        var listId = await _mediator.Send(new CreateListCommand(id, body.Title), ct);
        return CreatedAtAction(nameof(GetBoard), new { id }, listId);
    }

    [HttpPost("{id:guid}/lists/{listId:guid}/cards")]
    public async Task<ActionResult<Guid>> CreateCard(Guid id, Guid listId, [FromBody] CreateCardBody body, CancellationToken ct)
    {
        var cardId = await _mediator.Send(new CreateCardCommand(id, listId, body.Title), ct);
        return CreatedAtAction(nameof(GetBoard), new { id }, cardId);
    }

    [HttpPut("{id:guid}/cards/{cardId:guid}")]
    public async Task<IActionResult> UpdateCard(Guid id, Guid cardId, [FromBody] UpdateCardBody body, CancellationToken ct)
    {
        await _mediator.Send(new UpdateCardCommand(
            id, cardId, body.Title, body.Description, body.AssigneeId, body.DueDateUtc, body.LabelIds), ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}/cards/{cardId:guid}")]
    public async Task<IActionResult> DeleteCard(Guid id, Guid cardId, CancellationToken ct)
    {
        await _mediator.Send(new DeleteCardCommand(id, cardId), ct);
        return NoContent();
    }

    [HttpPost("cards/move")]
    public async Task<IActionResult> MoveCard(MoveCardCommand command, CancellationToken ct)
    {
        await _mediator.Send(command, ct);
        return NoContent();
    }

    [HttpGet("{id:guid}/members")]
    public async Task<ActionResult<List<BoardMemberDto>>> GetMembers(Guid id, CancellationToken ct)
    {
        return Ok(await _mediator.Send(new GetBoardMembersQuery(id), ct));
    }

    [HttpPost("{id:guid}/members")]
    public async Task<ActionResult<Guid>> InviteMember(Guid id, [FromBody] InviteMemberBody body, CancellationToken ct)
    {
        var memberId = await _mediator.Send(new InviteMemberCommand(id, body.Email, body.Role), ct);
        return Ok(memberId);
    }

    [HttpPost("{id:guid}/labels")]
    public async Task<ActionResult<Guid>> CreateLabel(Guid id, [FromBody] CreateLabelBody body, CancellationToken ct)
    {
        var labelId = await _mediator.Send(new CreateLabelCommand(id, body.Name, body.Color), ct);
        return Ok(labelId);
    }

    [HttpGet("{id:guid}/cards/{cardId:guid}/comments")]
    public async Task<ActionResult<List<CommentDto>>> GetComments(Guid id, Guid cardId, CancellationToken ct)
    {
        return Ok(await _mediator.Send(new GetCardCommentsQuery(id, cardId), ct));
    }

    [HttpPost("{id:guid}/cards/{cardId:guid}/comments")]
    public async Task<ActionResult<CommentDto>> AddComment(
        Guid id, Guid cardId, [FromBody] AddCommentBody body, CancellationToken ct)
    {
        var comment = await _mediator.Send(new AddCommentCommand(id, cardId, body.Body), ct);
        return Ok(comment);
    }
}

public record CreateListBody(string Title);
public record CreateCardBody(string Title);
public record UpdateCardBody(
    string Title,
    string? Description,
    Guid? AssigneeId,
    DateTime? DueDateUtc,
    List<Guid>? LabelIds);
public record InviteMemberBody(string Email, BoardRole Role);
public record CreateLabelBody(string Name, string Color);
public record AddCommentBody(string Body);
