using Htmx;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OfficePlanner.Database;
using OfficePlanner.ViewModels;
using System.ComponentModel.DataAnnotations;

namespace OfficePlanner.Pages;

[Authorize]
[RequireAntiforgeryToken]
public class LocationModel(OfficePlannerDatabase db, ILogger<LocationModel> logger) : PageModel
{
    [BindProperty(SupportsGet = true), Required]
    public required string Location { get; init; }

    public async Task<IActionResult> OnGet()
    {
        return Request.IsHtmx()
            ? Partial("_Location", await this.GetViewModel())
            : Page();
    }

    public async Task<IActionResult> OnPostCreateBuilding(string newBuildingName)
    {
        bool isAdmin = await db.IsUserAdmin(this.HttpContext);
        if (isAdmin)
        {
            if (string.IsNullOrEmpty(newBuildingName))
            {
                return Partial("_Location", await this.GetViewModel(isAdmin: isAdmin, newBuildingName: newBuildingName, newBuildingNameError: "Building name must not be empty"));
            }

            if (newBuildingName.Contains('/'))
            {
                return Partial("_Location", await this.GetViewModel(isAdmin: isAdmin, newBuildingName: newBuildingName, newBuildingNameError: "Building name must not contain '/'"));
            }

            logger.LogInformation("Creating building '{}'", newBuildingName);
            await db.CreateBuilding(this.Location, newBuildingName, this.HttpContext.RequestAborted);
        }
        return Partial("_Location", await this.GetViewModel(isAdmin: isAdmin));
    }

    public async Task<IActionResult> OnPostDeleteBuilding(string building)
    {
        bool isAdmin = await db.IsUserAdmin(this.HttpContext);
        if (isAdmin)
        {
            await db.DeleteBuilding(this.Location, building, this.HttpContext.RequestAborted);
        }
        return Partial("_Location", await this.GetViewModel(isAdmin: isAdmin));
    }

    public async Task<LocationViewModel> GetViewModel(bool? isAdmin = null, string newBuildingName = "", string? newBuildingNameError = null)
    {
        var dbLocation = await db.GetLocation(this.Location, this.HttpContext.RequestAborted);
        return new()
        {
            IsAdmin = isAdmin ?? await db.IsUserAdmin(this.HttpContext),
            Location = dbLocation,
            NewBuildingName = newBuildingName,
            NewBuildingNameError = newBuildingNameError,
        };
    }
}
