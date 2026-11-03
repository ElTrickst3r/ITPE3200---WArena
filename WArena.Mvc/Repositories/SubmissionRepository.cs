using Microsoft.EntityFrameworkCore;
using WArena.Mvc.Data;
using WArena.Mvc.Models;

namespace WArena.Mvc.Repositories;

public class SubmissionRepository(ApplicationDbContext context) : ISubmissionRepository
{
    public Task<bool> HasCorrectSubmissionAsync(string userId, int questionId) =>
        context.Submissions.AnyAsync(x => x.UserId == userId && x.QuestionId == questionId && x.IsCorrect);

    public async Task AddAsync(Submission submission)
    {
        context.Submissions.Add(submission);
        await context.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<Submission>> GetForUserAsync(string userId) =>
        await context.Submissions.Include(x => x.Question).ThenInclude(x => x!.Category)
            .Where(x => x.UserId == userId).OrderByDescending(x => x.SubmittedAtUtc).AsNoTracking().ToListAsync();

    public Task<int> CountCorrectForUserAsync(string userId) =>
        context.Submissions.CountAsync(x => x.UserId == userId && x.IsCorrect);

    public Task<int> CountAttemptsForUserAsync(string userId) =>
        context.Submissions.CountAsync(x => x.UserId == userId);

    public Task<int> CountCompletedQuestionsForSetAsync(string userId, int challengeSetId) =>
        context.Submissions.Where(x => x.UserId == userId && x.IsCorrect && x.Question!.ChallengeSetQuestions.Any(y => y.ChallengeSetId == challengeSetId))
            .Select(x => x.QuestionId).Distinct().CountAsync();
}
