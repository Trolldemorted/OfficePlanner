namespace OfficePlanner.Database;

public class Room
{
    public long Id { get; set; }
    public long FloorId { get; set; }
    public required string Name { get; set; }
    public required string LowercaseName { get; set; }

    public Floor? Floor { get; set; }
    public List<Desk> Desks { get; set; } = [];
}
