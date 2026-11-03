using System.ComponentModel.DataAnnotations;
using WArena.Api.Models;
namespace WArena.Api.Dtos;
public record QuestionDto(int Id,string Title,string Prompt,string Category,Difficulty Difficulty,QuestionType Type,int Points,IReadOnlyList<string> Options);
public class QuestionUpsertDto
{
    [Required, StringLength(120)] public string Title { get; set; } = string.Empty;
    [Required, StringLength(1200)] public string Prompt { get; set; } = string.Empty;
    [Required, StringLength(500)] public string CorrectAnswer { get; set; } = string.Empty;
    public QuestionType Type { get; set; }
    public Difficulty Difficulty { get; set; }
    [Range(10,1000)] public int Points { get; set; } = 100;
    [StringLength(250)] public string? OptionA { get; set; }
    [StringLength(250)] public string? OptionB { get; set; }
    [StringLength(250)] public string? OptionC { get; set; }
    [StringLength(250)] public string? OptionD { get; set; }
    [Range(1,int.MaxValue)] public int CategoryId { get; set; }
}
public record AnswerRequestDto([Required] string Answer);
public record AnswerResultDto(bool IsCorrect,int PointsEarned,int TotalXp,int CurrentStreak);
