using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskFlow.Domain.Entities;

namespace TaskFlow.Infrastructure.Persistence.Configurations;

public class BoardListConfiguration : IEntityTypeConfiguration<BoardList>
{
    public void Configure(EntityTypeBuilder<BoardList> builder)
    {
        builder.Property(l => l.Title).IsRequired().HasMaxLength(100);

        builder.HasMany(l => l.Cards)
            .WithOne(c => c.BoardList)
            .HasForeignKey(c => c.BoardListId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class CardConfiguration : IEntityTypeConfiguration<Card>
{
    public void Configure(EntityTypeBuilder<Card> builder)
    {
        builder.Property(c => c.Title).IsRequired().HasMaxLength(200);
        builder.Property(c => c.Description).HasMaxLength(2000);

        builder.HasOne(c => c.Assignee)
            .WithMany()
            .HasForeignKey(c => c.AssigneeId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> builder)
    {
        builder.HasIndex(u => u.Email).IsUnique();
        builder.Property(u => u.Email).IsRequired().HasMaxLength(256);
        builder.Property(u => u.DisplayName).IsRequired().HasMaxLength(100);
    }
}

public class BoardMemberConfiguration : IEntityTypeConfiguration<BoardMember>
{
    public void Configure(EntityTypeBuilder<BoardMember> builder)
    {
        builder.HasIndex(m => new { m.BoardId, m.UserId }).IsUnique();

        builder.HasOne(m => m.User)
            .WithMany()
            .HasForeignKey(m => m.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
