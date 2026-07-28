using TaskFlow.Domain.Common;

namespace TaskFlow.Domain.Entities;

public class AppUser : BaseEntity
{
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;

    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiresAtUtc { get; set; }
}
