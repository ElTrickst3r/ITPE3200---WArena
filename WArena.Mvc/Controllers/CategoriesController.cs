using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WArena.Mvc.Models;
using WArena.Mvc.Repositories;
using WArena.Mvc.ViewModels;

namespace WArena.Mvc.Controllers;

[Authorize(Roles = "Admin,Teacher")]
public class CategoriesController(ICategoryRepository repository, ILogger<CategoriesController> logger) : Controller
{
    public async Task<IActionResult> Index() => View(await repository.GetAllAsync());

    [HttpGet]
    public IActionResult Create() => View(new CategoryFormViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CategoryFormViewModel model)
    {
        if (!string.IsNullOrWhiteSpace(model.Name) && await repository.NameExistsAsync(model.Name))
            ModelState.AddModelError(nameof(model.Name), "A category with this name already exists.");
        if (!ModelState.IsValid) return View(model);

        await repository.AddAsync(new Category { Name = model.Name.Trim(), Description = model.Description?.Trim() });
        logger.LogInformation("Category {CategoryName} created", model.Name);
        TempData["Message"] = "Category created.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var category = await repository.GetByIdAsync(id);
        return category is null ? NotFound() : View(new CategoryFormViewModel { Id = category.Id, Name = category.Name, Description = category.Description });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, CategoryFormViewModel model)
    {
        if (id != model.Id) return BadRequest();
        if (!string.IsNullOrWhiteSpace(model.Name) && await repository.NameExistsAsync(model.Name, id))
            ModelState.AddModelError(nameof(model.Name), "A category with this name already exists.");
        if (!ModelState.IsValid) return View(model);

        var category = await repository.GetByIdAsync(id);
        if (category is null) return NotFound();
        category.Name = model.Name.Trim();
        category.Description = model.Description?.Trim();
        await repository.UpdateAsync(category);
        logger.LogInformation("Category {CategoryId} updated", id);
        TempData["Message"] = "Category updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var category = await repository.GetByIdAsync(id);
        return category is null ? NotFound() : View(category);
    }

    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        if (!await repository.DeleteAsync(id))
        {
            TempData["Error"] = "The category cannot be deleted while questions use it.";
            return RedirectToAction(nameof(Index));
        }
        logger.LogInformation("Category {CategoryId} deleted", id);
        TempData["Message"] = "Category deleted.";
        return RedirectToAction(nameof(Index));
    }
}
