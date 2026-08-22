using TaskFlow.Domain.Common;

namespace TaskFlow.Domain.Entities;

public class CardComment : BaseEntity
{
    public Guid CardId { get; set; }
    public Card Card { get; set; } = null!;

    public Guid AuthorId { get; set; }
    public AppUser Author { get; set; } = null!;

    public string Body { get; set; } = string.Empty;
}
