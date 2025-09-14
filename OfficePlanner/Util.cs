namespace OfficePlanner;

public class Util
{
    public const string USERID_KEY = "OfficePlanner:userid";

    public static long? GetUserId(HttpContext context)
    {
        string? userIdString = context.User.FindFirst(USERID_KEY)?.Value;
        if (long.TryParse(userIdString, out var userId))
        {
            return userId;
        }
        return null;
    }
}
