using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OfficePlanner.Database;
using System.Reflection.Metadata.Ecma335;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace OfficePlanner.Controllers;

[Route("api/[controller]/[action]")]
public class AdminController(OfficePlannerDatabase db, OPConfig config) : Controller
{
    [HttpPost]
    public async Task<IActionResult> SetAdmin(string adminPassword, bool isAdmin)
    {
        var userId = Util.GetUserId(this.HttpContext);
        if (userId is null)
        {
            return this.BadRequest();
        }
        if (CryptographicOperations.FixedTimeEquals(Encoding.Unicode.GetBytes(adminPassword), Encoding.Unicode.GetBytes(config.AdminPassword)))
        {
            await db.SetUserAdmin(userId.Value, isAdmin, this.HttpContext.RequestAborted);
            return this.NoContent();
        }
        else
        {
            return this.Unauthorized();
        }
    }
}
