using System.ComponentModel.DataAnnotations;

namespace WArena.Mvc.Models;

public class Achievement
{
    public int Id { get; set; }

    [Required, StringLength(80)]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(250)]
    public string Description { get; set; } = string.Empty;

    [Required, StringLength(20)]
    public string Icon { get; set; } = "🏆";

    public int RequiredCorrectAnswers { get; set; }
    public int RequiredStreak { get; set; }

    public ICollection<UserAchievement> UserAchievements { get; set; } = new List<UserAchievement>();
}
