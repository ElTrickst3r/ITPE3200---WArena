using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using WArena.Mvc.Data;
using WArena.Mvc.Models;
using WArena.Mvc.Repositories;
using Xunit;

namespace WArena.Mvc.Tests;

public class ChallengeSetOrderTests
{
    [Theory]
    [InlineData(new int[] { 1, 2, 3 }, new int[] { 3, 1, 2 })]
    [InlineData(new int[] { 4, 1, 5, 3, 2 }, new int[] { 3, 1, 2, 4, 5 })]
    [InlineData(new int[] { 2, 3 }, new int[] { 3, 2 })]
    [InlineData(new int[] { 5, 2, 5, 4, 2 }, new int[] { 2, 5, 4 })]
    public async Task Positive_EditChallengeSet_PreservesQuestionOrder(
        int[] selectedQuestions, int[] expectedOrder)
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        using var context = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection).Options);
        await context.Database.EnsureCreatedAsync();
        var category = new Category { Name = "Test" };
        var questions = Enumerable.Range(1, 5).Select(i => new Question
        {
            Category = category, Title = $"Question {i}", Prompt = "Prompt", CorrectAnswer = "Answer"
        }).ToArray();
        context.Questions.AddRange(questions);
        await context.SaveChangesAsync();
        int Id(int number) => questions[number - 1].Id;
        var repository = new ChallengeSetRepository(context);
        var set = new ChallengeSet { Name = "Practice", Description = "Original" };
        await repository.AddAsync(set, new[] { Id(3), Id(1), Id(2) });
        var otherSet = new ChallengeSet { Name = "Other" };
        await repository.AddAsync(otherSet, new[] { Id(1), Id(3) });
        var setId = set.Id;
        var otherSetId = otherSet.Id;
        context.ChangeTracker.Clear();

        var editing = (await repository.GetByIdAsync(setId))!;
        editing.Description = "Updated";
        await repository.UpdateAsync(editing, selectedQuestions.Select(Id).ToArray());
        context.ChangeTracker.Clear();

        var saved = (await repository.GetByIdAsync(setId))!;
        var links = saved.ChallengeSetQuestions.OrderBy(x => x.Order).ToArray();
        Assert.Equal(expectedOrder.Select(Id), links.Select(x => x.QuestionId));
        Assert.Equal(Enumerable.Range(1, expectedOrder.Length), links.Select(x => x.Order));
        Assert.Equal("Updated", saved.Description);
        Assert.Equal(5, await context.Questions.CountAsync());
        var unchanged = (await repository.GetByIdAsync(otherSetId))!;
        Assert.Equal(new[] { Id(1), Id(3) }, unchanged.ChallengeSetQuestions.OrderBy(x => x.Order).Select(x => x.QuestionId));
    }
}
