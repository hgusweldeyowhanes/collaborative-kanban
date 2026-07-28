using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskFlow.Application.Boards.Commands.CreateBoard;
using TaskFlow.Application.Boards.Commands.CreateList;
using TaskFlow.Application.Boards.Queries.GetBoards;
using TaskFlow.Application.Boards.Queries.GetBoardDetail;
using TaskFlow.Application.Cards.Commands.CreateCard;
using TaskFlow.Application.Cards.Commands.MoveCard;

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

    [HttpPost("cards/move")]
    public async Task<IActionResult> MoveCard(MoveCardCommand command, CancellationToken ct)
    {
        await _mediator.Send(command, ct);
        return NoContent();
    }
}

public record CreateListBody(string Title);
public record CreateCardBody(string Title);
