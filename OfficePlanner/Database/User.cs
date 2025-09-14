namespace OfficePlanner.Database;

public class User
{
    public long Id { get; set; }
    public required string AuthenticationScheme { get; set; }
    public required string Sub { get; set; }
    public required string Name { get; set; }
    public bool IsAdmin { get; set; } = false;
}
