using Microsoft.EntityFrameworkCore;
using WArena.Mvc.Data;
using WArena.Mvc.Models;

namespace WArena.Mvc.Repositories;

public class QuestionRepository(ApplicationDbContext context) : IQuestionRepository
{
    public async Task<IReadOnlyList<Question>> SearchAsync(QuestionSearchCriteria criteria)
    {
        var query = context.Questions.Include(x => x.Category).AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(criteria.Search))
        {
            var term = criteria.Search.Trim();
            query = query.Where(x => x.Title.Contains(term) || x.Prompt.Contains(term));
        }
        if (criteria.CategoryId.HasValue) query = query.Where(x => x.CategoryId == criteria.CategoryId.Value);
        if (criteria.Difficulty.HasValue) query = query.Where(x => x.Difficulty == criteria.Difficulty.Value);
        if (criteria.MinPoints.HasValue) query = query.Where(x => x.Points >= criteria.MinPoints.Value);
        if (criteria.MaxPoints.HasValue) query = query.Where(x => x.Points <= criteria.MaxPoints.Value);

        return await query.OrderBy(x => x.Category!.Name).ThenBy(x => x.Title).ToListAsync();
    }

    public async Task<IReadOnlyList<Question>> GetAllAsync() =>
        await context.Questions.Include(x => x.Category).OrderBy(x => x.Category!.Name).ThenBy(x => x.Title).AsNoTracking().ToListAsync();

    public Task<Question?> GetByIdAsync(int id) =>
        context.Questions.Include(x => x.Category).FirstOrDefaultAsync(x => x.Id == id);

    public async Task AddAsync(Question question)
    {
        context.Questions.Add(question);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Question question)
    {
        context.Questions.Update(question);
        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Question question)
    {
        context.Questions.Remove(question);
        await context.SaveChangesAsync();
    }
}
