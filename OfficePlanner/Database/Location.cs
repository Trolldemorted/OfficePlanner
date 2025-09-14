namespace OfficePlanner.Database;

public class Location
{
    public long Id { get; set; }
    public required string Name { get; set; }
    public required string LowercaseName { get; set; }

    public List<Building> Buildings { get; set; } = [];
}
