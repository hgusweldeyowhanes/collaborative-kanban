using TaskFlow.Domain.Common;

namespace TaskFlow.Domain.Entities;

public class CardChecklist : BaseEntity
{
    public Guid CardId { get; set; }
    public Card Card { get; set; } = null!;

    public string Title { get; set; } = string.Empty;
    public int Order { get; set; }

    public ICollection<ChecklistItem> Items { get; set; } = new List<ChecklistItem>();
}

public class ChecklistItem : BaseEntity
{
    public Guid ChecklistId { get; set; }
    public CardChecklist Checklist { get; set; } = null!;

    public string Title { get; set; } = string.Empty;
    public bool IsDone { get; set; }
    public int Order { get; set; }
}
