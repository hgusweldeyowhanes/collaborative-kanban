using TaskFlow.Domain.Common;

namespace TaskFlow.Domain.Entities;

public class Card : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Order { get; set; }

    public Guid BoardListId { get; set; }
    public BoardList BoardList { get; set; } = null!;

    public Guid? AssigneeId { get; set; }
    public AppUser? Assignee { get; set; }
}
