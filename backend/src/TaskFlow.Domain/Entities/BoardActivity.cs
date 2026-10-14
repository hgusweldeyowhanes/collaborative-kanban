using TaskFlow.Domain.Common;

namespace TaskFlow.Domain.Entities;

public class BoardActivity : BaseEntity
{
    public Guid BoardId { get; set; }
    public Board Board { get; set; } = null!;

    public Guid ActorUserId { get; set; }
    public AppUser Actor { get; set; } = null!;

    public Guid? CardId { get; set; }

    /// <summary>Stable action key, e.g. CardMoved, CommentAdded.</summary>
    public string Action { get; set; } = string.Empty;

    public string Summary { get; set; } = string.Empty;
}
