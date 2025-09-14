namespace OfficePlanner.Database;

public class Desk
{
    public long Id { get; set; }
    public long RoomId { get; set; }
    public required string Name { get; set; }
    public required string LowercaseName { get; set; }

    public Room? Room { get; set; }
    public List<Reservation> Reservations { get; set; } = [];
}
