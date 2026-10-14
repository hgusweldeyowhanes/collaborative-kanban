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
    DbSet<BoardLabel> BoardLabels { get; }
    DbSet<CardLabel> CardLabels { get; }
    DbSet<CardComment> CardComments { get; }
    DbSet<BoardActivity> BoardActivities { get; }
    DbSet<CardChecklist> CardChecklists { get; }
    DbSet<ChecklistItem> ChecklistItems { get; }

    /// <summary>Base64 concurrency token for a tracked card (Postgres xmin), or null.</summary>
    string? GetCardRowVersion(Card card);

    /// <summary>Sets the original concurrency token before SaveChanges for optimistic concurrency.</summary>
    void ApplyExpectedCardRowVersion(Card card, byte[]? expectedRowVersion);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
