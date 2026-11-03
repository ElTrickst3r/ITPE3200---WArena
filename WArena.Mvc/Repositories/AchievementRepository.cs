using Microsoft.EntityFrameworkCore;
using WArena.Mvc.Data;
using WArena.Mvc.Models;

namespace WArena.Mvc.Repositories;

public class AchievementRepository(ApplicationDbContext context) : IAchievementRepository
{
    public async Task<IReadOnlyList<Achievement>> GetAllAsync() =>
        await context.Achievements.OrderBy(x => x.Id).AsNoTracking().ToListAsync();

    public async Task<IReadOnlyList<UserAchievement>> GetForUserAsync(string userId) =>
        await context.UserAchievements.Include(x => x.Achievement)
            .Where(x => x.UserId == userId).OrderByDescending(x => x.EarnedAtUtc).AsNoTracking().ToListAsync();

    public async Task AwardEligibleAsync(ApplicationUser user, int correctAnswers)
    {
        var earnedIds = await context.UserAchievements.Where(x => x.UserId == user.Id).Select(x => x.AchievementId).ToListAsync();
        var eligible = await context.Achievements.Where(x => !earnedIds.Contains(x.Id) &&
            (x.RequiredCorrectAnswers == 0 || correctAnswers >= x.RequiredCorrectAnswers) &&
            (x.RequiredStreak == 0 || user.CurrentStreak >= x.RequiredStreak)).ToListAsync();

        foreach (var achievement in eligible)
        {
            context.UserAchievements.Add(new UserAchievement { UserId = user.Id, AchievementId = achievement.Id });
        }
        if (eligible.Count > 0) await context.SaveChangesAsync();
    }
}
