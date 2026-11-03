using WArena.Mvc.Models;

namespace WArena.Mvc.Repositories;

public interface IAchievementRepository
{
    Task<IReadOnlyList<Achievement>> GetAllAsync();
    Task<IReadOnlyList<UserAchievement>> GetForUserAsync(string userId);
    Task AwardEligibleAsync(ApplicationUser user, int correctAnswers);
}
