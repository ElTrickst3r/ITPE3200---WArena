using WArena.Mvc.Models;

namespace WArena.Mvc.Repositories;

public record ChallengeSetSearchCriteria(string? Search, int? MinQuestions, bool? IsPublished);

public interface IChallengeSetRepository
{
    Task<IReadOnlyList<ChallengeSet>> SearchAsync(ChallengeSetSearchCriteria criteria, bool includeUnpublished);
    Task<ChallengeSet?> GetByIdAsync(int id);
    Task AddAsync(ChallengeSet set, IReadOnlyList<int> questionIds);
    Task UpdateAsync(ChallengeSet set, IReadOnlyList<int> questionIds);
    Task DeleteAsync(ChallengeSet set);
}
