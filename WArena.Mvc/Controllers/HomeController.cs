using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WArena.Mvc.Models;
using WArena.Mvc.Repositories;
using WArena.Mvc.ViewModels;

namespace WArena.Mvc.Controllers;

public class HomeController(
    IChallengeSetRepository challengeSets,
    ISubmissionRepository submissions,
    IAchievementRepository achievements,
    UserManager<ApplicationUser> userManager,
    ILogger<HomeController> logger) : Controller
{
    public async Task<IActionResult> Index()
    {
        var sets = await challengeSets.SearchAsync(new ChallengeSetSearchCriteria(null, null, true), includeUnpublished: false);
        var model = new DashboardViewModel { ChallengeSets = sets.Take(6).ToList() };

        if (User.Identity?.IsAuthenticated == true)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var user = await userManager.FindByIdAsync(userId);
            var attempts = await submissions.CountAttemptsForUserAsync(userId);
            var correct = await submissions.CountCorrectForUserAsync(userId);
            var history = await submissions.GetForUserAsync(userId);

            model.TotalXp = user?.TotalXp ?? 0;
            model.CurrentStreak = user?.CurrentStreak ?? 0;
            model.TotalAttempts = attempts;
            model.CompletedQuestions = history.Where(x => x.IsCorrect).Select(x => x.QuestionId).Distinct().Count();
            model.Accuracy = attempts == 0 ? 0 : Math.Round((double)correct / attempts * 100);
            model.RecentAchievements = (await achievements.GetForUserAsync(userId)).Take(4).ToList();
        }

        return View(model);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        var requestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        logger.LogError("Unhandled MVC error. Request ID: {RequestId}", requestId);
        return View(new ErrorViewModel { RequestId = requestId });
    }
}
