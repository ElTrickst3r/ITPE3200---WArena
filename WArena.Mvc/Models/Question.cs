using System.ComponentModel.DataAnnotations;

namespace WArena.Mvc.Models;

public enum QuestionType
{
    [Display(Name = "Short answer")]
    ShortAnswer = 0,
    [Display(Name = "Multiple choice")]
    MultipleChoice = 1
}

public enum Difficulty
{
    Easy = 0,
    Medium = 1,
    Hard = 2
}

public class Question
{
    public int Id { get; set; }

    [Required, StringLength(120)]
    public string Title { get; set; } = string.Empty;

    [Required, StringLength(1200)]
    public string Prompt { get; set; } = string.Empty;

    [Required, StringLength(500)]
    public string CorrectAnswer { get; set; } = string.Empty;

    public QuestionType Type { get; set; }
    public Difficulty Difficulty { get; set; }

    [Range(10, 1000)]
    public int Points { get; set; } = 100;

    [StringLength(250)] public string? OptionA { get; set; }
    [StringLength(250)] public string? OptionB { get; set; }
    [StringLength(250)] public string? OptionC { get; set; }
    [StringLength(250)] public string? OptionD { get; set; }

    public int CategoryId { get; set; }
    public Category? Category { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<ChallengeSetQuestion> ChallengeSetQuestions { get; set; } = new List<ChallengeSetQuestion>();
    public ICollection<Submission> Submissions { get; set; } = new List<Submission>();
}
