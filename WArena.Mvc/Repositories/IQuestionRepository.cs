using WArena.Mvc.Models;

namespace WArena.Mvc.Repositories;

public record QuestionSearchCriteria(string? Search, int? CategoryId, Difficulty? Difficulty, int? MinPoints, int? MaxPoints);

public interface IQuestionRepository
{
    Task<IReadOnlyList<Question>> SearchAsync(QuestionSearchCriteria criteria);
    Task<IReadOnlyList<Question>> GetAllAsync();
    Task<Question?> GetByIdAsync(int id);
    Task AddAsync(Question question);
    Task UpdateAsync(Question question);
    Task DeleteAsync(Question question);
}
