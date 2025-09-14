using Microsoft.EntityFrameworkCore;

namespace OfficePlanner.Database;

public class OPDbContext(DbContextOptions<OPDbContext> options) : DbContext(options)
{
    public DbSet<Building> Buildings { get; set; }
    public DbSet<Desk> Desks { get; set; }
    public DbSet<Floor> Floors { get; set; }
    public DbSet<Location> Locations { get; set; }
    public DbSet<Reservation> Reservations { get; set; }
    public DbSet<Room> Rooms { get; set; }
    public DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Building>()
            .HasIndex(e => new { e.LocationId, e.LowercaseName })
            .IsUnique();

        modelBuilder.Entity<Desk>()
            .HasIndex(e => new { e.RoomId, e.LowercaseName })
            .IsUnique();

        modelBuilder.Entity<Floor>()
            .HasIndex(e => new { e.BuildingId, e.LowercaseName })
            .IsUnique();

        modelBuilder.Entity<Location>()
            .HasIndex(e => e.LowercaseName)
            .IsUnique();

        modelBuilder.Entity<Reservation>()
            .HasIndex(e => new { e.Day, e.DeskId })
            .IsUnique();

        modelBuilder.Entity<Room>()
            .HasIndex(e => new { e.FloorId, e.LowercaseName })
            .IsUnique();

        modelBuilder.Entity<User>()
            .HasIndex(e => new { e.AuthenticationScheme, e.Sub })
            .IsUnique();
    }
}
