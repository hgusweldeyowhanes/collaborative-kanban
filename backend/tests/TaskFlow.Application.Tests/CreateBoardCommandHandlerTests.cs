using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using TaskFlow.Application.Boards.Commands.CreateBoard;
using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Infrastructure.Persistence;
using Xunit;

namespace TaskFlow.Application.Tests;

public class CreateBoardCommandHandlerTests
{
    private static ApplicationDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task Handle_CreatesBoard_WithAuthenticatedUserAsOwnerAndAdminMember()
    {
        // Arrange
        await using var db = CreateInMemoryContext();
        var userId = Guid.NewGuid();

        var currentUserMock = new Mock<ICurrentUserService>();
        currentUserMock.Setup(x => x.UserId).Returns(userId);

        var handler = new CreateBoardCommandHandler(db, currentUserMock.Object);
        var command = new CreateBoardCommand("Sprint Planning", "Q3 sprint board");

        // Act
        var boardId = await handler.Handle(command, CancellationToken.None);

        // Assert
        var board = await db.Boards.Include(b => b.Members).FirstAsync(b => b.Id == boardId);

        board.Title.Should().Be("Sprint Planning");
        board.OwnerId.Should().Be(userId);
        board.Members.Should().ContainSingle(m => m.UserId == userId && m.Role == Domain.Entities.BoardRole.Admin);
    }

    [Fact]
    public async Task Handle_ThrowsUnauthorized_WhenNoCurrentUser()
    {
        // Arrange
        await using var db = CreateInMemoryContext();

        var currentUserMock = new Mock<ICurrentUserService>();
        currentUserMock.Setup(x => x.UserId).Returns((Guid?)null);

        var handler = new CreateBoardCommandHandler(db, currentUserMock.Object);
        var command = new CreateBoardCommand("Untitled", null);

        // Act
        var act = () => handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }
}
