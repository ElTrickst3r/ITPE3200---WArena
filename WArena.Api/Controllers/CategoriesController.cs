using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WArena.Api.Dtos;
using WArena.Api.Models;
using WArena.Api.Repositories;

namespace WArena.Api.Controllers;

[ApiController, Route("api/categories"), Authorize]
public class CategoriesController(ICategoryRepository repo, ILogger<CategoriesController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CategoryDto>>> Get()
    {
        var categories = await repo.GetAllAsync();
        return Ok(categories.Select(x => new CategoryDto(x.Id, x.Name, x.Description)));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CategoryDto>> GetOne(int id)
    {
        var x = await repo.GetAsync(id);
        return x is null ? NotFound() : Ok(new CategoryDto(x.Id, x.Name, x.Description));
    }

    [HttpPost, Authorize(Roles = "Admin,Teacher")]
    public async Task<ActionResult<CategoryDto>> Create(CategoryUpsertDto dto)
    {
        if (await repo.NameExistsAsync(dto.Name))
        {
            logger.LogWarning("API category creation rejected: name {Name} already exists", dto.Name);
            return Conflict(new { message = "A category with this name already exists." });
        }

        var x = new Category { Name = dto.Name.Trim(), Description = dto.Description?.Trim() };
        await repo.AddAsync(x);
        logger.LogInformation("API category {Id} created", x.Id);
        return CreatedAtAction(nameof(GetOne), new { id = x.Id }, new CategoryDto(x.Id, x.Name, x.Description));
    }

    [HttpPut("{id:int}"), Authorize(Roles = "Admin,Teacher")]
    public async Task<IActionResult> Update(int id, CategoryUpsertDto dto)
    {
        var x = await repo.GetAsync(id);
        if (x is null) return NotFound();
        if (await repo.NameExistsAsync(dto.Name, id))
        {
            logger.LogWarning("API category {Id} update rejected: name {Name} already exists", id, dto.Name);
            return Conflict(new { message = "A category with this name already exists." });
        }

        x.Name = dto.Name.Trim();
        x.Description = dto.Description?.Trim();
        await repo.SaveAsync();
        logger.LogInformation("API category {Id} updated", id);
        return NoContent();
    }

    [HttpDelete("{id:int}"), Authorize(Roles = "Admin,Teacher")]
    public async Task<IActionResult> Delete(int id)
    {
        if (!await repo.ExistsAsync(id)) return NotFound();
        if (!await repo.DeleteAsync(id))
        {
            logger.LogWarning("API category {Id} deletion rejected: still used by questions", id);
            return Conflict(new { message = "Category is still used by questions." });
        }

        logger.LogInformation("API category {Id} deleted", id);
        return NoContent();
    }
}