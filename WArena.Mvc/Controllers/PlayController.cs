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
        return View(await BuildQuestionViewModelAsync(question, setId, index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(PlayQuestionViewModel model)
    {
        var question = await questions.GetByIdAsync(model.QuestionId);
        if (question is null) return NotFound();

        if (!ModelState.IsValid)
            return View("Question", await BuildQuestionViewModelAsync(question, model.ChallengeSetId, model.QuestionIndex, model.Answer));

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var user = await userManager.FindByIdAsync(userId);
        if (user is null) return Challenge();

        var alreadyCompleted = await submissions.HasCorrectSubmissionAsync(userId, question.Id);
        var result = gamification.ScorePracticeAnswer(question, model.Answer, alreadyCompleted);
        var now = DateTime.UtcNow;

        await submissions.AddAsync(new Submission
        {
            QuestionId = question.Id,
            UserId = userId,
            Answer = model.Answer.Trim(),
            IsCorrect = result.IsCorrect,
            PointsEarned = result.PointsEarned,
            SubmittedAtUtc = now
        });

        if (result.IsCorrect)
        {
            user.TotalXp += result.PointsEarned;
            user.CurrentStreak = gamification.UpdateStreak(user.CurrentStreak, user.LastActivityUtc, now);
            user.LastActivityUtc = now;
            await userManager.UpdateAsync(user);
            await achievements.AwardEligibleAsync(user, await submissions.CountCorrectForUserAsync(userId));
        }

        var next = await GetNextQuestionAsync(model.ChallengeSetId, model.QuestionIndex);
        logger.LogInformation("User {UserId} answered question {QuestionId}. Correct={Correct}; Points={Points}", userId, question.Id, result.IsCorrect, result.PointsEarned);

        return View("Result", new AnswerResultViewModel
        {
            IsCorrect = result.IsCorrect,
            PointsEarned = result.PointsEarned,
            TotalXp = user.TotalXp,
            SubmittedAnswer = model.Answer.Trim(),
            NextQuestionId = next.QuestionId,
            ChallengeSetId = model.ChallengeSetId,
            NextQuestionIndex = next.Index
        });
    }

    private async Task<PlayQuestionViewModel> BuildQuestionViewModelAsync(Question question, int? setId, int index, string answer = "")
    {
        var total = 1;
        if (setId.HasValue)
        {
            var set = await sets.GetByIdAsync(setId.Value);
            if (set is not null) total = set.ChallengeSetQuestions.Count;
        }
        var options = new[] { question.OptionA, question.OptionB, question.OptionC, question.OptionD }.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x!).ToList();
        return new PlayQuestionViewModel
        {
            QuestionId = question.Id,
            ChallengeSetId = setId,
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

    private async Task<(int? QuestionId, int Index)> GetNextQuestionAsync(int? setId, int currentIndex)
    {
        if (!setId.HasValue) return (null, currentIndex + 1);
        var set = await sets.GetByIdAsync(setId.Value);
        if (set is null) return (null, currentIndex + 1);
        var ordered = set.ChallengeSetQuestions.OrderBy(x => x.Order).ToList();
        var nextIndex = currentIndex + 1;
        return nextIndex < ordered.Count ? (ordered[nextIndex].QuestionId, nextIndex) : (null, nextIndex);
    }
}
