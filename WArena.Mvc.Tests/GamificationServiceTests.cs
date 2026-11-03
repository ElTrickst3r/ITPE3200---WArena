using Xunit;
using WArena.Mvc.Models;
using WArena.Mvc.Services;

namespace WArena.Mvc.Tests;

public class GamificationServiceTests
{
    private readonly GamificationService _service = new();
    private static Question Question(int points = 100) => new() { CorrectAnswer = "Model View Controller", Points = points };

    // Positive tests
    [Fact]
    public void Positive_CorrectFirstAttempt_AwardsQuestionPoints()
    {
        var result = _service.ScorePracticeAnswer(Question(150), "Model View Controller", alreadyCompleted: false);
        Assert.True(result.IsCorrect);
        Assert.Equal(150, result.PointsEarned);
    }

    [Fact]
    public void Positive_CorrectAnswer_IsCaseInsensitive()
    {
        var result = _service.ScorePracticeAnswer(Question(), "model view controller", false);
        Assert.True(result.IsCorrect);
    }

    [Fact]
    public void Positive_CorrectAnswer_TrimsWhitespace()
    {
        var result = _service.ScorePracticeAnswer(Question(), "  Model View Controller  ", false);
        Assert.True(result.IsCorrect);
    }

    [Fact]
    public void Positive_RepeatedCorrectAnswer_AwardsTwentyPercentXp()
    {
        var result = _service.ScorePracticeAnswer(Question(200), "Model View Controller", alreadyCompleted: true);
        Assert.True(result.IsCorrect);
        Assert.Equal(40, result.PointsEarned);
    }

    [Fact]
    public void Positive_FastCorrectLiveAnswer_GetsMoreThanBasePoints()
    {
        var score = _service.CalculateLiveScore(true, 2_000, 20, maxPoints: 1000, basePoints: 500);
        Assert.InRange(score, 501, 1000);
    }

    [Fact]
    public void Positive_FirstLearningDay_StartsOneDayStreak()
    {
        var streak = _service.UpdateStreak(0, null, new DateTime(2026, 10, 4, 12, 0, 0, DateTimeKind.Utc));
        Assert.Equal(1, streak);
    }

    [Fact]
    public void Positive_ConsecutiveDay_IncrementsStreak()
    {
        var streak = _service.UpdateStreak(4, new DateTime(2026, 10, 3, 18, 0, 0, DateTimeKind.Utc), new DateTime(2026, 10, 4, 8, 0, 0, DateTimeKind.Utc));
        Assert.Equal(5, streak);
    }

    // Negative / boundary tests
    [Fact]
    public void Negative_WrongPracticeAnswer_GetsZeroPoints()
    {
        var result = _service.ScorePracticeAnswer(Question(), "Controller View Model", false);
        Assert.False(result.IsCorrect);
        Assert.Equal(0, result.PointsEarned);
    }

    [Fact]
    public void Negative_BlankPracticeAnswer_IsNotCorrect()
    {
        var result = _service.ScorePracticeAnswer(Question(), "   ", false);
        Assert.False(result.IsCorrect);
        Assert.Equal(0, result.PointsEarned);
    }

    [Fact]
    public void Negative_WrongLiveAnswer_GetsZeroPointsEvenWhenFast()
    {
        Assert.Equal(0, _service.CalculateLiveScore(false, 200, 20));
    }

    [Fact]
    public void Negative_LateLiveAnswer_GetsZeroPoints()
    {
        Assert.Equal(0, _service.CalculateLiveScore(true, 20_001, 20));
    }

    [Fact]
    public void Negative_NegativeElapsedTime_IsRejected()
    {
        Assert.Equal(0, _service.CalculateLiveScore(true, -1, 20));
    }

    [Fact]
    public void Negative_InvalidTimeLimit_IsRejected()
    {
        Assert.Equal(0, _service.CalculateLiveScore(true, 1000, 0));
    }

    [Fact]
    public void Negative_MissedDay_ResetsStreakToOne()
    {
        var streak = _service.UpdateStreak(9, new DateTime(2026, 10, 1, 18, 0, 0, DateTimeKind.Utc), new DateTime(2026, 10, 4, 8, 0, 0, DateTimeKind.Utc));
        Assert.Equal(1, streak);
    }
}
