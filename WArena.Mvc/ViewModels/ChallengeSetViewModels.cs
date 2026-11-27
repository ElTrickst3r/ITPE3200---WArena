using System.ComponentModel.DataAnnotations;
using WArena.Mvc.Models;

namespace WArena.Mvc.ViewModels;

public class ChallengeSetFormViewModel
{
    public int Id { get; set; }

    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [Display(Name = "Published")]
    public bool IsPublished { get; set; } = true;

    [MinLength(1, ErrorMessage = "Choose at least one question.")]
    public List<int> QuestionIds { get; set; } = new();

    public IReadOnlyList<Question> AvailableQuestions { get; set; } = Array.Empty<Question>();
}

public class ChallengeSetSearchViewModel
{
    public HashSet<int> CompletedQuestionIds { get; set; } = new();
    public string? Search { get; set; }
    public int? MinQuestions { get; set; }
    public bool? IsPublished { get; set; }
    public IReadOnlyList<ChallengeSet> ChallengeSets { get; set; } = Array.Empty<ChallengeSet>();
}

public class ChallengeSetDetailsViewModel
{
    public HashSet<int> CompletedQuestionIds { get; set; } = new();
    public ChallengeSet ChallengeSet { get; set; } = new();
    public int CompletedQuestions { get; set; }
    public int TotalQuestions { get; set; }
}
