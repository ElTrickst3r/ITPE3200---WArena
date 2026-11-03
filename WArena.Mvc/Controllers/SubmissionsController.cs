using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WArena.Mvc.Repositories;

namespace WArena.Mvc.Controllers;

[Authorize]
public class SubmissionsController(ISubmissionRepository submissions) : Controller
{
    public async Task<IActionResult> Index()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        return View(await submissions.GetForUserAsync(userId));
    }
}
