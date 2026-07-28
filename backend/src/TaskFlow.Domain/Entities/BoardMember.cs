using TaskFlow.Domain.Common;

namespace TaskFlow.Domain.Entities;

public enum BoardRole
{
    Viewer = 0,
    Editor = 1,
    Admin = 2
}

public class BoardMember : BaseEntity
{
    public Guid BoardId { get; set; }
    public Board Board { get; set; } = null!;

    public Guid UserId { get; set; }
    public AppUser User { get; set; } = null!;

    public BoardRole Role { get; set; } = BoardRole.Editor;
}
