using Htmx;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OfficePlanner.Database;
using OfficePlanner.ViewModels;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Xml.Linq;

namespace OfficePlanner.Pages;

[Authorize]
[RequireAntiforgeryToken]
public class BuildingModel(OfficePlannerDatabase db, ILogger<BuildingModel> logger) : PageModel
{
    [BindProperty(SupportsGet = true), Required]
    public required string Location { get; init; }

    [BindProperty(SupportsGet = true), Required]
    public required string Building { get; init; }

    public async Task<IActionResult> OnGet()
    {
        return Request.IsHtmx()
            ? Partial("_Building", await this.GetViewModel())
            : Page();
    }

    public async Task<IActionResult> OnPostUpsertFloor(string name, IFormFile floorPlanFile)
    {
        var isAdmin = await db.IsUserAdmin(this.HttpContext);
        if (isAdmin)
        {
            if (floorPlanFile.Length > 10_000_000)
            {
                return this.BadRequest();
            }

            using var fileStream = floorPlanFile.OpenReadStream();
            byte[] floorPlanBuffer = new byte[floorPlanFile.Length];
            await fileStream.ReadAsync(floorPlanBuffer.AsMemory(0, (int)floorPlanFile.Length), this.HttpContext.RequestAborted);

            var rooms = new Dictionary<string, Room>();
            string floorPlan;
            try
            {
                floorPlan = Encoding.UTF8.GetString(floorPlanBuffer);
                //var stream = new MemoryStream(floorPlanBuffer);
                XDocument xdoc = XDocument.Parse(floorPlan);// Why does Load work but Parse does not?!
                                                            //XDocument xdoc = XDocument.Load(stream, LoadOptions.None);
                var ns = xdoc.Root!.Name.Namespace;
                var rectElements = xdoc.Descendants(ns + "rect");

                foreach (var rect in rectElements)
                {
                    var desk = (string?)rect.Attribute("data-op-desk");
                    if (desk == null)
                    {
                        continue;
                    }

                    var room = (string?)rect.Attribute("data-op-room");
                    if (room == null)
                    {
                        continue;
                    }

                    rooms.TryAdd(room.ToLower(), new Room()
                    {
                        LowercaseName = room.ToLower(),
                        Name = room,
                    });
                    var dbRoom = rooms[room.ToLower()];
                    dbRoom.Desks.Add(new Desk()
                    {
                        LowercaseName = desk.ToLower(),
                        Name = desk,
                    });
                }
            }
            catch (Exception ex)
            {
                logger.LogError("OnPostCreateFloor failed: {}", ex);
                return this.BadRequest();
            }

            await db.UpsertFloor(this.Location, this.Building, name, [.. rooms.Values], floorPlan, this.HttpContext.RequestAborted);
        }
        return Partial("_Building", await this.GetViewModel(isAdmin: isAdmin));
    }

    public async Task<BuildingViewModel> GetViewModel(bool? isAdmin = null)
    {
        var building = await db.GetBuilding(this.Location, this.Building, this.HttpContext.RequestAborted);
        return new()
        {
            IsAdmin = isAdmin ?? await db.IsUserAdmin(this.HttpContext),
            Building = building,
            LocationLowercaseName = this.Location,
            LocationName = building?.Location?.Name,
        };
    }
}
