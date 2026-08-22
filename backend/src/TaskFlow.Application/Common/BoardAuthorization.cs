using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Domain.Entities;

namespace TaskFlow.Application.Common;

public static class BoardAuthorization
{
    public static async Task EnsureCanViewAsync(
        IApplicationDbContext db,
        Guid boardId,
        Guid? userId,
        CancellationToken ct = default)
    {
        var role = await GetRoleAsync(db, boardId, userId, ct);
        if (role is null)
            throw new UnauthorizedAccessException("Not a board member.");
    }

    public static async Task EnsureCanEditAsync(
        IApplicationDbContext db,
        Guid boardId,
        Guid? userId,
        CancellationToken ct = default)
    {
        var role = await GetRoleAsync(db, boardId, userId, ct);
        if (role is null || role < BoardRole.Editor)
            throw new UnauthorizedAccessException("Editor role required.");
    }

    public static async Task EnsureAdminAsync(
        IApplicationDbContext db,
        Guid boardId,
        Guid? userId,
        CancellationToken ct = default)
    {
        var role = await GetRoleAsync(db, boardId, userId, ct);
        if (role is null || role < BoardRole.Admin)
            throw new UnauthorizedAccessException("Admin role required.");
    }

    public static async Task<BoardRole?> GetRoleAsync(
        IApplicationDbContext db,
        Guid boardId,
        Guid? userId,
        CancellationToken ct = default)
    {
        if (userId is null)
            return null;

        var info = await db.Boards
            .AsNoTracking()
            .Where(b => b.Id == boardId)
            .Select(b => new
            {
                b.OwnerId,
                MemberRole = b.Members
                    .Where(m => m.UserId == userId)
                    .Select(m => (BoardRole?)m.Role)
                    .FirstOrDefault()
            })
            .FirstOrDefaultAsync(ct);

        if (info is null)
            return null;

        if (info.OwnerId == userId)
            return BoardRole.Admin;

        return info.MemberRole;
    }
}
