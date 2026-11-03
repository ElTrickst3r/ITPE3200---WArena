using System.ComponentModel.DataAnnotations;
namespace WArena.Api.Dtos;
public record CategoryDto(int Id,string Name,string? Description);
public class CategoryUpsertDto
{
    [Required, StringLength(60)] public string Name { get; set; } = string.Empty;
    [StringLength(250)] public string? Description { get; set; }
}
