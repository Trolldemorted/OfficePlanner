namespace OfficePlanner.Database;

public class Reservation
{
    public long Id { get; set; }
    public long DeskId { get; set; }
    public long UserId { get; set; }
    public DateOnly Day { get; set; }

    public Desk? Desk { get; set; }
    public User? User { get; set; }
}
