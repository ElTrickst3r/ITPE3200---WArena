using System.ComponentModel.DataAnnotations;

namespace WArena.Mvc.Models;

public class Category
{
    public int Id { get; set; }

    [Required, StringLength(60)]
    public string Name { get; set; } = string.Empty;

    [StringLength(250)]
    public string? Description { get; set; }

    public ICollection<Question> Questions { get; set; } = new List<Question>();
}
