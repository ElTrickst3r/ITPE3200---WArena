namespace WArena.Mvc.Models;

public class UserAchievement
{
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }

    public int AchievementId { get; set; }
    public Achievement? Achievement { get; set; }

    public DateTime EarnedAtUtc { get; set; } = DateTime.UtcNow;
}
