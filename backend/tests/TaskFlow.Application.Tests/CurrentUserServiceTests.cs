using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;
using TaskFlow.Infrastructure.Identity;
using Xunit;

namespace TaskFlow.Application.Tests;

public class CurrentUserServiceTests
{
    [Fact]
    public void UserId_ReturnsGuid_WhenNameIdentifierClaimExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Email, "user@example.com")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        var httpContextMock = new Mock<HttpContext>();
        httpContextMock.Setup(c => c.User).Returns(claimsPrincipal);

        var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        httpContextAccessorMock.Setup(a => a.HttpContext).Returns(httpContextMock.Object);

        var service = new CurrentUserService(httpContextAccessorMock.Object);

        // Act & Assert
        service.UserId.Should().Be(userId);
        service.Email.Should().Be("user@example.com");
    }

    [Fact]
    public void UserId_ReturnsGuid_WhenSubClaimExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var claims = new[]
        {
            new Claim("sub", userId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        var httpContextMock = new Mock<HttpContext>();
        httpContextMock.Setup(c => c.User).Returns(claimsPrincipal);

        var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        httpContextAccessorMock.Setup(a => a.HttpContext).Returns(httpContextMock.Object);

        var service = new CurrentUserService(httpContextAccessorMock.Object);

        // Act & Assert
        service.UserId.Should().Be(userId);
    }

    [Fact]
    public void UserId_ReturnsNull_WhenNoUserOrHttpContext()
    {
        // Arrange
        var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        httpContextAccessorMock.Setup(a => a.HttpContext).Returns((HttpContext?)null);

        var service = new CurrentUserService(httpContextAccessorMock.Object);

        // Act & Assert
        service.UserId.Should().BeNull();
        service.Email.Should().BeNull();
    }
}
