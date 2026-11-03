using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using WArena.Mvc.Models;
using WArena.Mvc.Repositories;
using WArena.Mvc.ViewModels;

namespace WArena.Mvc.Controllers;

[Authorize]
public class QuestionsController(
    IQuestionRepository questions,
    ICategoryRepository categories,
    ILogger<QuestionsController> logger) : Controller
{
    public async Task<IActionResult> Index(string? search, int? categoryId, Difficulty? difficulty, int? minPoints, int? maxPoints)
    {
        var model = new QuestionSearchViewModel
        {
            Search = search,
            CategoryId = categoryId,
            Difficulty = difficulty,
            MinPoints = minPoints,
            MaxPoints = maxPoints,
            Questions = await questions.SearchAsync(new QuestionSearchCriteria(search, categoryId, difficulty, minPoints, maxPoints)),
            Categories = await CategoryOptionsAsync(categoryId)
        };
        return View(model);
    }

    [Authorize(Roles = "Admin,Teacher")]
    [HttpGet]
    public async Task<IActionResult> Create() => View(await PrepareFormAsync(new QuestionFormViewModel()));

    [Authorize(Roles = "Admin,Teacher")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(QuestionFormViewModel model)
    {
        ValidateQuestionOptions(model);
        if (!ModelState.IsValid) return View(await PrepareFormAsync(model));

        var entity = Map(model);
        await questions.AddAsync(entity);
        logger.LogInformation("Question {QuestionId} created", entity.Id);
        TempData["Message"] = "Question created.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin,Teacher")]
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var q = await questions.GetByIdAsync(id);
        if (q is null) return NotFound();
        return View(await PrepareFormAsync(new QuestionFormViewModel
        {
            Id = q.Id, Title = q.Title, Prompt = q.Prompt, CorrectAnswer = q.CorrectAnswer,
            Type = q.Type, Difficulty = q.Difficulty, Points = q.Points, CategoryId = q.CategoryId,
            OptionA = q.OptionA, OptionB = q.OptionB, OptionC = q.OptionC, OptionD = q.OptionD
        }));
    }

    [Authorize(Roles = "Admin,Teacher")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, QuestionFormViewModel model)
    {
        if (id != model.Id) return BadRequest();
        ValidateQuestionOptions(model);
        if (!ModelState.IsValid) return View(await PrepareFormAsync(model));

        var q = await questions.GetByIdAsync(id);
        if (q is null) return NotFound();
        q.Title = model.Title.Trim(); q.Prompt = model.Prompt.Trim(); q.CorrectAnswer = model.CorrectAnswer.Trim();
        q.Type = model.Type; q.Difficulty = model.Difficulty; q.Points = model.Points; q.CategoryId = model.CategoryId;
        q.OptionA = model.OptionA?.Trim(); q.OptionB = model.OptionB?.Trim(); q.OptionC = model.OptionC?.Trim(); q.OptionD = model.OptionD?.Trim();
        await questions.UpdateAsync(q);
        logger.LogInformation("Question {QuestionId} updated", id);
        TempData["Message"] = "Question updated.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin,Teacher")]
    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var q = await questions.GetByIdAsync(id);
        return q is null ? NotFound() : View(q);
    }

    [Authorize(Roles = "Admin,Teacher")]
    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var q = await questions.GetByIdAsync(id);
        if (q is null) return NotFound();
        await questions.DeleteAsync(q);
        logger.LogInformation("Question {QuestionId} deleted", id);
        TempData["Message"] = "Question deleted.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<QuestionFormViewModel> PrepareFormAsync(QuestionFormViewModel model)
    {
        model.Categories = await CategoryOptionsAsync(model.CategoryId == 0 ? null : model.CategoryId);
        return model;
    }

    private async Task<IEnumerable<SelectListItem>> CategoryOptionsAsync(int? selected) =>
        (await categories.GetAllAsync()).Select(x => new SelectListItem(x.Name, x.Id.ToString(), x.Id == selected));

    private void ValidateQuestionOptions(QuestionFormViewModel model)
    {
        if (model.Type == QuestionType.MultipleChoice)
        {
            var options = new[] { model.OptionA, model.OptionB, model.OptionC, model.OptionD };
            if (options.Any(string.IsNullOrWhiteSpace)) ModelState.AddModelError(string.Empty, "Multiple-choice questions need four options.");
            var correct = model.CorrectAnswer?.Trim() ?? string.Empty;
            if (!options.Any(x => string.Equals(x?.Trim(), correct, StringComparison.OrdinalIgnoreCase)))
                ModelState.AddModelError(nameof(model.CorrectAnswer), "The correct answer must match one of the options.");
        }
    }

    private static Question Map(QuestionFormViewModel model) => new()
    {
        Title = model.Title.Trim(), Prompt = model.Prompt.Trim(), CorrectAnswer = model.CorrectAnswer.Trim(),
        Type = model.Type, Difficulty = model.Difficulty, Points = model.Points, CategoryId = model.CategoryId,
        OptionA = model.OptionA?.Trim(), OptionB = model.OptionB?.Trim(), OptionC = model.OptionC?.Trim(), OptionD = model.OptionD?.Trim()
    };
}
