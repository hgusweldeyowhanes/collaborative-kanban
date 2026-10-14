using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskFlow.Domain.Entities;

namespace TaskFlow.Infrastructure.Persistence.Configurations;

public class BoardLabelConfiguration : IEntityTypeConfiguration<BoardLabel>
{
    public void Configure(EntityTypeBuilder<BoardLabel> builder)
    {
        builder.Property(l => l.Name).IsRequired().HasMaxLength(50);
        builder.Property(l => l.Color).IsRequired().HasMaxLength(20);

        builder.HasOne(l => l.Board)
            .WithMany(b => b.Labels)
            .HasForeignKey(l => l.BoardId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class CardLabelConfiguration : IEntityTypeConfiguration<CardLabel>
{
    public void Configure(EntityTypeBuilder<CardLabel> builder)
    {
        builder.HasKey(x => new { x.CardId, x.LabelId });

        builder.HasOne(x => x.Label)
            .WithMany(l => l.CardLabels)
            .HasForeignKey(x => x.LabelId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class CardCommentConfiguration : IEntityTypeConfiguration<CardComment>
{
    public void Configure(EntityTypeBuilder<CardComment> builder)
    {
        builder.Property(c => c.Body).IsRequired().HasMaxLength(2000);

        builder.HasOne(c => c.Author)
            .WithMany()
            .HasForeignKey(c => c.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class BoardActivityConfiguration : IEntityTypeConfiguration<BoardActivity>
{
    public void Configure(EntityTypeBuilder<BoardActivity> builder)
    {
        builder.Property(a => a.Action).IsRequired().HasMaxLength(64);
        builder.Property(a => a.Summary).IsRequired().HasMaxLength(500);
        builder.HasIndex(a => new { a.BoardId, a.CreatedAtUtc });

        builder.HasOne(a => a.Board)
            .WithMany()
            .HasForeignKey(a => a.BoardId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.Actor)
            .WithMany()
            .HasForeignKey(a => a.ActorUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class CardChecklistConfiguration : IEntityTypeConfiguration<CardChecklist>
{
    public void Configure(EntityTypeBuilder<CardChecklist> builder)
    {
        builder.Property(c => c.Title).IsRequired().HasMaxLength(200);

        builder.HasMany(c => c.Items)
            .WithOne(i => i.Checklist)
            .HasForeignKey(i => i.ChecklistId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class ChecklistItemConfiguration : IEntityTypeConfiguration<ChecklistItem>
{
    public void Configure(EntityTypeBuilder<ChecklistItem> builder)
    {
        builder.Property(i => i.Title).IsRequired().HasMaxLength(300);
    }
}
