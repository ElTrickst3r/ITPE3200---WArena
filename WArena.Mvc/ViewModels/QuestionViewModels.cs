using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using WArena.Mvc.Models;

namespace WArena.Mvc.ViewModels;

public class QuestionFormViewModel
{
    public int Id { get; set; }

    [Required, StringLength(120)]
    public string Title { get; set; } = string.Empty;

    [Required, StringLength(1200)]
    public string Prompt { get; set; } = string.Empty;

    [Required, StringLength(500)]
    [Display(Name = "Correct answer")]
    public string CorrectAnswer { get; set; } = string.Empty;

    [Display(Name = "Question type")]
    public QuestionType Type { get; set; }

    public Difficulty Difficulty { get; set; }

    [Range(10, 1000)]
    [Display(Name = "XP reward")]
    public int Points { get; set; } = 100;

    [Display(Name = "Option A")] public string? OptionA { get; set; }
    [Display(Name = "Option B")] public string? OptionB { get; set; }
    [Display(Name = "Option C")] public string? OptionC { get; set; }
    [Display(Name = "Option D")] public string? OptionD { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Choose a category.")]
    [Display(Name = "Category")]
    public int CategoryId { get; set; }

    public IEnumerable<SelectListItem> Categories { get; set; } = Array.Empty<SelectListItem>();
}

public class QuestionSearchViewModel
{
    public HashSet<int> CompletedQuestionIds { get; set; } = new();
    public string? Search { get; set; }
    public int? CategoryId { get; set; }
    public Difficulty? Difficulty { get; set; }
    public int? MinPoints { get; set; }
    public int? MaxPoints { get; set; }
    public IReadOnlyList<Question> Questions { get; set; } = Array.Empty<Question>();
    public IEnumerable<SelectListItem> Categories { get; set; } = Array.Empty<SelectListItem>();
}

public class PlayQuestionViewModel
{
    public int QuestionId { get; set; }
    public bool IsCompleted { get; set; }
    public int? ChallengeSetId { get; set; }
    public int QuestionIndex { get; set; }
    public int TotalQuestions { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Prompt { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public QuestionType Type { get; set; }
    public Difficulty Difficulty { get; set; }
    public int Points { get; set; }
    public IReadOnlyList<string> Options { get; set; } = Array.Empty<string>();

    [Required(ErrorMessage = "Choose or enter an answer.")]
    [StringLength(500)]
    public string Answer { get; set; } = string.Empty;
}

public class AnswerResultViewModel
{
    public bool IsCorrect { get; set; }
    public int PointsEarned { get; set; }
    public int TotalXp { get; set; }
    public string SubmittedAnswer { get; set; } = string.Empty;
    public int? NextQuestionId { get; set; }
    public int? ChallengeSetId { get; set; }
    public int NextQuestionIndex { get; set; }
}
