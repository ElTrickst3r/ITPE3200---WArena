using System.ComponentModel.DataAnnotations;
namespace WArena.Api.Dtos;

public class AdminUpdateUserDto
{
    [Required, StringLength(30, MinimumLength = 3)]
    [RegularExpression(@"^[A-Za-z0-9._-]+$")]
    public string Username { get; set; } = string.Empty;

    [Required]
    public string Role { get; set; } = "Student";

    [MinLength(8)]
    public string? NewPassword { get; set; }
}

public record AdminUserDto(string Id, string Username, string? Email, int TotalXp, string Role);