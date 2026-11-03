using WArena.Api.Dtos;
using WArena.Api.Models;

namespace WArena.Api.Services;

public static class DtoMapper
{
    public static QuestionDto ToDto(this Question q)=>new(q.Id,q.Title,q.Prompt,q.Category?.Name??string.Empty,q.Difficulty,q.Type,q.Points,Options(q));
    public static SetQuestionDto ToSetQuestionDto(this Question q)=>new(q.Id,q.Title,q.Prompt,q.Category?.Name??string.Empty,q.Difficulty.ToString(),q.Type.ToString(),q.Points,Options(q));
    public static ChallengeSetListDto ToListDto(this ChallengeSet s)=>new(s.Id,s.Name,s.Description,s.IsPublished,s.ChallengeSetQuestions.Count,s.ChallengeSetQuestions.Sum(x=>x.Question?.Points??0));
    public static ChallengeSetDetailsDto ToDetailsDto(this ChallengeSet s)=>new(s.Id,s.Name,s.Description,s.IsPublished,s.ChallengeSetQuestions.OrderBy(x=>x.Order).Where(x=>x.Question!=null).Select(x=>x.Question!.ToSetQuestionDto()).ToList());
    private static IReadOnlyList<string> Options(Question q)=>new[]{q.OptionA,q.OptionB,q.OptionC,q.OptionD}.Where(x=>!string.IsNullOrWhiteSpace(x)).Select(x=>x!).ToList();
}
