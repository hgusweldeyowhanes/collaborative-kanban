using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using TaskFlow.Application.Boards.Queries.GetBoards;
using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Domain.Entities;
using TaskFlow.Infrastructure.Persistence;
using Xunit;

namespace TaskFlow.Application.Tests;

public class GetBoardsQueryHandlerTests
{
    private static ApplicationDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task Handle_ReturnsBoards_OwnedOrMemberOf_OrderedByCreatedAtDesc()
    {
        // Arrange
        await using var db = CreateInMemoryContext();
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();

        var boardOwned = new Board
        {
            Id = Guid.NewGuid(),
            Title = "Owned Board",
            OwnerId = userId,
            CreatedAtUtc = DateTime.UtcNow.AddHours(-2)
        };

        var boardMember = new Board
        {
            Id = Guid.NewGuid(),
            Title = "Member Board",
            OwnerId = otherUserId,
            CreatedAtUtc = DateTime.UtcNow.AddHours(-1),
            Members = new List<BoardMember>
            {
                new BoardMember { UserId = userId, Role = BoardRole.Editor }
            }
        };

        var boardUnrelated = new Board
        {
            Id = Guid.NewGuid(),
            Title = "Unrelated Board",
            OwnerId = otherUserId,
            CreatedAtUtc = DateTime.UtcNow
        };

        db.Boards.AddRange(boardOwned, boardMember, boardUnrelated);
        await db.SaveChangesAsync();

        var currentUserMock = new Mock<ICurrentUserService>();
        currentUserMock.Setup(x => x.UserId).Returns(userId);

        var handler = new GetBoardsQueryHandler(db, currentUserMock.Object);

        // Act
        var result = await handler.Handle(new GetBoardsQuery(), CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result[0].Id.Should().Be(boardMember.Id); // Newer
        result[1].Id.Should().Be(boardOwned.Id);  // Older
    }

    [Fact]
    public async Task Handle_ReturnsEmptyList_WhenUserHasNoBoards()
    {
        // Arrange
        await using var db = CreateInMemoryContext();
        var currentUserMock = new Mock<ICurrentUserService>();
        currentUserMock.Setup(x => x.UserId).Returns(Guid.NewGuid());

        var handler = new GetBoardsQueryHandler(db, currentUserMock.Object);

        // Act
        var result = await handler.Handle(new GetBoardsQuery(), CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }
}
