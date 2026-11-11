using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using WArena.Mvc.Controllers;
using WArena.Mvc.Data;
using WArena.Mvc.Models;
using WArena.Mvc.Repositories;
using WArena.Mvc.ViewModels;
using Xunit;

namespace WArena.Mvc.Tests;

public class ChallengeSetProgressTests : IAsyncLifetime
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private readonly ApplicationUser _user = new() { UserName = "student", TotalXp = 250 };
    private readonly ChallengeSet _completedSet = new() { Name = "Completed", IsPublished = true };
    private readonly ChallengeSet _partialSet = new() { Name = "Partial", IsPublished = true };
    private readonly ChallengeSet _unansweredSet = new() { Name = "Unanswered", IsPublished = true };
    private readonly ChallengeSet _emptySet = new() { Name = "Empty", IsPublished = true };
    private readonly Question _sharedQuestion = new() { Title = "Shared question" };
    private readonly Question _secondCorrectQuestion = new() { Title = "Second correct question" };
    private readonly Question _incorrectQuestion = new() { Title = "Incorrect-only question" };
    private readonly Question _outsideQuestion = new() { Title = "Question outside the sets" };
    private readonly Question _unansweredQuestion = new() { Title = "Unanswered question" };
    private ApplicationDbContext _db = null!;
    private ChallengeSetsController _controller = null!;
    private int _historyCount;
    private int _completionQueries;

    [Fact]
    public async Task Positive_Overview_ShowsCompletedPartialAndUnansweredSets()
    {
        var view = Assert.IsType<ViewResult>(await _controller.Index(null, null, null));
        var model = Assert.IsType<ChallengeSetSearchViewModel>(view.Model);

        Assert.Equal(1, _completionQueries);
        Assert.Equal(4, model.ChallengeSets.Count);
        Assert.Equal(2, model.CompletedQuestionIds.Count);
        var completed = model.ChallengeSets.Single(set => set.Id == _completedSet.Id);
        var partial = model.ChallengeSets.Single(set => set.Id == _partialSet.Id);
        var unanswered = model.ChallengeSets.Single(set => set.Id == _unansweredSet.Id);
        Assert.Equal(2, completed.ChallengeSetQuestions.Count(link => model.CompletedQuestionIds.Contains(link.QuestionId)));
        Assert.Equal(1, partial.ChallengeSetQuestions.Count(link => model.CompletedQuestionIds.Contains(link.QuestionId)));
        Assert.Equal(0, unanswered.ChallengeSetQuestions.Count(link => model.CompletedQuestionIds.Contains(link.QuestionId)));
        Assert.Empty(model.ChallengeSets.Single(set => set.Id == _emptySet.Id).ChallengeSetQuestions);
        await AssertProgressUnchangedAsync();
    }

    [Fact]
    public async Task Positive_CompletedSet_CountsRepeatedAnswersOnceAndKeepsQuestionOrder()
    {
        var view = Assert.IsType<ViewResult>(await _controller.Details(_completedSet.Id));
        var model = Assert.IsType<ChallengeSetDetailsViewModel>(view.Model);

        Assert.Equal(1, _completionQueries);
        Assert.Equal(2, model.TotalQuestions);
        Assert.Equal(2, model.CompletedQuestions);
        Assert.Equal(2, model.CompletedQuestionIds.Count);
        Assert.Contains(_sharedQuestion.Id, model.CompletedQuestionIds);
        Assert.Contains(_secondCorrectQuestion.Id, model.CompletedQuestionIds);
        Assert.Equal(new[] { _secondCorrectQuestion.Id, _sharedQuestion.Id },
            model.ChallengeSet.ChallengeSetQuestions.OrderBy(link => link.Order).Select(link => link.QuestionId));
        await AssertProgressUnchangedAsync();
    }

    [Fact]
    public async Task Positive_PartialSet_CountsOnlyTheUsersCorrectAnswersInThatSet()
    {
        var view = Assert.IsType<ViewResult>(await _controller.Details(_partialSet.Id));
        var model = Assert.IsType<ChallengeSetDetailsViewModel>(view.Model);

        Assert.Equal(1, _completionQueries);
        Assert.Equal(3, model.TotalQuestions);
        Assert.Equal(1, model.CompletedQuestions);
        Assert.Equal(_sharedQuestion.Id, Assert.Single(model.CompletedQuestionIds));
        Assert.DoesNotContain(_incorrectQuestion.Id, model.CompletedQuestionIds);
        Assert.DoesNotContain(_unansweredQuestion.Id, model.CompletedQuestionIds);
        Assert.DoesNotContain(_secondCorrectQuestion.Id, model.CompletedQuestionIds);
        Assert.DoesNotContain(_outsideQuestion.Id, model.CompletedQuestionIds);
        await AssertProgressUnchangedAsync();
    }

    [Fact]
    public async Task Negative_OtherUsersAnswers_DoNotCompleteAnUnansweredSet()
    {
        var view = Assert.IsType<ViewResult>(await _controller.Details(_unansweredSet.Id));
        var model = Assert.IsType<ChallengeSetDetailsViewModel>(view.Model);

        Assert.Equal(1, _completionQueries);
        Assert.Equal(1, model.TotalQuestions);
        Assert.Equal(0, model.CompletedQuestions);
        Assert.Empty(model.CompletedQuestionIds);
        await AssertProgressUnchangedAsync();
    }

    [Fact]
    public async Task Positive_EmptySet_HasNoProgressAndNeedsNoCompletionQuery()
    {
        var view = Assert.IsType<ViewResult>(await _controller.Details(_emptySet.Id));
        var model = Assert.IsType<ChallengeSetDetailsViewModel>(view.Model);

        Assert.Equal(0, _completionQueries);
        Assert.Equal(0, model.TotalQuestions);
        Assert.Equal(0, model.CompletedQuestions);
        Assert.Empty(model.CompletedQuestionIds);
        await AssertProgressUnchangedAsync();
    }

    [Fact]
    public async Task Negative_AnonymousUser_HasNoCompletionData()
    {
        _controller.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());

        // Direct calls bypass Authorize; this checks the action's defensive handling.
        var view = Assert.IsType<ViewResult>(await _controller.Index(null, null, null));
        var model = Assert.IsType<ChallengeSetSearchViewModel>(view.Model);

        Assert.Equal(0, _completionQueries);
        Assert.Equal(4, model.ChallengeSets.Count);
        Assert.Empty(model.CompletedQuestionIds);
        await AssertProgressUnchangedAsync();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Positive_FilteredEmptySet_NeedsNoCompletionQuery(bool authenticated)
    {
        if (!authenticated)
            _controller.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());

        var view = Assert.IsType<ViewResult>(await _controller.Index("Empty", null, true));
        var model = Assert.IsType<ChallengeSetSearchViewModel>(view.Model);

        Assert.Equal(_emptySet.Id, Assert.Single(model.ChallengeSets).Id);
        Assert.Empty(model.CompletedQuestionIds);
        Assert.Equal(0, _completionQueries);
        await AssertProgressUnchangedAsync();
    }

    private async Task AssertProgressUnchangedAsync()
    {
        Assert.Equal(_historyCount, await _db.Submissions.CountAsync());
        Assert.Equal(250, await _db.Users.Where(user => user.Id == _user.Id).Select(user => user.TotalXp).SingleAsync());
    }

    private static Submission AnswerFor(ApplicationUser user, Question question, bool correct) => new()
    {
        UserId = user.Id,
        QuestionId = question.Id,
        Answer = correct ? "correct" : "wrong",
        IsCorrect = correct
    };

    public async Task InitializeAsync()
    {
        await _connection.OpenAsync();
        _db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .LogTo(message =>
            {
                // Count executed SQL commands involving submissions, without a custom interceptor.
                if (message.Contains("Submissions", StringComparison.Ordinal)) _completionQueries++;
            }, new[] { RelationalEventId.CommandExecuted })
            .Options);
        await _db.Database.EnsureCreatedAsync();

        var otherUser = new ApplicationUser { UserName = "other" };
        var category = new Category { Name = "HTML" };
        _db.Users.AddRange(_user, otherUser);
        var questions = new[] { _sharedQuestion, _secondCorrectQuestion, _incorrectQuestion, _outsideQuestion, _unansweredQuestion };
        foreach (var question in questions)
        {
            question.Category = category;
            question.Prompt = "Answer";
            question.CorrectAnswer = "correct";
        }
        _db.Questions.AddRange(questions);
        await _db.SaveChangesAsync();

        var sets = new ChallengeSetRepository(_db);
        await sets.AddAsync(_completedSet, new[] { _secondCorrectQuestion.Id, _sharedQuestion.Id });
        await sets.AddAsync(_partialSet, new[] { _sharedQuestion.Id, _incorrectQuestion.Id, _unansweredQuestion.Id });
        await sets.AddAsync(_unansweredSet, new[] { _unansweredQuestion.Id });
        await sets.AddAsync(_emptySet, Array.Empty<int>());

        _db.Submissions.AddRange(
            AnswerFor(_user, _sharedQuestion, true),
            AnswerFor(_user, _sharedQuestion, true),
            AnswerFor(_user, _secondCorrectQuestion, true),
            AnswerFor(_user, _incorrectQuestion, false),
            AnswerFor(_user, _outsideQuestion, true),
            AnswerFor(otherUser, _incorrectQuestion, true),
            AnswerFor(otherUser, _unansweredQuestion, true));
        await _db.SaveChangesAsync();
        _historyCount = await _db.Submissions.CountAsync();

        var http = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                new[] { new Claim(ClaimTypes.NameIdentifier, _user.Id) }, "Test"))
        };
        _controller = new ChallengeSetsController(sets, new QuestionRepository(_db), new SubmissionRepository(_db),
            NullLogger<ChallengeSetsController>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = http }
        };
        _completionQueries = 0;
    }

    public Task DisposeAsync()
    {
        _db?.Dispose();
        _connection.Dispose();
        return Task.CompletedTask;
    }
}
