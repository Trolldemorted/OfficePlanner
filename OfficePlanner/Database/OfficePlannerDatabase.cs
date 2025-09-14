using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using System.Drawing;

namespace OfficePlanner.Database;

public class OfficePlannerDatabase(OPDbContext dbContext, ILogger<OfficePlannerDatabase> logger)
{
    private readonly OPDbContext dbContext = dbContext;
    private readonly ILogger<OfficePlannerDatabase> logger = logger;
    #region Index
    public async Task DeleteLocation(string lowercaseName, CancellationToken token)
    {
        await this.dbContext.Locations
            .Where(e => e.LowercaseName == lowercaseName)
            .ExecuteDeleteAsync(token);
        await this.dbContext.SaveChangesAsync(token);
    }
    #endregion
    #region Location
    public async Task<Location?> GetLocation(string lowercaseName, CancellationToken token)
    {
        return await this.dbContext.Locations
            .Where(e => e.LowercaseName == lowercaseName)
            .Include(e => e.Buildings.OrderBy(e => e.LowercaseName))
            .AsNoTracking()
            .SingleOrDefaultAsync(token);
    }

    public async Task<Location?> CreateBuilding(string location, string building, CancellationToken token)
    {
        //TODO transaction, check whether it exists, ...
        var dbLocation = await this.dbContext.Locations
            .Where(e => e.LowercaseName == location)
            .Include(e => e.Buildings)
            .SingleOrDefaultAsync(token);

        if (dbLocation == null)
        {
            this.logger.LogWarning("CreateBuilding failed (could not find location)");
            return null;
        }

        dbLocation.Buildings.Add(new Building()
        {
            Location = dbLocation,
            Name = building,
            LowercaseName = building.ToLower(),
        });
        await this.dbContext.SaveChangesAsync(token);
        return dbLocation;
    }

    public async Task DeleteBuilding(string location, string building, CancellationToken token)
    {
        await this.dbContext.Buildings
            .Where(e => e.LowercaseName == building)
            .Where(e => e.Location!.LowercaseName == location)
            .ExecuteDeleteAsync(token);
        await this.dbContext.SaveChangesAsync(token);
    }
    #endregion
    #region Building
    public async Task<long?> GetBuildingId(string location, string building, CancellationToken token)
    {
        return await this.dbContext.Buildings
            .Where(e => e.LowercaseName == building)
            .Where(e => e.Location!.LowercaseName == location)
            .Select(e => e.Id)
            .SingleOrDefaultAsync(token);
    }

    public async Task<Building?> GetBuilding(string location, string building, CancellationToken token)
    {
        return await this.dbContext.Buildings
            .Where(e => e.LowercaseName == building)
            .Where(e => e.Location!.LowercaseName == location)
            .Include(e => e.Floors.OrderBy(e => e.Name))
            .AsNoTracking()
            .SingleOrDefaultAsync(token);
    }

    public async Task UpsertFloor(string location, string building, string floor, List<Room> newRooms, string floorPlan, CancellationToken token)
    {
        var lowercaseFloor = floor.ToLower();
        var oldFloor = await this.dbContext.Floors
            .Where(e => e.LowercaseName == lowercaseFloor)
            .Where(e => e.Building!.LowercaseName == building)
            .Where(e => e.Building!.Location!.LowercaseName == location)
            .Include(e => e.Rooms)
            .ThenInclude(e => e.Desks)
            .SingleOrDefaultAsync(token);

        if (oldFloor != null)
        {
            // Merging a new floor with an existing floor
            oldFloor.FloorPlan = floorPlan;
            foreach (var newRoom in newRooms)
            {
                var oldRoom = oldFloor.Rooms
                    .Where(e => e.LowercaseName == newRoom.LowercaseName)
                    .SingleOrDefault();
                if (oldRoom != null)
                {
                    // Add all desks from newRoom which are not in oldRoom
                    foreach (var newDesk in newRoom.Desks)
                    {
                        var oldDesk = oldRoom.Desks.Where(e => e.LowercaseName == newDesk.LowercaseName).SingleOrDefault();
                        if (oldDesk == null)
                        {
                            oldRoom.Desks.Add(newDesk);
                        }
                    }
                    // Remove all desks which are not in newRoom
                    oldRoom.Desks.RemoveAll(desk => !newRoom.Desks.Select(e => e.LowercaseName).Contains(desk.LowercaseName));
                }
                else
                {
                    // Add a new room with desks
                    oldFloor.Rooms.Add(newRoom);
                }
            }
            // Remove all rooms which are not in newRooms
            oldFloor.Rooms.RemoveAll(room => !newRooms.Select(e => e.LowercaseName).Contains(room.LowercaseName));
        }
        else
        {
            // Adding a new floor with new rooms and desks
            var buildingId = await this.dbContext.Buildings
                .Where(e => e.LowercaseName == building)
                .Where(e => e.Location!.LowercaseName == location)
                .Select(e => e.Id)
                .SingleAsync(token);

            this.dbContext.Floors.Add(new Floor()
            {
                FloorPlan = floorPlan,
                Name = floor,
                LowercaseName = lowercaseFloor,
                BuildingId = buildingId,
                Rooms = newRooms,
            });
        }

        await this.dbContext.SaveChangesAsync(token);
    }
    #endregion
    #region Floor
    public async Task<Floor?> GetFloor(string location, string building, string name, CancellationToken token)
    {
        return await this.dbContext.Floors
            .Where(e => e.LowercaseName == name)
            .Where(e => e.Building!.LowercaseName == building)
            .Where(e => e.Building!.Location!.LowercaseName == location)
            .Include(e => e.Rooms)
            .ThenInclude(e => e.Desks)
            .ThenInclude(e => e.Reservations)
            .ThenInclude(e => e.User)
            .AsNoTracking()
            .SingleOrDefaultAsync(token);
    }

    public async Task<Reservation?> UpsertReservation(string location, string building, string name, string room, DateOnly day, string desk, long userId, CancellationToken token)
    {
        var dbDesk = await this.dbContext.Desks
            .Where(e => e.LowercaseName == desk)
            .Where(e => e.Room!.LowercaseName == room)
            .Where(e => e.Room!.Floor!.LowercaseName == name)
            .Where(e => e.Room!.Floor!.Building!.LowercaseName == building)
            .Where(e => e.Room!.Floor!.Building!.Location!.LowercaseName == location)
            .SingleOrDefaultAsync(token);

        if (dbDesk == null)
        {
            return null;
        }

        var reservation = new Reservation()
        {
            Day = day,
            DeskId = dbDesk.Id,
            UserId = userId,
        };
        dbDesk.Reservations.Add(reservation);
        await this.dbContext.SaveChangesAsync(token);
        return reservation;
    }

    public async Task DeleteReservation(string location, string building, string name, string room, DateOnly day, string desk, long userId, CancellationToken token)
    {
        await this.dbContext.Reservations
            .Where(e => e.Day == day)
            .Where(e => e.Desk!.LowercaseName == desk)
            .Where(e => e.Desk!.Room!.LowercaseName == room)
            .Where(e => e.Desk!.Room!.Floor!.LowercaseName == name)
            .Where(e => e.Desk!.Room!.Floor!.Building!.LowercaseName == building)
            .Where(e => e.Desk!.Room!.Floor!.Building!.Location!.LowercaseName == location)
            .Where(e => e.UserId == userId)
            .ExecuteDeleteAsync(token);
    }
    #endregion
    public async Task<User> UpsertUser(string scheme, string sub, string name, CancellationToken token)
    {
        var user = await this.dbContext.Users
            .Where(e => e.Sub == sub)
            .SingleOrDefaultAsync(token);

        if (user != null)
        {
            user.Name = name;
        }
        else
        {
            user = new User()
            {
                Name = name,
                Sub = sub,
                AuthenticationScheme = scheme,
            };
            this.dbContext.Users.Add(user);
        }
        await this.dbContext.SaveChangesAsync(token);
        return user;
    }

    public async Task<User?> SetUserAdmin(long userId, bool value, CancellationToken requestAborted)
    {
        var user = await this.dbContext.Users
            .Where(e => e.Id == userId)
            .SingleOrDefaultAsync(requestAborted);
        if (user == null)
        {
            return null;
        }

        user.IsAdmin = value;
        await this.dbContext.SaveChangesAsync(requestAborted);
        return user;
    }

    public async Task<bool> IsUserAdmin(long userId, CancellationToken requestAborted)
    {
        return await this.dbContext.Users
            .AsNoTracking()
            .Where(e => e.Id == userId)
            .Select(e => e.IsAdmin)
            .SingleOrDefaultAsync(requestAborted);
    }

    public async Task<bool> IsUserAdmin(HttpContext context)
    {
        var userId = Util.GetUserId(context);
        if (userId == null)
        {
            return false;
        }
        return await IsUserAdmin(userId.Value, context.RequestAborted);
    }
}
