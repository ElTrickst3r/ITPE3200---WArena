using WArena.Api.Models;

namespace WArena.Api.Services;

public interface IGamificationService
{
    (bool IsCorrect,int Points) ScorePractice(Question question,string answer,bool alreadyCompleted);
    int ScoreLive(bool isCorrect,int elapsedMs,int timeLimitSeconds,int maxPoints=1000,int basePoints=500);
    int UpdateStreak(int currentStreak,DateTime? lastActivityUtc,DateTime activityUtc);
}

public class GamificationService : IGamificationService
{
    public (bool IsCorrect,int Points) ScorePractice(Question question,string answer,bool alreadyCompleted)
    {
        var correct=!string.IsNullOrWhiteSpace(answer)&&string.Equals(answer.Trim(),question.CorrectAnswer.Trim(),StringComparison.OrdinalIgnoreCase);
        var points=!correct ? 0 : alreadyCompleted ? CalculateRepeatPracticeXp(question.Points) : question.Points;
        return (correct, points);
    }

    private static int CalculateRepeatPracticeXp(int questionPoints)
    {
        if(questionPoints<=0)return 0;
        return Math.Max(1,(int)Math.Round(questionPoints*0.20,MidpointRounding.AwayFromZero));
    }

    public int ScoreLive(bool isCorrect,int elapsedMs,int timeLimitSeconds,int maxPoints=1000,int basePoints=500)
    {
        if(!isCorrect||elapsedMs<0||timeLimitSeconds<=0||basePoints<0||maxPoints<basePoints)return 0;
        var limit=timeLimitSeconds*1000;
        if(elapsedMs>limit)return 0;
        var ratio=1d-(double)elapsedMs/limit;
        return Math.Clamp(basePoints+(int)Math.Round((maxPoints-basePoints)*ratio),basePoints,maxPoints);
    }

    public int UpdateStreak(int currentStreak,DateTime? lastActivityUtc,DateTime activityUtc)
    {
        if(lastActivityUtc is null)return 1;
        var days=(activityUtc.Date-lastActivityUtc.Value.Date).Days;
        return days switch{<=0=>Math.Max(1,currentStreak),1=>Math.Max(0,currentStreak)+1,_=>1};
    }
}
