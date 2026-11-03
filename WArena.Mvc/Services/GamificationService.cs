using WArena.Mvc.Models;

namespace WArena.Mvc.Services;

public record PracticeScoreResult(bool IsCorrect, int PointsEarned);

public interface IGamificationService
{
    PracticeScoreResult ScorePracticeAnswer(Question question, string answer, bool alreadyCompleted);
    int CalculateLiveScore(bool isCorrect, int elapsedMs, int timeLimitSeconds, int maxPoints = 1000, int basePoints = 500);
    int UpdateStreak(int currentStreak, DateTime? lastActivityUtc, DateTime activityUtc);
}

public class GamificationService : IGamificationService
{
    public PracticeScoreResult ScorePracticeAnswer(Question question, string answer, bool alreadyCompleted)
    {
        var normalizedAnswer = (answer ?? string.Empty).Trim();
        var normalizedCorrect = question.CorrectAnswer.Trim();
        var correct = normalizedAnswer.Length > 0 && string.Equals(normalizedAnswer, normalizedCorrect, StringComparison.OrdinalIgnoreCase);
        var points = !correct
            ? 0
            : alreadyCompleted
                ? CalculateRepeatPracticeXp(question.Points)
                : question.Points;
        return new PracticeScoreResult(correct, points);
    }

    private static int CalculateRepeatPracticeXp(int questionPoints)
    {
        if (questionPoints <= 0) return 0;
        return Math.Max(1, (int)Math.Round(questionPoints * 0.20, MidpointRounding.AwayFromZero));
    }

    public int CalculateLiveScore(bool isCorrect, int elapsedMs, int timeLimitSeconds, int maxPoints = 1000, int basePoints = 500)
    {
        if (!isCorrect || elapsedMs < 0 || timeLimitSeconds <= 0 || maxPoints < basePoints || basePoints < 0)
            return 0;

        var limitMs = timeLimitSeconds * 1000;
        if (elapsedMs > limitMs)
            return 0;

        var remainingRatio = 1d - ((double)elapsedMs / limitMs);
        var speedBonus = (int)Math.Round((maxPoints - basePoints) * remainingRatio);
        return Math.Clamp(basePoints + speedBonus, basePoints, maxPoints);
    }

    public int UpdateStreak(int currentStreak, DateTime? lastActivityUtc, DateTime activityUtc)
    {
        if (lastActivityUtc is null)
            return 1;

        var days = (activityUtc.Date - lastActivityUtc.Value.Date).Days;
        return days switch
        {
            <= 0 => Math.Max(currentStreak, 1),
            1 => Math.Max(currentStreak, 0) + 1,
            _ => 1
        };
    }
}
