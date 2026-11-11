using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using WArena.Mvc.Controllers;
using WArena.Mvc.Data;
using WArena.Mvc.Models;
using WArena.Mvc.Repositories;
using WArena.Mvc.Services;
using WArena.Mvc.ViewModels;
using Xunit;

namespace WArena.Mvc.Tests;

public class PlayResultTests : IAsyncLifetime
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private readonly ChallengeSet _set = new() { Name = "Practice", IsPublished = true };
    private ServiceProvider _provider = null!;
    private IServiceScope _scope = null!;
    private ApplicationDbContext _db = null!;
    private PlayController _controller = null!;
    private Question _question = null!;
    private Question _nextQuestion = null!;
    private string _userId = null!;
    private string _otherUserId = null!;

    [Theory]
    [InlineData(false, "correct")]
    [InlineData(true, "correct")]
    [InlineData(false, "wrong")]
    [InlineData(true, "wrong")]
    public async Task Positive_Submit_RedirectsToSavedResult(bool inSet, string answer)
    {
        var result = await _controller.Submit(Answer(answer, inSet));

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(PlayController.Result), redirect.ActionName);
        var submissionId = Assert.IsType<int>(redirect.RouteValues!["id"]);
        Assert.Equal(inSet ? _set.Id : (int?)null, redirect.RouteValues["setId"]);
        Assert.Equal(0, redirect.RouteValues["index"]);
        var saved = await _db.Submissions.SingleAsync();
        Assert.Equal(submissionId, saved.Id);
        Assert.Equal(answer, saved.Answer);
    }

    [Theory]
    [InlineData(false, "correct", 100)]
    [InlineData(true, "correct", 100)]
    [InlineData(false, "wrong", 0)]
    [InlineData(true, "wrong", 0)]
    public async Task Positive_ResultRefresh_DoesNotAddSubmissionOrXp(bool inSet, string answer, int expectedXp)
    {
        var redirect = Assert.IsType<RedirectToActionResult>(await _controller.Submit(Answer(answer, inSet)));
        var submissionId = (int)redirect.RouteValues!["id"]!;
        int? setId = inSet ? _set.Id : null;

        for (var refresh = 0; refresh < 2; refresh++)
        {
            var view = Assert.IsType<ViewResult>(await _controller.Result(submissionId, setId, 0));
            var result = Assert.IsType<AnswerResultViewModel>(view.Model);

            Assert.Equal("Result", view.ViewName);
            Assert.Equal(answer == "correct", result.IsCorrect);
            Assert.Equal(answer, result.SubmittedAnswer);
            Assert.Equal(expectedXp, result.PointsEarned);
            Assert.Equal(expectedXp, result.TotalXp);
            Assert.Equal(setId, result.ChallengeSetId);
            Assert.Equal(inSet ? _nextQuestion.Id : (int?)null, result.NextQuestionId);
            Assert.Equal(1, result.NextQuestionIndex);
            Assert.Equal(1, await _db.Submissions.CountAsync());
            Assert.Equal(expectedXp, await TotalXpAsync());
        }
    }

    [Theory]
    [InlineData(false, "correct", 20, 120)]
    [InlineData(true, "correct", 20, 120)]
    [InlineData(false, "wrong", 0, 0)]
    [InlineData(true, "wrong", 0, 0)]
    public async Task Positive_IntentionalRetry_UsesRepeatScoring(bool inSet, string answer, int retryXp, int totalXp)
    {
        await _controller.Submit(Answer(answer, inSet));

        var result = await _controller.Submit(Answer(answer, inSet));

        Assert.IsType<RedirectToActionResult>(result);
        var history = await _db.Submissions.OrderBy(submission => submission.Id).ToListAsync();
        Assert.Equal(2, history.Count);
        Assert.Equal(retryXp, history[1].PointsEarned);
        Assert.Equal(totalXp, await TotalXpAsync());
    }

    [Fact]
    public async Task Negative_MissingResult_ReturnsNotFound()
    {
        await _controller.Submit(Answer("correct", false));

        var result = await _controller.Result(int.MaxValue);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Negative_OtherUsersResult_ReturnsNotFound()
    {
        var redirect = Assert.IsType<RedirectToActionResult>(await _controller.Submit(Answer("correct", false)));
        var submissionId = (int)redirect.RouteValues!["id"]!;
        _controller.HttpContext.User = Principal(_otherUserId);

        var result = await _controller.Result(submissionId);

        Assert.IsType<NotFoundResult>(result);
    }

    private PlayQuestionViewModel Answer(string answer, bool inSet) => new()
    {
        QuestionId = _question.Id,
        ChallengeSetId = inSet ? _set.Id : null,
        QuestionIndex = 0,
        Answer = answer
    };

    private Task<int> TotalXpAsync() =>
        _db.Users.Where(user => user.Id == _userId).Select(user => user.TotalXp).SingleAsync();

    private static ClaimsPrincipal Principal(string userId) =>
        new(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, userId) }, "Test"));

    public async Task InitializeAsync()
    {
        await _connection.OpenAsync();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddControllersWithViews();
        services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(_connection));
        services.AddIdentityCore<ApplicationUser>().AddEntityFrameworkStores<ApplicationDbContext>();
        _provider = services.BuildServiceProvider();
        _scope = _provider.CreateScope();
        _db = _scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await _db.Database.EnsureCreatedAsync();
        var manager = _scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = new ApplicationUser { UserName = "student" };
        var otherUser = new ApplicationUser { UserName = "other" };
        Assert.True((await manager.CreateAsync(user)).Succeeded);
        Assert.True((await manager.CreateAsync(otherUser)).Succeeded);
        _userId = user.Id;
        _otherUserId = otherUser.Id;
        var category = new Category { Name = "HTML" };
        _question = new Question
        {
            Category = category, Title = "First", Prompt = "Answer", CorrectAnswer = "correct", Points = 100
        };
        _nextQuestion = new Question
        {
            Category = category, Title = "Next", Prompt = "Answer", CorrectAnswer = "correct"
        };
        _db.Questions.AddRange(_question, _nextQuestion);
        await _db.SaveChangesAsync();
        var sets = new ChallengeSetRepository(_db);
        await sets.AddAsync(_set, new[] { _question.Id, _nextQuestion.Id });
        var http = new DefaultHttpContext
        {
            RequestServices = _scope.ServiceProvider,
            User = Principal(_userId)
        };
        _controller = new PlayController(new QuestionRepository(_db), sets, new SubmissionRepository(_db),
            new AchievementRepository(_db), new GamificationService(), manager, NullLogger<PlayController>.Instance)
        {
            ControllerContext = new ControllerContext(new ActionContext(http, new RouteData(), new ControllerActionDescriptor()))
        };
    }

    public Task DisposeAsync()
    {
        _scope?.Dispose();
        _provider?.Dispose();
        _connection.Dispose();
        return Task.CompletedTask;
    }
}
