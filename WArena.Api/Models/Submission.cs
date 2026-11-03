using System.ComponentModel.DataAnnotations;
namespace WArena.Api.Models;
public class Submission
{
    public int Id { get; set; }
    public int QuestionId { get; set; }
    public Question? Question { get; set; }
    [Required, StringLength(500)] public string Answer { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
    public int PointsEarned { get; set; }
    public DateTime SubmittedAtUtc { get; set; } = DateTime.UtcNow;
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }
}
