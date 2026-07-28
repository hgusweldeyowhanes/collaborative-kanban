using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using TaskFlow.Application.Common.Interfaces;

namespace TaskFlow.Infrastructure.Identity;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? UserId
    {
        get
        {
            var sub = _httpContextAccessor.HttpContext?.User?
                .FindFirstValue(ClaimTypes.NameIdentifier)
                ?? _httpContextAccessor.HttpContext?.User?.FindFirstValue("sub");

            return Guid.TryParse(sub, out var id) ? id : null;
        }
    }

    public string? Email =>
        _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Email);
}
