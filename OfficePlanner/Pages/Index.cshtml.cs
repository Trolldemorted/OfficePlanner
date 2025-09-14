using Htmx;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OfficePlanner.Database;
using OfficePlanner.ViewModels;
namespace OfficePlanner.Pages;

[Authorize]
[RequireAntiforgeryToken]
public class IndexModel(OfficePlannerDatabase db, OPDbContext dbContext) : PageModel
{
    public async Task<IActionResult> OnGetAsync()
    {
        return Request.IsHtmx()
            ? Partial("_Index", await this.GetViewModel())
            : Page();
    }

    public async Task<IActionResult> OnPostNewLocationAsync(string newLocationName)
    {
        bool isAdmin = await db.IsUserAdmin(this.HttpContext);
        if (isAdmin)
        {
            if (string.IsNullOrEmpty(newLocationName))
            {
                return Partial("_index", await this.GetViewModel(isAdmin: isAdmin, newLocationName: newLocationName, newLocationNameError: "Location name must not be empty"));
            }

            if (newLocationName.Contains('/'))
            {
                return Partial("_index", await this.GetViewModel(isAdmin: isAdmin, newLocationName: newLocationName, newLocationNameError: "Location name must not contain '/'"));
            }

            dbContext.Locations.Add(new Location()
            {
                Name = newLocationName,
                LowercaseName = newLocationName.ToLower(),
            });
            await dbContext.SaveChangesAsync(Request.HttpContext.RequestAborted);
        }
        return Partial("_Index", await this.GetViewModel(isAdmin: isAdmin));
    }

    public async Task<IActionResult> OnPostDeleteLocationAsync(string locationName)
    {
        bool isAdmin = await db.IsUserAdmin(this.HttpContext);
        if (isAdmin)
        {
            await db.DeleteLocation(locationName, this.HttpContext.RequestAborted);
        }
        return Partial("_Index", await this.GetViewModel(isAdmin: isAdmin));
    }

    public async Task<IndexViewModel> GetViewModel(bool? isAdmin = null, string newLocationName = "", string? newLocationNameError = null)
    {
        var dbLocations = await dbContext.Locations
            .AsNoTracking()
            .OrderBy(e => e.Name)
            .ToListAsync(this.HttpContext.RequestAborted);

        return new()
        {
            IsAdmin = isAdmin ?? await db.IsUserAdmin(this.HttpContext),
            Locations = dbLocations,
            NewLocationName = newLocationName,
            NewLocationNameError = newLocationNameError,
        };
    }
}
