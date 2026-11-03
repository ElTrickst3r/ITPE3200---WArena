using System.Security.Cryptography;
using WArena.Api.Dtos;
using WArena.Api.Models;
using WArena.Api.Repositories;

namespace WArena.Api.Services;

public interface ILiveArenaService
{
    Task<LiveRoom> CreateAsync(string hostUserId,CreateLiveRoomDto dto);
    Task<LiveRoom?> JoinAsync(string code,string userId);
    Task<LiveRoom?> StartAsync(string code,string hostUserId);
    Task<(LiveRoom? Room,int Score,string? Error)> SubmitAnswerAsync(string code,string userId,LiveAnswerDto dto);
    Task<LiveRoom?> NextAsync(string code,string hostUserId);
    Task<LiveRoomDto?> GetStateAsync(string code);
}

public class LiveArenaService(
    ILiveArenaRepository live,
    IChallengeSetRepository sets,
    IGamificationService gamification) : ILiveArenaService
{
    private static readonly string[] Themes=["cars","horses","rockets","boats"];

    public async Task<LiveRoom> CreateAsync(string hostUserId,CreateLiveRoomDto dto)
    {
        var set=await sets.GetAsync(dto.ChallengeSetId) ?? throw new KeyNotFoundException("Challenge set not found.");
        if(set.ChallengeSetQuestions.Count==0)throw new InvalidOperationException("The challenge set has no questions.");
        var requested=(dto.Theme??"random").ToLowerInvariant();
        var theme=requested=="random"?Themes[RandomNumberGenerator.GetInt32(Themes.Length)]:Themes.Contains(requested)?requested:"cars";
        LiveRoom room;
        do
        {
            room=new LiveRoom{Code=GenerateCode(),ChallengeSetId=set.Id,HostUserId=hostUserId,Theme=theme,QuestionTimeLimitSeconds=dto.TimeLimitSeconds};
        } while(await live.GetByCodeAsync(room.Code) is not null);
        await live.AddRoomAsync(room);
        return room;
    }

    public async Task<LiveRoom?> JoinAsync(string code,string userId)
    {
        var room=await live.GetByCodeWithStateAsync(code.ToUpperInvariant());
        if(room is null||room.Status=="Completed")return null;
        if(!await live.HasParticipantAsync(room.Id,userId))await live.AddParticipantAsync(new LiveParticipant{LiveRoomId=room.Id,UserId=userId});
        return await live.GetByCodeWithStateAsync(room.Code);
    }

    public async Task<LiveRoom?> StartAsync(string code,string hostUserId)
    {
        var room=await live.GetByCodeWithStateAsync(code.ToUpperInvariant());
        if(room is null||room.HostUserId!=hostUserId)return null;
        room.Status="Running";room.CurrentQuestionIndex=0;room.QuestionStartedAtUtc=DateTime.UtcNow;await live.SaveAsync();return room;
    }

    public async Task<(LiveRoom? Room,int Score,string? Error)> SubmitAnswerAsync(string code,string userId,LiveAnswerDto dto)
    {
        var room=await live.GetByCodeWithStateAsync(code.ToUpperInvariant());
        if(room is null)return (null,0,"Room not found.");
        if(room.Status!="Running")return (room,0,"The room is not accepting answers.");
        var ordered=room.ChallengeSet!.ChallengeSetQuestions.OrderBy(x=>x.Order).ToList();
        if(room.CurrentQuestionIndex<0||room.CurrentQuestionIndex>=ordered.Count)return (room,0,"No active question.");
        var question=ordered[room.CurrentQuestionIndex].Question!;
        if(!room.Participants.Any(x=>x.UserId==userId))return (room,0,"Join the room before answering.");
        if(await live.HasAnsweredAsync(room.Id,question.Id,userId))return (room,0,"You already answered this question.");

        var correct=string.Equals(dto.Answer.Trim(),question.CorrectAnswer.Trim(),StringComparison.OrdinalIgnoreCase);
        var elapsedMs=room.QuestionStartedAtUtc.HasValue
            ? Math.Max(0,(int)(DateTime.UtcNow-room.QuestionStartedAtUtc.Value).TotalMilliseconds)
            : dto.ElapsedMs;
        var withinTime=elapsedMs<=room.QuestionTimeLimitSeconds*1000;
        var countedCorrect=correct&&withinTime;
        var score=gamification.ScoreLive(countedCorrect,elapsedMs,room.QuestionTimeLimitSeconds);
        var participant=room.Participants.Single(x=>x.UserId==userId);
        participant.TotalScore+=score;
        participant.TotalResponseTimeMs+=elapsedMs;
        if(countedCorrect)participant.CorrectAnswers++;
        await live.AddAnswerAsync(new LiveAnswer{LiveRoomId=room.Id,QuestionId=question.Id,UserId=userId,Answer=dto.Answer.Trim(),IsCorrect=countedCorrect,Score=score,ResponseTimeMs=elapsedMs});
        return (await live.GetByCodeWithStateAsync(room.Code),score,null);
    }

    public async Task<LiveRoom?> NextAsync(string code,string hostUserId)
    {
        var room=await live.GetByCodeWithStateAsync(code.ToUpperInvariant());
        if(room is null||room.HostUserId!=hostUserId)return null;
        var total=room.ChallengeSet!.ChallengeSetQuestions.Count;
        if(room.CurrentQuestionIndex+1>=total){room.Status="Completed";room.QuestionStartedAtUtc=null;}
        else{room.CurrentQuestionIndex++;room.QuestionStartedAtUtc=DateTime.UtcNow;}
        await live.SaveAsync();return room;
    }

    public async Task<LiveRoomDto?> GetStateAsync(string code)
    {
        var room=await live.GetByCodeWithStateAsync(code.ToUpperInvariant());
        if(room is null)return null;
        var ordered=room.ChallengeSet!.ChallengeSetQuestions.OrderBy(x=>x.Order).ToList();
        LiveQuestionDto? question=null;
        if(room.Status=="Running"&&room.CurrentQuestionIndex>=0&&room.CurrentQuestionIndex<ordered.Count)
        {
            var q=ordered[room.CurrentQuestionIndex].Question!;
            question=new LiveQuestionDto(q.Id,q.Title,q.Prompt,q.Type.ToString(),new[]{q.OptionA,q.OptionB,q.OptionC,q.OptionD}.Where(x=>!string.IsNullOrWhiteSpace(x)).Select(x=>x!).ToList(),room.CurrentQuestionIndex,ordered.Count,room.QuestionTimeLimitSeconds);
        }
        var laneByUser = room.Participants
            .OrderBy(x => x.Id)
            .Select((participant, index) => new { participant.UserId, Lane = index + 1 })
            .ToDictionary(x => x.UserId, x => x.Lane);
        var leaderboard = room.Participants
            .OrderByDescending(x => x.TotalScore)
            .ThenByDescending(x => x.CorrectAnswers)
            .ThenBy(x => x.TotalResponseTimeMs)
            .Select((participant, index) => new LeaderboardRowDto(
                participant.UserId,
                participant.User?.UserName ?? participant.User?.DisplayName ?? participant.User?.Email ?? "Player",
                participant.TotalScore,
                participant.CorrectAnswers,
                participant.TotalResponseTimeMs,
                index + 1,
                laneByUser[participant.UserId]))
            .ToList();
        var currentAnswerCount=question is null?0:room.Answers.Count(x=>x.QuestionId==question.Id);
        var maxPossibleScore=Math.Max(1,ordered.Count*1000);
        return new LiveRoomDto(room.Code,room.Theme,room.Status,room.CurrentQuestionIndex,room.ChallengeSet!.Name,room.HostUserId,question,leaderboard,room.Participants.Count,currentAnswerCount,maxPossibleScore);
    }

    private static string GenerateCode()
    {
        const string chars="ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        return new string(Enumerable.Range(0,5).Select(_=>chars[RandomNumberGenerator.GetInt32(chars.Length)]).ToArray());
    }
}
