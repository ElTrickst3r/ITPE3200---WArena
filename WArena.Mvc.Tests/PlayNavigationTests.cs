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

public class PlayNavigationTests : IAsyncLifetime
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private readonly ChallengeSet _set = new() { Name = "Practice", IsPublished = true };
    private ServiceProvider _provider = null!;
    private IServiceScope _scope = null!;
    private ApplicationDbContext _db = null!;
    private PlayController _controller = null!;
    private ChallengeSetRepository _sets = null!;
    private Question[] _questions = null!;

    [Theory]
    [InlineData(-1)]
    [InlineData(999)]
    public async Task Positive_InvalidIndex_UsesQuestionPosition(int index)
    {
        var view = Assert.IsType<ViewResult>(await _controller.Question(_questions[1].Id, _set.Id, index));
        var question = Assert.IsType<PlayQuestionViewModel>(view.Model);
        Assert.Equal(1, question.QuestionIndex);
        Assert.Equal(3, question.TotalQuestions);

        var redirect = Assert.IsType<RedirectToActionResult>(await _controller.Submit(Answer(index)));
        Assert.Equal(1, redirect.RouteValues!["index"]);
        var submissionId = (int)redirect.RouteValues["id"]!;
        var resultView = Assert.IsType<ViewResult>(await _controller.Result(submissionId, _set.Id, index));
        var result = Assert.IsType<AnswerResultViewModel>(resultView.Model);

        Assert.Equal(_questions[2].Id, result.NextQuestionId);
        Assert.Equal(2, result.NextQuestionIndex);
        Assert.Equal(100, result.PointsEarned);
        Assert.Equal(1, await _db.Submissions.CountAsync());
    }

    [Fact]
    public async Task Positive_RemovedEarlierQuestion_UpdatesResultNavigation()
    {
        var redirect = Assert.IsType<RedirectToActionResult>(await _controller.Submit(Answer(1)));
        await RemoveEarlierQuestionAsync();

        var view = Assert.IsType<ViewResult>(await _controller.Result((int)redirect.RouteValues!["id"]!, _set.Id, 1));
        var result = Assert.IsType<AnswerResultViewModel>(view.Model);

        Assert.Equal(_questions[2].Id, result.NextQuestionId);
        Assert.Equal(1, result.NextQuestionIndex);
    }

    [Fact]
    public async Task Positive_RetryAfterSetChange_UsesCurrentPositionAndReducedXp()
    {
        await _controller.Submit(Answer(1));
        await RemoveEarlierQuestionAsync();

        var redirect = Assert.IsType<RedirectToActionResult>(await _controller.Submit(Answer(1)));

        Assert.Equal(0, redirect.RouteValues!["index"]);
        Assert.Equal(120, await _db.Users.Select(user => user.TotalXp).SingleAsync());
        Assert.Equal(2, await _db.Submissions.CountAsync());
    }

    [Fact]
    public async Task Negative_InvalidAnswer_RedisplaysCurrentQuestionPosition()
    {
        await _controller.Submit(Answer(1));
        await RemoveEarlierQuestionAsync();
        await _controller.Submit(Answer(1));
        _controller.ModelState.AddModelError(nameof(PlayQuestionViewModel.Answer), "Enter an answer.");
        _controller.ModelState.SetModelValue(nameof(PlayQuestionViewModel.QuestionIndex), "999", "999");

        var view = Assert.IsType<ViewResult>(await _controller.Submit(Answer(999)));
        var question = Assert.IsType<PlayQuestionViewModel>(view.Model);

        Assert.Equal(0, question.QuestionIndex);
        Assert.Equal(2, question.TotalQuestions);
        Assert.False(_controller.ModelState.ContainsKey(nameof(PlayQuestionViewModel.QuestionIndex)));
        Assert.Equal(2, await _db.Submissions.CountAsync());
    }

    [Fact]
    public async Task Negative_RemovedQuestion_RejectsNewAnswersAndPreservesResult()
    {
        var redirect = Assert.IsType<RedirectToActionResult>(await _controller.Submit(Answer(1)));
        await _sets.UpdateAsync(_set, new[] { _questions[0].Id, _questions[2].Id });

        await AssertSetUnavailableAsync((int)redirect.RouteValues!["id"]!);
    }

    [Fact]
    public async Task Negative_DeletedSet_RejectsNewAnswersAndPreservesResult()
    {
        var redirect = Assert.IsType<RedirectToActionResult>(await _controller.Submit(Answer(1)));
        await _sets.DeleteAsync(_set);

        await AssertSetUnavailableAsync((int)redirect.RouteValues!["id"]!);
    }

    [Fact]
    public async Task Negative_UnpublishedSet_RejectsNewAnswersAndPreservesResult()
    {
        var redirect = Assert.IsType<RedirectToActionResult>(await _controller.Submit(Answer(1)));
        _set.IsPublished = false;
        await _db.SaveChangesAsync();

        await AssertSetUnavailableAsync((int)redirect.RouteValues!["id"]!);
    }

    private async Task AssertSetUnavailableAsync(int submissionId)
    {
        Assert.IsType<NotFoundResult>(await _controller.Question(_questions[1].Id, _set.Id, 1));
        Assert.IsType<NotFoundResult>(await _controller.Submit(Answer(1)));
        var view = Assert.IsType<ViewResult>(await _controller.Result(submissionId, _set.Id, -2));
        var result = Assert.IsType<AnswerResultViewModel>(view.Model);
        Assert.Null(result.NextQuestionId);
        Assert.Null(result.ChallengeSetId);
        Assert.Equal(100, result.PointsEarned);
        Assert.Equal(100, result.TotalXp);
        Assert.Equal(1, await _db.Submissions.CountAsync());
    }

    private Task RemoveEarlierQuestionAsync() =>
        _sets.UpdateAsync(_set, new[] { _questions[1].Id, _questions[2].Id });

    private PlayQuestionViewModel Answer(int index) => new()
    {
        QuestionId = _questions[1].Id,
        ChallengeSetId = _set.Id,
        QuestionIndex = index,
        Answer = "correct"
    };

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
        Assert.True((await manager.CreateAsync(user)).Succeeded);
        var category = new Category { Name = "HTML" };
        _questions = Enumerable.Range(1, 3).Select(number => new Question
        {
            Category = category, Title = $"Question {number}", Prompt = "Answer", CorrectAnswer = "correct", Points = 100
        }).ToArray();
        _db.Questions.AddRange(_questions);
        await _db.SaveChangesAsync();
        _sets = new ChallengeSetRepository(_db);
        await _sets.AddAsync(_set, _questions.Select(question => question.Id).ToArray());
        var http = new DefaultHttpContext
        {
            RequestServices = _scope.ServiceProvider,
            User = new ClaimsPrincipal(new ClaimsIdentity(
                new[] { new Claim(ClaimTypes.NameIdentifier, user.Id) }, "Test"))
        };
        _controller = new PlayController(new QuestionRepository(_db), _sets, new SubmissionRepository(_db),
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
