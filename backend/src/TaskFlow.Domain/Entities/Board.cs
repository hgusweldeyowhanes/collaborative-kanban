using TaskFlow.Domain.Common;

namespace TaskFlow.Domain.Entities;

public class Board : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }

    public Guid OwnerId { get; set; }
    public AppUser Owner { get; set; } = null!;

    public ICollection<BoardList> Lists { get; set; } = new List<BoardList>();
    public ICollection<BoardMember> Members { get; set; } = new List<BoardMember>();
    public ICollection<BoardLabel> Labels { get; set; } = new List<BoardLabel>();
}
