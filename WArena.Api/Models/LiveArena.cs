using System.ComponentModel.DataAnnotations;
namespace WArena.Api.Models;

public class LiveRoom
{
    public int Id { get; set; }
    [Required, StringLength(8)] public string Code { get; set; } = string.Empty;
    public int ChallengeSetId { get; set; }
    public ChallengeSet? ChallengeSet { get; set; }
    [Required] public string HostUserId { get; set; } = string.Empty;
    [Required, StringLength(20)] public string Theme { get; set; } = "cars";
    [Required, StringLength(20)] public string Status { get; set; } = "Waiting";
    public int CurrentQuestionIndex { get; set; }
    public int QuestionTimeLimitSeconds { get; set; } = 20;
    public DateTime? QuestionStartedAtUtc { get; set; }
    public ICollection<LiveParticipant> Participants { get; set; } = new List<LiveParticipant>();
    public ICollection<LiveAnswer> Answers { get; set; } = new List<LiveAnswer>();
}

public class LiveParticipant
{
    public int Id { get; set; }
    public int LiveRoomId { get; set; }
    public LiveRoom? LiveRoom { get; set; }
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }
    public int TotalScore { get; set; }
    public int CorrectAnswers { get; set; }
    public int TotalResponseTimeMs { get; set; }
}

public class LiveAnswer
{
    public int Id { get; set; }
    public int LiveRoomId { get; set; }
    public LiveRoom? LiveRoom { get; set; }
    public int QuestionId { get; set; }
    public string UserId { get; set; } = string.Empty;
    [StringLength(500)] public string Answer { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
    public int Score { get; set; }
    public int ResponseTimeMs { get; set; }
}
