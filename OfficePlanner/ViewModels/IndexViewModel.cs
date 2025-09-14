using OfficePlanner.Database;

namespace OfficePlanner.ViewModels;

public record IndexViewModel
{
    public required bool IsAdmin { get; set; }
    public required List<Location> Locations { get; set; }
    public required string NewLocationName { get; set; }
    public required string? NewLocationNameError { get; set; }
}
