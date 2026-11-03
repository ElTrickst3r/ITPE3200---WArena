using Microsoft.EntityFrameworkCore;
using WArena.Api.Data;
using WArena.Api.Models;

namespace WArena.Api.Repositories;

public interface ICategoryRepository
{
    Task<List<Category>> GetAllAsync(); Task<Category?> GetAsync(int id); Task AddAsync(Category entity); Task SaveAsync(); Task<bool> DeleteAsync(int id);
}
public class CategoryRepository(ApplicationDbContext db) : ICategoryRepository
{
    public Task<List<Category>> GetAllAsync() => db.Categories.OrderBy(x=>x.Name).AsNoTracking().ToListAsync();
    public Task<Category?> GetAsync(int id) => db.Categories.Include(x=>x.Questions).FirstOrDefaultAsync(x=>x.Id==id);
    public async Task AddAsync(Category entity){ db.Categories.Add(entity); await db.SaveChangesAsync(); }
    public Task SaveAsync() => db.SaveChangesAsync();
    public async Task<bool> DeleteAsync(int id){ var entity=await GetAsync(id); if(entity is null || entity.Questions.Count>0) return false; db.Remove(entity); await db.SaveChangesAsync(); return true; }
}

public record QuestionFilter(string? Search,int? CategoryId,Difficulty? Difficulty,int? MinPoints,int? MaxPoints);
public interface IQuestionRepository
{
    Task<List<Question>> SearchAsync(QuestionFilter filter); Task<List<Question>> GetAllAsync(); Task<Question?> GetAsync(int id); Task AddAsync(Question entity); Task SaveAsync(); Task DeleteAsync(Question entity);
}
public class QuestionRepository(ApplicationDbContext db) : IQuestionRepository
{
    public async Task<List<Question>> SearchAsync(QuestionFilter f){ var q=db.Questions.Include(x=>x.Category).AsNoTracking().AsQueryable(); if(!string.IsNullOrWhiteSpace(f.Search)){var term=f.Search.Trim();q=q.Where(x=>x.Title.Contains(term)||x.Prompt.Contains(term));} if(f.CategoryId.HasValue)q=q.Where(x=>x.CategoryId==f.CategoryId); if(f.Difficulty.HasValue)q=q.Where(x=>x.Difficulty==f.Difficulty); if(f.MinPoints.HasValue)q=q.Where(x=>x.Points>=f.MinPoints); if(f.MaxPoints.HasValue)q=q.Where(x=>x.Points<=f.MaxPoints); return await q.OrderBy(x=>x.Category!.Name).ThenBy(x=>x.Title).ToListAsync(); }
    public Task<List<Question>> GetAllAsync()=>db.Questions.Include(x=>x.Category).OrderBy(x=>x.Title).AsNoTracking().ToListAsync();
    public Task<Question?> GetAsync(int id)=>db.Questions.Include(x=>x.Category).FirstOrDefaultAsync(x=>x.Id==id);
    public async Task AddAsync(Question entity){db.Add(entity);await db.SaveChangesAsync();}
    public Task SaveAsync()=>db.SaveChangesAsync();
    public async Task DeleteAsync(Question entity){db.Remove(entity);await db.SaveChangesAsync();}
}

public record SetFilter(string? Search,int? MinQuestions,bool? IsPublished);
public interface IChallengeSetRepository
{
    Task<List<ChallengeSet>> SearchAsync(SetFilter filter,bool includeDrafts); Task<ChallengeSet?> GetAsync(int id); Task AddAsync(ChallengeSet entity,IReadOnlyList<int> questionIds); Task UpdateAsync(ChallengeSet entity,IReadOnlyList<int> questionIds); Task DeleteAsync(ChallengeSet entity);
}
public class ChallengeSetRepository(ApplicationDbContext db) : IChallengeSetRepository
{
    public async Task<List<ChallengeSet>> SearchAsync(SetFilter f,bool includeDrafts){var q=db.ChallengeSets.Include(x=>x.ChallengeSetQuestions).ThenInclude(x=>x.Question).ThenInclude(x=>x!.Category).AsNoTracking().AsQueryable(); if(!includeDrafts)q=q.Where(x=>x.IsPublished); if(!string.IsNullOrWhiteSpace(f.Search)){var t=f.Search.Trim();q=q.Where(x=>x.Name.Contains(t)||(x.Description!=null&&x.Description.Contains(t)));} if(f.MinQuestions.HasValue)q=q.Where(x=>x.ChallengeSetQuestions.Count>=f.MinQuestions); if(f.IsPublished.HasValue)q=q.Where(x=>x.IsPublished==f.IsPublished); return await q.OrderBy(x=>x.Name).ToListAsync();}
    public Task<ChallengeSet?> GetAsync(int id)=>db.ChallengeSets.Include(x=>x.ChallengeSetQuestions.OrderBy(y=>y.Order)).ThenInclude(x=>x.Question).ThenInclude(x=>x!.Category).FirstOrDefaultAsync(x=>x.Id==id);
    public async Task AddAsync(ChallengeSet entity,IReadOnlyList<int> ids){entity.ChallengeSetQuestions=ids.Distinct().Select((id,i)=>new ChallengeSetQuestion{QuestionId=id,Order=i+1}).ToList();db.Add(entity);await db.SaveChangesAsync();}
    public async Task UpdateAsync(ChallengeSet entity, IReadOnlyList<int> ids)
    {
        var orderedIds = ids.Distinct().ToList();
        var existingByQuestionId = entity.ChallengeSetQuestions.ToDictionary(x => x.QuestionId);

        foreach (var link in entity.ChallengeSetQuestions.Where(x => !orderedIds.Contains(x.QuestionId)).ToList())
            db.ChallengeSetQuestions.Remove(link);

        for (var i = 0; i < orderedIds.Count; i++)
        {
            var questionId = orderedIds[i];
            if (existingByQuestionId.TryGetValue(questionId, out var existing))
                existing.Order = i + 1;
            else
                entity.ChallengeSetQuestions.Add(new ChallengeSetQuestion { ChallengeSetId = entity.Id, QuestionId = questionId, Order = i + 1 });
        }

        await db.SaveChangesAsync();
    }
    public async Task DeleteAsync(ChallengeSet entity){db.Remove(entity);await db.SaveChangesAsync();}
}

public interface IProgressRepository
{
    Task<bool> HasCorrectAsync(string userId,int questionId); Task AddSubmissionAsync(Submission submission); Task<List<Submission>> HistoryAsync(string userId); Task<int> CorrectCountAsync(string userId); Task<int> AttemptCountAsync(string userId); Task<List<UserAchievement>> AchievementsAsync(string userId); Task AwardEligibleAsync(ApplicationUser user,int correctCount);
}
public class ProgressRepository(ApplicationDbContext db) : IProgressRepository
{
    public Task<bool> HasCorrectAsync(string uid,int qid)=>db.Submissions.AnyAsync(x=>x.UserId==uid&&x.QuestionId==qid&&x.IsCorrect);
    public async Task AddSubmissionAsync(Submission s){db.Add(s);await db.SaveChangesAsync();}
    public Task<List<Submission>> HistoryAsync(string uid)=>db.Submissions.Include(x=>x.Question).ThenInclude(x=>x!.Category).Where(x=>x.UserId==uid).OrderByDescending(x=>x.SubmittedAtUtc).AsNoTracking().ToListAsync();
    public Task<int> CorrectCountAsync(string uid)=>db.Submissions.CountAsync(x=>x.UserId==uid&&x.IsCorrect);
    public Task<int> AttemptCountAsync(string uid)=>db.Submissions.CountAsync(x=>x.UserId==uid);
    public Task<List<UserAchievement>> AchievementsAsync(string uid)=>db.UserAchievements.Include(x=>x.Achievement).Where(x=>x.UserId==uid).OrderByDescending(x=>x.EarnedAtUtc).AsNoTracking().ToListAsync();
    public async Task AwardEligibleAsync(ApplicationUser user,int correctCount){var earned=await db.UserAchievements.Where(x=>x.UserId==user.Id).Select(x=>x.AchievementId).ToListAsync();var eligible=await db.Achievements.Where(x=>!earned.Contains(x.Id)&&(x.RequiredCorrectAnswers==0||correctCount>=x.RequiredCorrectAnswers)&&(x.RequiredStreak==0||user.CurrentStreak>=x.RequiredStreak)).ToListAsync();foreach(var a in eligible)db.UserAchievements.Add(new UserAchievement{UserId=user.Id,AchievementId=a.Id});if(eligible.Count>0)await db.SaveChangesAsync();}
}
