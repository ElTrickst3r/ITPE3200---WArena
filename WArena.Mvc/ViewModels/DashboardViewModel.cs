using WArena.Mvc.Models;

namespace WArena.Mvc.ViewModels;

public class DashboardViewModel
{
    public int TotalXp { get; set; }
    public int CompletedQuestions { get; set; }
    public int TotalAttempts { get; set; }
    public double Accuracy { get; set; }
    public int CurrentStreak { get; set; }
    public IReadOnlyList<ChallengeSet> ChallengeSets { get; set; } = Array.Empty<ChallengeSet>();
    public IReadOnlyList<UserAchievement> RecentAchievements { get; set; } = Array.Empty<UserAchievement>();
}
