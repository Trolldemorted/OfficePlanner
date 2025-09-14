namespace OfficePlanner.Database;

public class Floor
{
    public long Id { get; set; }
    public required string Name { get; set; }
    public required string LowercaseName { get; set; }
    public required long BuildingId { get; set; }
    public required string FloorPlan { get; set; }

    public Building? Building { get; set; }
    public List<Room> Rooms { get; set; } = [];
}
