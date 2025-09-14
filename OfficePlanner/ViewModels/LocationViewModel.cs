using OfficePlanner.Database;

namespace OfficePlanner.ViewModels;

public record LocationViewModel
{
    public required bool IsAdmin { get; set; }
    public required Location? Location { get; set; }
    public required string NewBuildingName { get; set; }
    public required string? NewBuildingNameError { get; set; }
}
