using FluentAssertions;
using TaskFlow.Application.Cards.Commands.MoveCard;
using Xunit;

namespace TaskFlow.Application.Tests;

public class MoveCardCommandValidatorTests
{
    private readonly MoveCardCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_PassesValidation()
    {
        // Arrange
        var command = new MoveCardCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 0);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_EmptyBoardId_FailsValidation()
    {
        // Arrange
        var command = new MoveCardCommand(Guid.Empty, Guid.NewGuid(), Guid.NewGuid(), 0);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(MoveCardCommand.BoardId));
    }

    [Fact]
    public void Validate_EmptyCardId_FailsValidation()
    {
        // Arrange
        var command = new MoveCardCommand(Guid.NewGuid(), Guid.Empty, Guid.NewGuid(), 0);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(MoveCardCommand.CardId));
    }

    [Fact]
    public void Validate_EmptyTargetListId_FailsValidation()
    {
        // Arrange
        var command = new MoveCardCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.Empty, 0);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(MoveCardCommand.TargetListId));
    }

    [Fact]
    public void Validate_NegativeOrder_FailsValidation()
    {
        // Arrange
        var command = new MoveCardCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), -1);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(MoveCardCommand.NewOrder));
    }
}
