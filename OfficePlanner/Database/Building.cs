namespace OfficePlanner.Database;

public class Building
{
    public long Id { get; set; }
    public long LocationId { get; set; }
    public required string Name { get; set; }
    public required string LowercaseName { get; set; }

    public Location? Location { get; set; }
    public List<Floor> Floors { get; set; } = [];
}
