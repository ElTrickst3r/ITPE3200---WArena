using System.ComponentModel.DataAnnotations;
namespace WArena.Api.Models;
public class ChallengeSet
{
    public int Id { get; set; }
    [Required, StringLength(100)] public string Name { get; set; } = string.Empty;
    [StringLength(500)] public string? Description { get; set; }
    public bool IsPublished { get; set; } = true;
    public ICollection<ChallengeSetQuestion> ChallengeSetQuestions { get; set; } = new List<ChallengeSetQuestion>();
}
public class ChallengeSetQuestion
{
    public int ChallengeSetId { get; set; }
    public ChallengeSet? ChallengeSet { get; set; }
    public int QuestionId { get; set; }
    public Question? Question { get; set; }
    public int Order { get; set; }
}
