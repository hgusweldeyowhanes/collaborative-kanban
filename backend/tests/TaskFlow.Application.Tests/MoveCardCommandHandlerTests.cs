using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using TaskFlow.Application.Cards.Commands.MoveCard;
using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Domain.Entities;
using TaskFlow.Infrastructure.Persistence;
using Xunit;

namespace TaskFlow.Application.Tests;

public class MoveCardCommandHandlerTests
{
    private static ApplicationDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task Handle_MovesCard_AndNotifiesClients()
    {
        await using var db = CreateInMemoryContext();
        var userId = Guid.NewGuid();
        var cardId = Guid.NewGuid();
        var boardId = Guid.NewGuid();
        var originalListId = Guid.NewGuid();
        var targetListId = Guid.NewGuid();

        db.Users.Add(new AppUser
        {
            Id = userId,
            Email = "a@test.com",
            DisplayName = "A",
            PasswordHash = "x"
        });
        db.Boards.Add(new Board
        {
            Id = boardId,
            Title = "B",
            OwnerId = userId,
            Members = { new BoardMember { UserId = userId, Role = BoardRole.Admin } }
        });
        db.BoardLists.AddRange(
            new BoardList { Id = originalListId, BoardId = boardId, Title = "Todo", Order = 0 },
            new BoardList { Id = targetListId, BoardId = boardId, Title = "Done", Order = 1 });
        db.Cards.AddRange(
            new Card { Id = cardId, Title = "Test Card", BoardListId = originalListId, Order = 0 },
            new Card { Id = Guid.NewGuid(), Title = "Other", BoardListId = targetListId, Order = 0 },
            new Card { Id = Guid.NewGuid(), Title = "Other2", BoardListId = targetListId, Order = 1 });
        await db.SaveChangesAsync();

        var notifierMock = new Mock<IBoardNotifier>();
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(c => c.UserId).Returns(userId);

        var handler = new MoveCardCommandHandler(db, currentUser.Object, notifierMock.Object);
        var command = new MoveCardCommand(boardId, cardId, targetListId, 2);

        await handler.Handle(command, CancellationToken.None);

        var updatedCard = await db.Cards.FindAsync(cardId);
        updatedCard.Should().NotBeNull();
        updatedCard!.BoardListId.Should().Be(targetListId);
        updatedCard.Order.Should().Be(2);

        notifierMock.Verify(
            n => n.NotifyCardMovedAsync(boardId, cardId, targetListId, 2, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ThrowsKeyNotFoundException_WhenCardDoesNotExist()
    {
        await using var db = CreateInMemoryContext();
        var userId = Guid.NewGuid();
        var boardId = Guid.NewGuid();
        db.Users.Add(new AppUser { Id = userId, Email = "a@test.com", DisplayName = "A", PasswordHash = "x" });
        db.Boards.Add(new Board
        {
            Id = boardId,
            Title = "B",
            OwnerId = userId,
            Members = { new BoardMember { UserId = userId, Role = BoardRole.Editor } }
        });
        await db.SaveChangesAsync();

        var notifierMock = new Mock<IBoardNotifier>();
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(c => c.UserId).Returns(userId);
        var handler = new MoveCardCommandHandler(db, currentUser.Object, notifierMock.Object);

        var command = new MoveCardCommand(boardId, Guid.NewGuid(), Guid.NewGuid(), 0);
        var act = () => handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }
}
