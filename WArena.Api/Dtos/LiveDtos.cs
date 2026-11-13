using System.ComponentModel.DataAnnotations;
namespace WArena.Api.Dtos;

public class CreateLiveRoomDto
{
    [Range(1, int.MaxValue)] public int ChallengeSetId { get; set; }
    public string Theme { get; set; } = "random";
    [Range(5, 60)] public int TimeLimitSeconds { get; set; } = 20;
}

public record JoinRoomDto(string? DisplayName = null);
public record LiveAnswerDto([Required, StringLength(500)] string Answer, [Range(0, 60000)] int ElapsedMs);
public record LeaderboardRowDto(string UserId, string DisplayName, int Score, int CorrectAnswers, int TotalResponseTimeMs, int Rank, int Lane);
public record LiveQuestionDto(int Id, string Title, string Prompt, string Type, IReadOnlyList<string> Options, int Index, int Total, int TimeLimitSeconds);
public record LiveRoomDto(
    string Code,
    string Theme,
    string Status,
    int CurrentQuestionIndex,
    string ChallengeSetName,
    string HostUserId,
    LiveQuestionDto? CurrentQuestion,
    IReadOnlyList<LeaderboardRowDto> Leaderboard,
    int ParticipantCount,
    int CurrentAnswerCount,
    int MaxPossibleScore);
