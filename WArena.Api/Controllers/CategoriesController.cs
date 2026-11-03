using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WArena.Api.Dtos;
using WArena.Api.Models;
using WArena.Api.Repositories;

namespace WArena.Api.Controllers;

[ApiController,Route("api/categories"),Authorize]
public class CategoriesController(ICategoryRepository repo,ILogger<CategoriesController> logger):ControllerBase
{
    [HttpGet] public async Task<ActionResult<IEnumerable<CategoryDto>>> Get()=>Ok((await repo.GetAllAsync()).Select(x=>new CategoryDto(x.Id,x.Name,x.Description)));
    [HttpGet("{id:int}")] public async Task<ActionResult<CategoryDto>> GetOne(int id){var x=await repo.GetAsync(id);return x is null?NotFound():Ok(new CategoryDto(x.Id,x.Name,x.Description));}
    [HttpPost,Authorize(Roles="Admin,Teacher")] public async Task<ActionResult<CategoryDto>> Create(CategoryUpsertDto dto){var x=new Category{Name=dto.Name.Trim(),Description=dto.Description?.Trim()};await repo.AddAsync(x);logger.LogInformation("API category {Id} created",x.Id);return CreatedAtAction(nameof(GetOne),new{id=x.Id},new CategoryDto(x.Id,x.Name,x.Description));}
    [HttpPut("{id:int}"),Authorize(Roles="Admin,Teacher")] public async Task<IActionResult> Update(int id,CategoryUpsertDto dto){var x=await repo.GetAsync(id);if(x is null)return NotFound();x.Name=dto.Name.Trim();x.Description=dto.Description?.Trim();await repo.SaveAsync();return NoContent();}
    [HttpDelete("{id:int}"),Authorize(Roles="Admin,Teacher")] public async Task<IActionResult> Delete(int id)=>await repo.DeleteAsync(id)?NoContent():Conflict(new{message="Category not found or still used by questions."});
}
