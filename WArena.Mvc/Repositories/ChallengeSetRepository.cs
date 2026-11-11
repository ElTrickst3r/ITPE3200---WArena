using Microsoft.EntityFrameworkCore;
using WArena.Mvc.Data;
using WArena.Mvc.Models;

namespace WArena.Mvc.Repositories;

public class ChallengeSetRepository(ApplicationDbContext context) : IChallengeSetRepository
{
    public async Task<IReadOnlyList<ChallengeSet>> SearchAsync(ChallengeSetSearchCriteria criteria, bool includeUnpublished)
    {
        var query = context.ChallengeSets
            .Include(x => x.ChallengeSetQuestions).ThenInclude(x => x.Question).ThenInclude(x => x!.Category)
            .AsNoTracking().AsQueryable();

        if (!includeUnpublished) query = query.Where(x => x.IsPublished);
        if (!string.IsNullOrWhiteSpace(criteria.Search))
        {
            var term = criteria.Search.Trim();
            query = query.Where(x => x.Name.Contains(term) || (x.Description != null && x.Description.Contains(term)));
        }
        if (criteria.MinQuestions.HasValue) query = query.Where(x => x.ChallengeSetQuestions.Count >= criteria.MinQuestions.Value);
        if (criteria.IsPublished.HasValue) query = query.Where(x => x.IsPublished == criteria.IsPublished.Value);

        return await query.OrderBy(x => x.Name).ToListAsync();
    }

    public Task<ChallengeSet?> GetByIdAsync(int id) =>
        context.ChallengeSets
            .Include(x => x.ChallengeSetQuestions.OrderBy(y => y.Order))
            .ThenInclude(x => x.Question).ThenInclude(x => x!.Category)
            .FirstOrDefaultAsync(x => x.Id == id);

    public async Task AddAsync(ChallengeSet set, IReadOnlyList<int> questionIds)
    {
        set.ChallengeSetQuestions = questionIds.Distinct().Select((id, index) => new ChallengeSetQuestion
        {
            QuestionId = id,
            Order = index + 1
        }).ToList();
        context.ChallengeSets.Add(set);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(ChallengeSet set, IReadOnlyList<int> questionIds)
    {
        var existingByQuestionId = set.ChallengeSetQuestions.ToDictionary(x => x.QuestionId);
        // Keep the saved order of retained questions, then append new selections.
        var orderedIds = set.ChallengeSetQuestions.OrderBy(x => x.Order)
            .Where(x => questionIds.Contains(x.QuestionId)).Select(x => x.QuestionId)
            .Concat(questionIds.Where(id => !existingByQuestionId.ContainsKey(id)))
            .Distinct().ToList();

        foreach (var link in set.ChallengeSetQuestions
                     .Where(x => !orderedIds.Contains(x.QuestionId))
                     .ToList())
        {
            context.ChallengeSetQuestions.Remove(link);
        }

        for (var index = 0; index < orderedIds.Count; index++)
        {
            var questionId = orderedIds[index];
            if (existingByQuestionId.TryGetValue(questionId, out var existing))
            {
                existing.Order = index + 1;
            }
            else
            {
                set.ChallengeSetQuestions.Add(new ChallengeSetQuestion
                {
                    ChallengeSetId = set.Id,
                    QuestionId = questionId,
                    Order = index + 1
                });
            }
        }

        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(ChallengeSet set)
    {
        context.ChallengeSets.Remove(set);
        await context.SaveChangesAsync();
    }
}
