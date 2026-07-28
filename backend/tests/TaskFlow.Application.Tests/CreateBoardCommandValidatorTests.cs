using FluentAssertions;
using TaskFlow.Application.Boards.Commands.CreateBoard;
using Xunit;

namespace TaskFlow.Application.Tests;

public class CreateBoardCommandValidatorTests
{
    private readonly CreateBoardCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_PassesValidation()
    {
        // Arrange
        var command = new CreateBoardCommand("Project Alpha", "Description here");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validate_EmptyOrNullTitle_FailsValidation(string? title)
    {
        // Arrange
        var command = new CreateBoardCommand(title!, "Description");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(CreateBoardCommand.Title));
    }

    [Fact]
    public void Validate_TitleExceedingMaxLength_FailsValidation()
    {
        // Arrange
        var command = new CreateBoardCommand(new string('a', 101), "Description");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(CreateBoardCommand.Title));
    }

    [Fact]
    public void Validate_DescriptionExceedingMaxLength_FailsValidation()
    {
        // Arrange
        var command = new CreateBoardCommand("Valid Title", new string('a', 1001));

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(CreateBoardCommand.Description));
    }
}
