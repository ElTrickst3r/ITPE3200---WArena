using WArena.Mvc.Models;

namespace WArena.Mvc.Repositories;

public interface ISubmissionRepository
{
    Task<bool> HasCorrectSubmissionAsync(string userId, int questionId);
    Task<HashSet<int>> GetCompletedQuestionIdsAsync(string userId, IReadOnlyCollection<int> questionIds);
    Task AddAsync(Submission submission);
    Task<Submission?> GetByIdForUserAsync(int id, string userId);
    Task<IReadOnlyList<Submission>> GetForUserAsync(string userId);
    Task<int> CountCorrectForUserAsync(string userId);
    Task<int> CountAttemptsForUserAsync(string userId);
    Task<int> CountCompletedQuestionsForSetAsync(string userId, int challengeSetId);
}
