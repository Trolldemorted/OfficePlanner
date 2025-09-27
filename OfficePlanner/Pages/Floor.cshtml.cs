using Htmx;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OfficePlanner.Database;
using OfficePlanner.ViewModels;
using System.ComponentModel.DataAnnotations;
using System.Xml.Linq;

namespace OfficePlanner.Pages;

[Authorize]
[RequireAntiforgeryToken]
public class FloorModel(OfficePlannerDatabase db, ILogger<FloorModel> logger) : PageModel
{
    [BindProperty(SupportsGet = true), Required]
    public required string Location { get; init; }

    [BindProperty(SupportsGet = true), Required]
    public required string Building { get; init; }

    [BindProperty(SupportsGet = true), Required]
    public required string Floor { get; init; }

    [BindProperty(SupportsGet = true)]
    public DateOnly? Day { get; init; }

    public async Task<IActionResult> OnGet(DateOnly? day)
    {
        return this.Request.IsHtmx()
            ? Partial("_Floor", await this.GetViewModel(day))
            : Page();
    }

    public async Task<IActionResult> OnPostBookDesk(string room, string desk, DateOnly reservationDay)
    {
        if (Util.GetUserId(this.HttpContext) is long userId)
        {
            await db.UpsertReservation(
                this.Location,
                this.Building,
                this.Floor,
                room,
                reservationDay,
                desk,
                userId,
                this.HttpContext.RequestAborted);
        }
        return Partial("_Floor", await this.GetViewModel(reservationDay));
    }

    public async Task<IActionResult> OnPostFreeDesk(string room, string desk, DateOnly reservationDay)
    {
        if (Util.GetUserId(this.HttpContext) is long userId)
        {
            await db.DeleteReservation(
                this.Location,
                this.Building,
                this.Floor,
                room,
                reservationDay,
                desk,
                userId,
                this.HttpContext.RequestAborted);
        }
        return Partial("_Floor", await this.GetViewModel(reservationDay));
    }

    private string TryLoadSvg(string floorFile, long userId, DateOnly day, Floor floor)
    {
        /*
        Rules for svgs:
        - these svgs are RAW HTML and thus DANGEROUS.Do NOT accept svgs from untrusted personnel.
        - all your ids must be valid
        - if a rect is a desk, add data-op-desk="deskname" and data-op-room="roomname" to it
        */
        try
        {
            XDocument xdoc = XDocument.Parse(floorFile);
            foreach (var descendant in xdoc.Descendants())
            {
                descendant.SetAttributeValue("style", null);
                if (descendant.Name.LocalName != "rect")
                {
                    continue;
                }

                var desk = (string?)descendant.Attribute("data-op-desk");
                if (desk == null)
                {
                    continue;
                }

                var room = (string?)descendant.Attribute("data-op-room");
                if (room == null)
                {
                    continue;
                }

                var dbDesk = floor.Rooms
                    .SelectMany(e => e.Desks)
                    .Where(e => e.LowercaseName == desk)
                    .SingleOrDefault();

                if (dbDesk == null)
                {
                    continue;
                }

                var dbReservation = dbDesk.Reservations
                    .Where(e => e.Day == day)
                    .SingleOrDefault();
                if (dbReservation != null)
                {
                    if (dbReservation.UserId == userId)
                    {
                        descendant.SetAttributeValue("fill", "lightgreen");
                        descendant.SetAttributeValue("data-hx-post", this.Url.Page("Floor", "FreeDesk", new
                        {
                            this.Location,
                            this.Building,
                            this.Floor,
                            ReservationDay = day,
                            Room = room,
                            Desk = desk,
                        }));
                        descendant.SetAttributeValue("data-hx-target", "#main");
                    }
                    else
                    {
                        descendant.SetAttributeValue("fill", "orange");
                    }
                    descendant.Add(new XElement("title", dbReservation.User!.Name));
                    continue;
                }

                descendant.SetAttributeValue("fill", "lightgray");
                descendant.SetAttributeValue("data-hx-post", this.Url.Page("Floor", "BookDesk", new
                {
                    this.Location,
                    this.Building,
                    this.Floor,
                    ReservationDay = day,
                    Room = room,
                    Desk = desk,
                }));
                descendant.SetAttributeValue("data-hx-target", "#main");
            }
            return xdoc.ToString(SaveOptions.DisableFormatting);
        }
        catch (Exception ex)
        {
            logger.LogError("TryLoadSvg failed: {}", ex);
            return floorFile;
        }
    }

    public async Task<FloorViewModel> GetViewModel(DateOnly? day = null, bool? isAdmin = false)
    {
        long userId = 0;
        DateOnly setDay = day ?? DateOnly.FromDateTime(DateTime.Now).GetFirstWorkingDay();
        var floor = await db.GetFloor(this.Location, this.Building, this.Floor, this.HttpContext.RequestAborted);
        string? floorPlan = null;
        if (floor != null && Util.GetUserId(this.HttpContext) is long sessionUserId)
        {
            floorPlan = this.TryLoadSvg(floor.FloorPlan, sessionUserId, setDay, floor);
            userId = sessionUserId;
        }
        return new()
        {
            IsAdmin = isAdmin ?? await db.IsUserAdmin(this.HttpContext),
            BuildingLowercaseName = this.Building,
            Day = setDay,
            Floor = floor,
            FloorPlan = floorPlan,
            LocationLowercaseName = this.Location,
            UserId = userId,
            BuildingName = floor?.Building?.Name,
            LocationName = floor?.Building?.Location?.Name,
        };
    }
}
