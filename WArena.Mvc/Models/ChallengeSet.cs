using System.ComponentModel.DataAnnotations;

namespace WArena.Mvc.Models;

public class ChallengeSet
{
    public int Id { get; set; }

    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    public bool IsPublished { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<ChallengeSetQuestion> ChallengeSetQuestions { get; set; } = new List<ChallengeSetQuestion>();
}
