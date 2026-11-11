using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WArena.Mvc.Models;
using WArena.Mvc.Repositories;
using WArena.Mvc.ViewModels;

namespace WArena.Mvc.Controllers;

[Authorize]
public class ChallengeSetsController(
    IChallengeSetRepository sets,
    IQuestionRepository questions,
    ISubmissionRepository submissions,
    ILogger<ChallengeSetsController> logger) : Controller
{
    public async Task<IActionResult> Index(string? search, int? minQuestions, bool? isPublished)
    {
        var canManage = User.IsInRole("Admin") || User.IsInRole("Teacher");
        var model = new ChallengeSetSearchViewModel
        {
            Search = search,
            MinQuestions = minQuestions,
            IsPublished = isPublished,
            ChallengeSets = await sets.SearchAsync(new ChallengeSetSearchCriteria(search, minQuestions, isPublished), includeUnpublished: canManage)
        };
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (User.Identity?.IsAuthenticated == true && userId is not null)
        {
            var questionIds = model.ChallengeSets.SelectMany(x => x.ChallengeSetQuestions)
                .Select(x => x.QuestionId).Distinct().ToArray();
            model.CompletedQuestionIds = await submissions.GetCompletedQuestionIdsAsync(userId, questionIds);
        }
        return View(model);
    }

    public async Task<IActionResult> Details(int id)
    {
        var set = await sets.GetByIdAsync(id);
        if (set is null || (!set.IsPublished && !User.IsInRole("Admin") && !User.IsInRole("Teacher"))) return NotFound();

        var completed = await submissions.GetCompletedQuestionIdsAsync(User.FindFirstValue(ClaimTypes.NameIdentifier)!,
            set.ChallengeSetQuestions.Select(x => x.QuestionId).ToArray());
        return View(new ChallengeSetDetailsViewModel
        {
            ChallengeSet = set,
            CompletedQuestionIds = completed,
            CompletedQuestions = completed.Count,
            TotalQuestions = set.ChallengeSetQuestions.Count
        });
    }

    [Authorize(Roles = "Admin,Teacher")]
    [HttpGet]
    public async Task<IActionResult> Create() => View(await PrepareFormAsync(new ChallengeSetFormViewModel()));

    [Authorize(Roles = "Admin,Teacher")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ChallengeSetFormViewModel model)
    {
        if (!ModelState.IsValid) return View(await PrepareFormAsync(model));
        var set = new ChallengeSet { Name = model.Name.Trim(), Description = model.Description?.Trim(), IsPublished = model.IsPublished };
        await sets.AddAsync(set, model.QuestionIds);
        logger.LogInformation("Challenge set {ChallengeSetId} created", set.Id);
        TempData["Message"] = "Challenge set created.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin,Teacher")]
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var set = await sets.GetByIdAsync(id);
        if (set is null) return NotFound();
        return View(await PrepareFormAsync(new ChallengeSetFormViewModel
        {
            Id = set.Id,
            Name = set.Name,
            Description = set.Description,
            IsPublished = set.IsPublished,
            QuestionIds = set.ChallengeSetQuestions.OrderBy(x => x.Order).Select(x => x.QuestionId).ToList()
        }));
    }

    [Authorize(Roles = "Admin,Teacher")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ChallengeSetFormViewModel model)
    {
        if (id != model.Id) return BadRequest();
        if (!ModelState.IsValid) return View(await PrepareFormAsync(model));
        var set = await sets.GetByIdAsync(id);
        if (set is null) return NotFound();
        set.Name = model.Name.Trim(); set.Description = model.Description?.Trim(); set.IsPublished = model.IsPublished;
        await sets.UpdateAsync(set, model.QuestionIds);
        logger.LogInformation("Challenge set {ChallengeSetId} updated", id);
        TempData["Message"] = "Challenge set updated.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin,Teacher")]
    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var set = await sets.GetByIdAsync(id);
        return set is null ? NotFound() : View(set);
    }

    [Authorize(Roles = "Admin,Teacher")]
    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var set = await sets.GetByIdAsync(id);
        if (set is null) return NotFound();
        await sets.DeleteAsync(set);
        logger.LogInformation("Challenge set {ChallengeSetId} deleted", id);
        TempData["Message"] = "Challenge set deleted.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<ChallengeSetFormViewModel> PrepareFormAsync(ChallengeSetFormViewModel model)
    {
        model.AvailableQuestions = await questions.GetAllAsync();
        return model;
    }
}
