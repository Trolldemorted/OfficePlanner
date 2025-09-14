using OfficePlanner.Database;

namespace OfficePlanner.ViewModels;

public record BuildingViewModel
{
    public required bool IsAdmin { get; set; }
    public required string LocationLowercaseName { get; set; }
    public required string? LocationName { get; set; }
    public required Building? Building { get; set; }
}
