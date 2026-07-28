using TaskFlow.Domain.Common;

namespace TaskFlow.Domain.Entities;

public class BoardList : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public int Order { get; set; }

    public Guid BoardId { get; set; }
    public Board Board { get; set; } = null!;

    public ICollection<Card> Cards { get; set; } = new List<Card>();
}
