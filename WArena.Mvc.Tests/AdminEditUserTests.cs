using System.Security.Claims;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using WArena.Mvc.Controllers;
using WArena.Mvc.Data;
using WArena.Mvc.Models;
using WArena.Mvc.ViewModels;
using Xunit;

namespace WArena.Mvc.Tests;

public class AdminEditUserTests : IAsyncLifetime
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private ServiceProvider _provider = null!;
    private IServiceScope _scope = null!;
    private ApplicationDbContext _db = null!;
    private UserManager<ApplicationUser> _manager = null!;
    private AdminController _controller = null!;
    private ApplicationUser _user = null!;

    [Fact]
    public async Task Negative_InvalidPassword_DoesNotSaveAccountChanges()
    {
        var originalHash = _user.PasswordHash;
        var originalStamp = _user.SecurityStamp;

        var result = await _controller.EditUser(EditModel("abcdefgh"));
        var saved = await ReloadUserAsync();

        Assert.IsType<ViewResult>(result);
        Assert.Equal("original", saved.UserName);
        Assert.Equal("original", saved.DisplayName);
        Assert.Equal(new[] { "Student" }, await _manager.GetRolesAsync(saved));
        Assert.Equal(originalHash, saved.PasswordHash);
        Assert.Equal(originalStamp, saved.SecurityStamp);
        Assert.Equal(150, saved.TotalXp);
        Assert.Contains(_controller.ModelState[nameof(AdminEditUserViewModel.NewPassword)]!.Errors,
            error => error.ErrorMessage == _manager.ErrorDescriber.PasswordRequiresDigit().Description);
    }

    [Fact]
    public async Task Positive_ValidPassword_SavesAccountChanges()
    {
        var result = await _controller.EditUser(EditModel("Updated123!"));
        var saved = await ReloadUserAsync();

        Assert.Equal("Users", Assert.IsType<RedirectToActionResult>(result).ActionName);
        Assert.Equal("updated", saved.UserName);
        Assert.Equal("updated", saved.DisplayName);
        Assert.Equal(new[] { "Teacher" }, await _manager.GetRolesAsync(saved));
        Assert.True(await _manager.CheckPasswordAsync(saved, "Updated123!"));
        Assert.Equal(150, saved.TotalXp);
    }

    [Fact]
    public async Task Positive_NoNewPassword_SavesChangesAndKeepsPassword()
    {
        var result = await _controller.EditUser(EditModel(null));
        var saved = await ReloadUserAsync();

        Assert.Equal("Users", Assert.IsType<RedirectToActionResult>(result).ActionName);
        Assert.Equal("updated", saved.UserName);
        Assert.Equal("updated", saved.DisplayName);
        Assert.Equal(new[] { "Teacher" }, await _manager.GetRolesAsync(saved));
        Assert.True(await _manager.CheckPasswordAsync(saved, "Original123!"));
        Assert.Equal(150, saved.TotalXp);
    }

    [Fact]
    public async Task Positive_Startup_PreservesExistingSeedUserAndCreatesMissingUsers()
    {
        Assert.True((await _manager.SetEmailAsync(_user, "teacher@warena.local")).Succeeded);
        _user.DisplayName = "Saved display name";
        _user.CurrentStreak = 3;
        _user.LastActivityUtc = new DateTime(2026, 10, 4, 12, 0, 0, DateTimeKind.Utc);
        Assert.True((await _manager.UpdateAsync(_user)).Succeeded);
        var originalHash = _user.PasswordHash;
        var originalStamp = _user.SecurityStamp;
        var lastActivity = _user.LastActivityUtc;
        var submission = new Submission
        {
            UserId = _user.Id,
            Question = new Question
            {
                Category = new Category { Name = "Regression" },
                Title = "Saved question",
                Prompt = "Saved prompt",
                CorrectAnswer = "Saved answer"
            },
            Answer = "Saved answer",
            IsCorrect = true,
            PointsEarned = 30
        };
        _db.Submissions.Add(submission);
        await _db.SaveChangesAsync();

        for (var startup = 0; startup < 2; startup++)
        {
            using var scope = _provider.CreateScope();
            await DbInitializer.InitializeAsync(scope.ServiceProvider);
        }

        var saved = await ReloadUserAsync();
        Assert.Equal("original", saved.UserName);
        Assert.Equal("Saved display name", saved.DisplayName);
        Assert.Equal(new[] { "Student" }, await _manager.GetRolesAsync(saved));
        Assert.Equal(originalHash, saved.PasswordHash);
        Assert.Equal(originalStamp, saved.SecurityStamp);
        Assert.Equal(150, saved.TotalXp);
        Assert.Equal(3, saved.CurrentStreak);
        Assert.Equal(lastActivity, saved.LastActivityUtc);
        var savedSubmission = await _db.Submissions.SingleAsync();
        Assert.Equal(submission.Id, savedSubmission.Id);
        Assert.Equal(_user.Id, savedSubmission.UserId);
        Assert.Equal("Saved answer", savedSubmission.Answer);
        Assert.True(savedSubmission.IsCorrect);
        Assert.Equal(30, savedSubmission.PointsEarned);

        foreach (var username in new[] { "admin", "student", "student1", "student2", "student3", "student4", "student5" })
        {
            var created = await _manager.FindByEmailAsync($"{username}@warena.local");
            Assert.NotNull(created);
            Assert.Equal(username, created.UserName);
            Assert.Equal(username, created.DisplayName);
            Assert.Equal(new[] { username == "admin" ? "Admin" : "Student" }, await _manager.GetRolesAsync(created));
            Assert.True(await _manager.CheckPasswordAsync(created, username == "admin" ? "Admin123!" : "Student123!"));
        }
        Assert.Equal(8, await _db.Users.CountAsync());
    }

    private AdminEditUserViewModel EditModel(string? password) => new()
    {
        Id = _user.Id,
        Email = _user.Email!,
        Username = "updated",
        Role = "Teacher",
        NewPassword = password
    };

    private async Task<ApplicationUser> ReloadUserAsync()
    {
        // Identity's tracked user may still contain changes rolled back in the database.
        _db.ChangeTracker.Clear();
        return (await _manager.FindByIdAsync(_user.Id))!;
    }

    public async Task InitializeAsync()
    {
        await _connection.OpenAsync();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDataProtection().UseEphemeralDataProtectionProvider();
        services.AddControllersWithViews();
        services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(_connection));
        services.AddIdentity<ApplicationUser, IdentityRole>(options =>
        {
            options.Password.RequiredLength = 8;
            options.Password.RequireDigit = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.User.RequireUniqueEmail = true;
        }).AddEntityFrameworkStores<ApplicationDbContext>().AddDefaultTokenProviders();
        _provider = services.BuildServiceProvider();
        _scope = _provider.CreateScope();
        _db = _scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await _db.Database.EnsureCreatedAsync();
        _manager = _scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roles = _scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        Assert.True((await roles.CreateAsync(new IdentityRole("Student"))).Succeeded);
        Assert.True((await roles.CreateAsync(new IdentityRole("Teacher"))).Succeeded);
        _user = new ApplicationUser
        {
            UserName = "original",
            DisplayName = "original",
            Email = "user@example.com",
            TotalXp = 150
        };
        Assert.True((await _manager.CreateAsync(_user, "Original123!")).Succeeded);
        Assert.True((await _manager.AddToRoleAsync(_user, "Student")).Succeeded);
        var http = new DefaultHttpContext
        {
            RequestServices = _scope.ServiceProvider,
            User = new ClaimsPrincipal(new ClaimsIdentity(
                new[] { new Claim(ClaimTypes.NameIdentifier, "admin-id") }, "Test"))
        };
        _controller = new AdminController(_manager, _db, NullLogger<AdminController>.Instance)
        {
            ControllerContext = new ControllerContext(new ActionContext(http, new RouteData(), new ControllerActionDescriptor())),
            TempData = new TempDataDictionary(http, _scope.ServiceProvider.GetRequiredService<ITempDataProvider>())
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
