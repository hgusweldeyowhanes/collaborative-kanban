using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Moq;
using TaskFlow.Application.Common.Behaviors;
using Xunit;

namespace TaskFlow.Application.Tests;

public class ValidationBehaviorTests
{
    public record TestRequest(string Name) : IRequest<string>;

    [Fact]
    public async Task Handle_CallsNext_WhenNoValidatorsProvided()
    {
        // Arrange
        var validators = Enumerable.Empty<IValidator<TestRequest>>();
        var behavior = new ValidationBehavior<TestRequest, string>(validators);
        var request = new TestRequest("Sample");
        RequestHandlerDelegate<string> next = () => Task.FromResult("Success");

        // Act
        var result = await behavior.Handle(request, next, CancellationToken.None);

        // Assert
        result.Should().Be("Success");
    }

    [Fact]
    public async Task Handle_CallsNext_WhenValidationPasses()
    {
        // Arrange
        var validatorMock = new Mock<IValidator<TestRequest>>();
        validatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<TestRequest>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        var behavior = new ValidationBehavior<TestRequest, string>(new[] { validatorMock.Object });
        var request = new TestRequest("Valid");
        RequestHandlerDelegate<string> next = () => Task.FromResult("Success");

        // Act
        var result = await behavior.Handle(request, next, CancellationToken.None);

        // Assert
        result.Should().Be("Success");
    }

    [Fact]
    public async Task Handle_ThrowsValidationException_WhenValidationFails()
    {
        // Arrange
        var failure = new ValidationFailure("Name", "Name is required");
        var validatorMock = new Mock<IValidator<TestRequest>>();
        validatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<TestRequest>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(new[] { failure }));

        var behavior = new ValidationBehavior<TestRequest, string>(new[] { validatorMock.Object });
        var request = new TestRequest("");
        RequestHandlerDelegate<string> next = () => Task.FromResult("Success");

        // Act
        var act = () => behavior.Handle(request, next, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .Where(e => e.Errors.Any(err => err.PropertyName == "Name"));
    }
}
