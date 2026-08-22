namespace TaskFlow.Domain.Entities;

public class CardLabel
{
    public Guid CardId { get; set; }
    public Card Card { get; set; } = null!;

    public Guid LabelId { get; set; }
    public BoardLabel Label { get; set; } = null!;
}
