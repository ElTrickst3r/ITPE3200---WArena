using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WArena.Api.Data;
using WArena.Api.Dtos;
using WArena.Api.Models;
using WArena.Api.Repositories;
using WArena.Api.Services;

namespace WArena.Api.Controllers;

[ApiController, Route("api/progress"), Authorize]
public class ProgressController(
    IQuestionRepository questions,
    IProgressRepository progress,
    IGamificationService gamification,
    UserManager<ApplicationUser> users,
    ApplicationDbContext db,
    ILogger<ProgressController> logger) : ControllerBase
{
    [HttpGet("dashboard")]
    public async Task<ActionResult<DashboardDto>> Dashboard()
    {
        var uid = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var user = await users.FindByIdAsync(uid);
        if (user is null) return Unauthorized();

        var history = await progress.HistoryAsync(uid);
        var attempts = history.Count;
        var correct = history.Count(x => x.IsCorrect);
        var completedQuestions = history.Where(x => x.IsCorrect).Select(x => x.QuestionId).Distinct().Count();
        var accuracy = attempts == 0 ? 0 : Math.Round((double)correct / attempts * 100);

        var badges = (await progress.AchievementsAsync(uid))
            .Take(4)
            .Select(ToAchievementDto)
            .ToList();

        return Ok(new DashboardDto(user.TotalXp, completedQuestions, attempts, accuracy, user.CurrentStreak, badges));
    }

    [HttpGet("history")]
    public async Task<ActionResult<IEnumerable<SubmissionDto>>> History()
    {
        var uid = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var history = await progress.HistoryAsync(uid);

        return Ok(history.Select(x => new SubmissionDto(
            x.Id,
            x.QuestionId,
            x.Question?.Title ?? "",
            x.Question?.Category?.Name ?? "",
            x.Answer,
            x.IsCorrect,
            x.PointsEarned,
            x.SubmittedAtUtc)));
    }

    [HttpGet("achievements")]
    public async Task<ActionResult<IEnumerable<AchievementDto>>> Achievements()
    {
        var uid = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var achievements = await progress.AchievementsAsync(uid);
        return Ok(achievements.Select(ToAchievementDto));
    }

    [HttpPost("questions/{questionId:int}/answer")]
    public async Task<ActionResult<AnswerResultDto>> Answer(int questionId, AnswerRequestDto dto)
    {
        var question = await questions.GetAsync(questionId);
        if (question is null) return NotFound();

        var uid = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var user = await users.FindByIdAsync(uid);
        if (user is null) return Unauthorized();

        // Submission, XP and achievements are saved together. Returning before CommitAsync rolls everything back.
        await using var transaction = await db.Database.BeginTransactionAsync();

        var already = await progress.HasCorrectAsync(uid, questionId);
        var result = gamification.ScorePractice(question, dto.Answer, already);
        var now = DateTime.UtcNow;

        await progress.AddSubmissionAsync(new Submission
        {
            QuestionId = questionId,
            UserId = uid,
            Answer = dto.Answer.Trim(),
            IsCorrect = result.IsCorrect,
            PointsEarned = result.Points,
            SubmittedAtUtc = now
        });

        if (result.IsCorrect)
        {
            user.TotalXp += result.Points;
            user.CurrentStreak = gamification.UpdateStreak(user.CurrentStreak, user.LastActivityUtc, now);
            user.LastActivityUtc = now;

            var updateResult = await users.UpdateAsync(user);
            if (!updateResult.Succeeded)
                return Conflict(new { message = "Your answer could not be saved. Please try again." });

            await progress.AwardEligibleAsync(user, await progress.CorrectCountAsync(uid));
        }

        await transaction.CommitAsync();
        logger.LogInformation("API user {UserId} answered {QuestionId}. Correct={Correct}, points={Points}", uid, questionId, result.IsCorrect, result.Points);
        return Ok(new AnswerResultDto(result.IsCorrect, result.Points, user.TotalXp, user.CurrentStreak));
    }

    private static AchievementDto ToAchievementDto(UserAchievement x) => new(
        x.AchievementId,
        x.Achievement?.Name ?? "",
        x.Achievement?.Description ?? "",
        x.Achievement?.Icon ?? "🏆",
        x.EarnedAtUtc);
}