using Microsoft.EntityFrameworkCore;
using WArena.Mvc.Data;
using WArena.Mvc.Models;

namespace WArena.Mvc.Repositories;

public class CategoryRepository(ApplicationDbContext context) : ICategoryRepository
{
    public async Task<IReadOnlyList<Category>> GetAllAsync() =>
        await context.Categories.OrderBy(x => x.Name).AsNoTracking().ToListAsync();

    public Task<Category?> GetByIdAsync(int id) =>
        context.Categories.FirstOrDefaultAsync(x => x.Id == id);

    public async Task<bool> NameExistsAsync(string name, int? ignoreId = null)
    {
        var names = await context.Categories
            .Where(x => !ignoreId.HasValue || x.Id != ignoreId.Value)
            .Select(x => x.Name).ToListAsync();
        return names.Any(x => string.Equals(x.Trim(), name.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    public async Task AddAsync(Category category)
    {
        context.Categories.Add(category);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Category category)
    {
        context.Categories.Update(category);
        await context.SaveChangesAsync();
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var category = await context.Categories.Include(x => x.Questions).FirstOrDefaultAsync(x => x.Id == id);
        if (category is null || category.Questions.Count > 0) return false;
        context.Categories.Remove(category);
        await context.SaveChangesAsync();
        return true;
    }
}
