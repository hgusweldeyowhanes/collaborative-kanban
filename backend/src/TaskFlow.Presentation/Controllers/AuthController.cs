using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Domain.Entities;
using TaskFlow.Infrastructure.Identity;
using TaskFlow.Infrastructure.Persistence;

namespace TaskFlow.Presentation.Controllers;

public record RegisterRequest(string Email, string Password, string DisplayName);
public record LoginRequest(string Email, string Password);
public record RefreshRequest(string RefreshToken);
public record AuthResponse(string AccessToken, string RefreshToken);

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ITokenService _tokenService;

    public AuthController(ApplicationDbContext db, ITokenService tokenService)
    {
        _db = db;
        _tokenService = tokenService;
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request)
    {
        if (await _db.Users.AnyAsync(u => u.Email == request.Email))
            return Conflict(new { title = "Email already registered" });

        var user = new AppUser
        {
            Email = request.Email,
            DisplayName = request.DisplayName,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password)
        };

        var (access, refresh) = await IssueTokensAsync(user);
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return Ok(new AuthResponse(access, refresh));
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return Unauthorized(new { title = "Invalid credentials" });

        var (access, refresh) = await IssueTokensAsync(user);
        await _db.SaveChangesAsync();

        return Ok(new AuthResponse(access, refresh));
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponse>> Refresh(RefreshRequest request)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.RefreshToken == request.RefreshToken);

        if (user is null || user.RefreshTokenExpiresAtUtc < DateTime.UtcNow)
            return Unauthorized(new { title = "Invalid or expired refresh token" });

        var (access, refresh) = await IssueTokensAsync(user);
        await _db.SaveChangesAsync();

        return Ok(new AuthResponse(access, refresh));
    }

    private async Task<(string access, string refresh)> IssueTokensAsync(AppUser user)
    {
        var access = _tokenService.GenerateAccessToken(user);
        var refresh = _tokenService.GenerateRefreshToken();

        user.RefreshToken = refresh;
        user.RefreshTokenExpiresAtUtc = DateTime.UtcNow.AddDays(7);

        await Task.CompletedTask;
        return (access, refresh);
    }
}
