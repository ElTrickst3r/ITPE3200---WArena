namespace WArena.Api.Dtos;
public record SubmissionDto(int Id,int QuestionId,string Question,string Category,string Answer,bool IsCorrect,int PointsEarned,DateTime SubmittedAtUtc);
public record AchievementDto(int Id,string Name,string Description,string Icon,DateTime EarnedAtUtc);
public record DashboardDto(int TotalXp,int CompletedQuestions,int Attempts,double Accuracy,int CurrentStreak,IReadOnlyList<AchievementDto> RecentAchievements);
