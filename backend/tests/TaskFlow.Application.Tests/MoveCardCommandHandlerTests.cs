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
        // Arrange
        await using var db = CreateInMemoryContext();
        var cardId = Guid.NewGuid();
        var boardId = Guid.NewGuid();
        var originalListId = Guid.NewGuid();
        var targetListId = Guid.NewGuid();

        var card = new Card
        {
            Id = cardId,
            Title = "Test Card",
            BoardListId = originalListId,
            Order = 0,
            CreatedAtUtc = DateTime.UtcNow
        };

        db.Cards.Add(card);
        await db.SaveChangesAsync();

        var notifierMock = new Mock<IBoardNotifier>();
        var handler = new MoveCardCommandHandler(db, notifierMock.Object);

        var command = new MoveCardCommand(boardId, cardId, targetListId, 2);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        var updatedCard = await db.Cards.FindAsync(cardId);
        updatedCard.Should().NotBeNull();
        updatedCard!.BoardListId.Should().Be(targetListId);
        updatedCard.Order.Should().Be(2);
        updatedCard.UpdatedAtUtc.Should().NotBeNull();

        notifierMock.Verify(n => n.NotifyCardMovedAsync(boardId, cardId, targetListId, 2, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ThrowsKeyNotFoundException_WhenCardDoesNotExist()
    {
        // Arrange
        await using var db = CreateInMemoryContext();
        var notifierMock = new Mock<IBoardNotifier>();
        var handler = new MoveCardCommandHandler(db, notifierMock.Object);

        var command = new MoveCardCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 0);

        // Act
        var act = () => handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }
}
