using TaskFlow.Domain.Common;

namespace TaskFlow.Domain.Entities;

public class BoardLabel : BaseEntity
{
    public Guid BoardId { get; set; }
    public Board Board { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = "#6366f1";

    public ICollection<CardLabel> CardLabels { get; set; } = new List<CardLabel>();
}
