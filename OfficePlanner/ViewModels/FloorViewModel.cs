using OfficePlanner.Database;

namespace OfficePlanner.ViewModels;

public record FloorViewModel
{
    public required bool IsAdmin { get; set; }
    public required long UserId { get; set; }
    public required string LocationLowercaseName { get; set; }
    public required string BuildingLowercaseName { get; set; }
    public required DateOnly Day { get; set; }
    public required Floor? Floor { get; set; }
    public required string? FloorPlan { get; set; }
    public required string? LocationName { get; set; }
    public required string? BuildingName { get; set; }
}
