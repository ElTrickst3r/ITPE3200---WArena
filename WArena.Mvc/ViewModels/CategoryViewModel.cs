using System.ComponentModel.DataAnnotations;

namespace WArena.Mvc.ViewModels;

public class CategoryFormViewModel
{
    public int Id { get; set; }

    [Required, StringLength(60)]
    public string Name { get; set; } = string.Empty;

    [StringLength(250)]
    public string? Description { get; set; }
}
