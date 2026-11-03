using System.ComponentModel.DataAnnotations;
namespace WArena.Api.Dtos;

public record LoginDto([Required, EmailAddress] string Email, [Required] string Password);

public record RegisterDto(
    [Required, StringLength(30, MinimumLength = 3), RegularExpression(@"^[A-Za-z0-9._-]+$")] string Username,
    [Required, EmailAddress] string Email,
    [Required, MinLength(8)] string Password);

public record UpdateUsernameDto(
    [Required, StringLength(30, MinimumLength = 3), RegularExpression(@"^[A-Za-z0-9._-]+$")] string Username);

public record ChangePasswordDto(
    [Required] string CurrentPassword,
    [Required, MinLength(8)] string NewPassword);

public record AuthResponseDto(string Token, string UserId, string Username, string Email, string Role, int TotalXp, int CurrentStreak);
