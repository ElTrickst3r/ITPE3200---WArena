using System.ComponentModel.DataAnnotations;

namespace WArena.Mvc.ViewModels;

public class UserListItemViewModel
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public int TotalXp { get; set; }
}

public class AdminEditUserViewModel
{
    [Required]
    public string Id { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    [Required, StringLength(30, MinimumLength = 3)]
    [RegularExpression(@"^[A-Za-z0-9._-]+$", ErrorMessage = "Username can only contain letters, numbers, dots, underscores and hyphens.")]
    public string Username { get; set; } = string.Empty;

    [Required]
    public string Role { get; set; } = "Student";

    [DataType(DataType.Password), MinLength(8)]
    [Display(Name = "New password (optional)")]
    public string? NewPassword { get; set; }
}

public class DeleteUserViewModel
{
    public string Id { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}
