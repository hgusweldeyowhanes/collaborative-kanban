using MediatR;

namespace TaskFlow.Application.Boards.Commands.CreateBoard;

public record CreateBoardCommand(string Title, string? Description) : IRequest<Guid>;
