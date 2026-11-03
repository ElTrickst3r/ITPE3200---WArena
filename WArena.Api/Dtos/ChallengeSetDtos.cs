using System.ComponentModel.DataAnnotations;
namespace WArena.Api.Dtos;
public record ChallengeSetListDto(int Id,string Name,string? Description,bool IsPublished,int QuestionCount,int PossibleXp);
public record SetQuestionDto(int Id,string Title,string Prompt,string Category,string Difficulty,string Type,int Points,IReadOnlyList<string> Options);
public record ChallengeSetDetailsDto(int Id,string Name,string? Description,bool IsPublished,IReadOnlyList<SetQuestionDto> Questions);
public class ChallengeSetUpsertDto
{
    [Required, StringLength(100)] public string Name { get; set; } = string.Empty;
    [StringLength(500)] public string? Description { get; set; }
    public bool IsPublished { get; set; } = true;
    [MinLength(1)] public List<int> QuestionIds { get; set; } = new();
}
