using System.IdentityModel.Tokens.Jwt;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using TaskFlow.Domain.Entities;
using TaskFlow.Infrastructure.Identity;
using Xunit;

namespace TaskFlow.Application.Tests;

public class TokenServiceTests
{
    private readonly IConfiguration _configuration;

    public TokenServiceTests()
    {
        var inMemorySettings = new Dictionary<string, string?>
        {
            { "Jwt:Secret", "SUPER_SECRET_KEY_FOR_TESTING_PURPOSES_MUST_BE_AT_LEAST_32_CHARS" },
            { "Jwt:Issuer", "TaskFlow.Presentation" },
            { "Jwt:Audience", "TaskFlow.Client" }
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();
    }

    [Fact]
    public void GenerateAccessToken_ReturnsValidJwt_WithExpectedClaims()
    {
        // Arrange
        var tokenService = new TokenService(_configuration);
        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            Email = "testuser@example.com",
            DisplayName = "Test User"
        };

        // Act
        var token = tokenService.GenerateAccessToken(user);

        // Assert
        token.Should().NotBeNullOrWhiteSpace();

        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        jwtToken.Issuer.Should().Be("TaskFlow.Presentation");
        jwtToken.Audiences.Should().Contain("TaskFlow.Client");

        var subClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub);
        subClaim?.Value.Should().Be(user.Id.ToString());

        var emailClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Email);
        emailClaim?.Value.Should().Be("testuser@example.com");

        var nameClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "displayName");
        nameClaim?.Value.Should().Be("Test User");
    }

    [Fact]
    public void GenerateRefreshToken_ReturnsNonEmptyBase64String()
    {
        // Arrange
        var tokenService = new TokenService(_configuration);

        // Act
        var refreshToken = tokenService.GenerateRefreshToken();

        // Assert
        refreshToken.Should().NotBeNullOrWhiteSpace();

        var act = () => Convert.FromBase64String(refreshToken);
        act.Should().NotThrow();
    }
}
