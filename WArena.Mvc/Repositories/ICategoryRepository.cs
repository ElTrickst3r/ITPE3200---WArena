using WArena.Mvc.Models;

namespace WArena.Mvc.Repositories;

public interface ICategoryRepository
{
    Task<IReadOnlyList<Category>> GetAllAsync();
    Task<Category?> GetByIdAsync(int id);
    Task<bool> NameExistsAsync(string name, int? ignoreId = null);
    Task AddAsync(Category category);
    Task UpdateAsync(Category category);
    Task<bool> DeleteAsync(int id);
}
