using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Common.Interfaces;
using TaskFlow.Domain.Entities;

namespace TaskFlow.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public const string CardConcurrencyProperty = "xmin";

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Board> Boards => Set<Board>();
    public DbSet<BoardList> BoardLists => Set<BoardList>();
    public DbSet<Card> Cards => Set<Card>();
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<BoardMember> BoardMembers => Set<BoardMember>();
    public DbSet<BoardLabel> BoardLabels => Set<BoardLabel>();
    public DbSet<CardLabel> CardLabels => Set<CardLabel>();
    public DbSet<CardComment> CardComments => Set<CardComment>();
    public DbSet<BoardActivity> BoardActivities => Set<BoardActivity>();
    public DbSet<CardChecklist> CardChecklists => Set<CardChecklist>();
    public DbSet<ChecklistItem> ChecklistItems => Set<ChecklistItem>();

    public string? GetCardRowVersion(Card card)
    {
        var entry = Entry(card);
        var prop = entry.Properties.FirstOrDefault(p => p.Metadata.Name == CardConcurrencyProperty);
        if (prop?.CurrentValue is null) return null;

        var xmin = Convert.ToUInt32(prop.CurrentValue);
        return Convert.ToBase64String(BitConverter.GetBytes(xmin));
    }

    public void ApplyExpectedCardRowVersion(Card card, byte[]? expectedRowVersion)
    {
        if (expectedRowVersion is null || expectedRowVersion.Length < 4) return;

        var entry = Entry(card);
        var prop = entry.Property(CardConcurrencyProperty);
        var xmin = BitConverter.ToUInt32(expectedRowVersion, 0);
        prop.OriginalValue = xmin;
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        base.OnModelCreating(builder);
    }
}
