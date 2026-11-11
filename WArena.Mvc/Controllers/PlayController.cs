using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WArena.Mvc.Models;
using WArena.Mvc.Repositories;
using WArena.Mvc.Services;
using WArena.Mvc.ViewModels;

namespace WArena.Mvc.Controllers;

[Authorize]
public class PlayController(
    IQuestionRepository questions,
    IChallengeSetRepository sets,
    ISubmissionRepository submissions,
    IAchievementRepository achievements,
    IGamificationService gamification,
    UserManager<ApplicationUser> userManager,
    ILogger<PlayController> logger) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Question(int id, int? setId = null, int index = 0)
    {
        var question = await questions.GetByIdAsync(id);
        if (question is null) return NotFound();
        var navigation = await GetQuestionNavigationAsync(question.Id, setId);
        if (navigation is null) return NotFound();
        return View(await BuildQuestionViewModelAsync(question, setId, navigation.Value.Index, navigation.Value.Total));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(PlayQuestionViewModel model)
    {
        var question = await questions.GetByIdAsync(model.QuestionId);
        if (question is null) return NotFound();
        var navigation = await GetQuestionNavigationAsync(question.Id, model.ChallengeSetId);
        if (navigation is null) return NotFound();

        // The posted index is only a display hint; use the question's current position.
        ModelState.Remove(nameof(PlayQuestionViewModel.QuestionIndex));

        if (!ModelState.IsValid)
            return View("Question", await BuildQuestionViewModelAsync(question, model.ChallengeSetId, navigation.Value.Index, navigation.Value.Total, model.Answer));

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var user = await userManager.FindByIdAsync(userId);
        if (user is null) return Challenge();

        var alreadyCompleted = await submissions.HasCorrectSubmissionAsync(userId, question.Id);
        var result = gamification.ScorePracticeAnswer(question, model.Answer, alreadyCompleted);
        var now = DateTime.UtcNow;

        var submission = new Submission
        {
            QuestionId = question.Id,
            UserId = userId,
            Answer = model.Answer.Trim(),
            IsCorrect = result.IsCorrect,
            PointsEarned = result.PointsEarned,
            SubmittedAtUtc = now
        };
        await submissions.AddAsync(submission);

        if (result.IsCorrect)
        {
            user.TotalXp += result.PointsEarned;
            user.CurrentStreak = gamification.UpdateStreak(user.CurrentStreak, user.LastActivityUtc, now);
            user.LastActivityUtc = now;
            await userManager.UpdateAsync(user);
            await achievements.AwardEligibleAsync(user, await submissions.CountCorrectForUserAsync(userId));
        }

        logger.LogInformation("User {UserId} answered question {QuestionId}. Correct={Correct}; Points={Points}", userId, question.Id, result.IsCorrect, result.PointsEarned);

        return RedirectToAction(nameof(Result), new { id = submission.Id, setId = model.ChallengeSetId, index = navigation.Value.Index });
    }

    [HttpGet]
    public async Task<IActionResult> Result(int id, int? setId = null, int index = 0)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var submission = await submissions.GetByIdForUserAsync(id, userId);
        if (submission is null) return NotFound();
        var user = await userManager.FindByIdAsync(userId);
        if (user is null) return Challenge();

        var navigation = await GetQuestionNavigationAsync(submission.QuestionId, setId);
        if (navigation is null) setId = null;
        return View("Result", new AnswerResultViewModel
        {
            IsCorrect = submission.IsCorrect,
            PointsEarned = submission.PointsEarned,
            TotalXp = user.TotalXp,
            SubmittedAnswer = submission.Answer,
            NextQuestionId = navigation?.NextQuestionId,
            ChallengeSetId = setId,
            NextQuestionIndex = (navigation?.Index ?? 0) + 1
        });
    }

    private async Task<PlayQuestionViewModel> BuildQuestionViewModelAsync(Question question, int? setId, int index, int total, string answer = "")
    {
        var options = new[] { question.OptionA, question.OptionB, question.OptionC, question.OptionD }.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x!).ToList();
        return new PlayQuestionViewModel
        {
            QuestionId = question.Id,
            ChallengeSetId = setId,
            IsCompleted = await submissions.HasCorrectSubmissionAsync(User.FindFirstValue(ClaimTypes.NameIdentifier)!, question.Id),
            QuestionIndex = index,
            TotalQuestions = total,
            Title = question.Title,
            Prompt = question.Prompt,
            Category = question.Category?.Name ?? string.Empty,
            Type = question.Type,
            Difficulty = question.Difficulty,
            Points = question.Points,
            Options = options,
            Answer = answer
        };
    }

    private async Task<(int Index, int Total, int? NextQuestionId)?> GetQuestionNavigationAsync(int questionId, int? setId)
    {
        if (!setId.HasValue) return (0, 1, null);
        var set = await sets.GetByIdAsync(setId.Value);
        if (set is null || (!set.IsPublished && !User.IsInRole("Admin") && !User.IsInRole("Teacher"))) return null;
        var ordered = set.ChallengeSetQuestions.OrderBy(x => x.Order).ToList();
        var currentIndex = ordered.FindIndex(x => x.QuestionId == questionId);
        if (currentIndex < 0) return null;
        var nextId = currentIndex + 1 < ordered.Count ? ordered[currentIndex + 1].QuestionId : (int?)null;
        return (currentIndex, ordered.Count, nextId);
    }
}
