using WArena.Mvc.Models;

namespace WArena.Mvc.Repositories;

public interface ISubmissionRepository
{
    Task<bool> HasCorrectSubmissionAsync(string userId, int questionId);
    Task AddAsync(Submission submission);
    Task<IReadOnlyList<Submission>> GetForUserAsync(string userId);
    Task<int> CountCorrectForUserAsync(string userId);
    Task<int> CountAttemptsForUserAsync(string userId);
    Task<int> CountCompletedQuestionsForSetAsync(string userId, int challengeSetId);
}
