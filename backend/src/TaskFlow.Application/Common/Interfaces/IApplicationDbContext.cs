using Microsoft.EntityFrameworkCore;
using TaskFlow.Domain.Entities;

namespace TaskFlow.Application.Common.Interfaces;

// This interface is the Application layer's only dependency on persistence.
// Infrastructure implements it (ApplicationDbContext). Keeps Application
// testable without spinning up a real database.
public interface IApplicationDbContext
{
    DbSet<Board> Boards { get; }
    DbSet<BoardList> BoardLists { get; }
    DbSet<Card> Cards { get; }
    DbSet<AppUser> Users { get; }
    DbSet<BoardMember> BoardMembers { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
