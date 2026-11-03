using Microsoft.AspNetCore.Identity;

namespace WArena.Api.Models;

public class ApplicationUser : IdentityUser
{
    public string DisplayName { get; set; } = string.Empty;
    public int TotalXp { get; set; }
    public int CurrentStreak { get; set; }
    public DateTime? LastActivityUtc { get; set; }
    public ICollection<Submission> Submissions { get; set; } = new List<Submission>();
}
