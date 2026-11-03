namespace WArena.Mvc.Models;

public class ChallengeSetQuestion
{
    public int ChallengeSetId { get; set; }
    public ChallengeSet? ChallengeSet { get; set; }

    public int QuestionId { get; set; }
    public Question? Question { get; set; }

    public int Order { get; set; }
}
