using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using WArena.Mvc.Controllers;
using WArena.Mvc.Data;
using WArena.Mvc.Models;
using WArena.Mvc.Repositories;
using WArena.Mvc.ViewModels;
using Xunit;

namespace WArena.Mvc.Tests;

public class CategoryNameTests : IAsyncLifetime
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private ApplicationDbContext _db = null!;
    private CategoryRepository _repository = null!;

    [Theory]
    [InlineData("HTML", " html ")]
    [InlineData(" HTML ", "html")]
    [InlineData("\tHTML\n", "\nHtml\t")]
    [InlineData("ÆØÅ", " æøå ")]
    public async Task Negative_DuplicateName_RejectsCategoryCreation(string storedName, string submittedName)
    {
        var original = new Category { Name = storedName };
        var other = new Category { Name = "Other" };
        _db.Categories.AddRange(original, other);
        await _db.SaveChangesAsync();
        var controller = CreateController();

        var result = await controller.Create(new CategoryFormViewModel { Name = submittedName });

        Assert.IsType<ViewResult>(result);
        Assert.True(await _repository.NameExistsAsync(submittedName));
        Assert.Contains(controller.ModelState[nameof(CategoryFormViewModel.Name)]!.Errors,
            error => error.ErrorMessage == "A category with this name already exists.");
        _db.ChangeTracker.Clear();
        Assert.Equal(2, await _db.Categories.CountAsync());
        Assert.Equal(storedName, (await _db.Categories.FindAsync(original.Id))!.Name);
        Assert.Equal("Other", (await _db.Categories.FindAsync(other.Id))!.Name);
    }

    [Theory]
    [InlineData("HTML", " html ")]
    [InlineData(" HTML ", "html")]
    [InlineData("\tHTML\n", "\nHtml\t")]
    [InlineData("ÆØÅ", " æøå ")]
    public async Task Negative_DuplicateName_RejectsCategoryEdit(string storedName, string submittedName)
    {
        var original = new Category { Name = storedName };
        var other = new Category { Name = "Other" };
        _db.Categories.AddRange(original, other);
        await _db.SaveChangesAsync();
        var controller = CreateController();
        var model = new CategoryFormViewModel { Id = other.Id, Name = submittedName };

        var result = await controller.Edit(other.Id, model);

        Assert.IsType<ViewResult>(result);
        Assert.Contains(controller.ModelState[nameof(CategoryFormViewModel.Name)]!.Errors,
            error => error.ErrorMessage == "A category with this name already exists.");
        _db.ChangeTracker.Clear();
        Assert.Equal(2, await _db.Categories.CountAsync());
        Assert.Equal(storedName, (await _db.Categories.FindAsync(original.Id))!.Name);
        Assert.Equal("Other", (await _db.Categories.FindAsync(other.Id))!.Name);
    }

    [Fact]
    public async Task Positive_CurrentCategory_IsExcludedFromDuplicateCheck()
    {
        var category = new Category { Name = "HTML" };
        _db.Categories.Add(category);
        await _db.SaveChangesAsync();

        Assert.True(await _repository.NameExistsAsync(" html "));
        Assert.False(await _repository.NameExistsAsync(" html ", category.Id));
    }

    [Fact]
    public async Task Positive_Startup_PreservesDuplicateCategoriesAndQuestionLinks()
    {
        var duplicateCategories = new[]
        {
            new Category { Name = "HTML", Description = "First HTML" },
            new Category { Name = "HTML", Description = "Second HTML" },
            new Category { Name = " html ", Description = "Spaced HTML" },
            new Category { Name = "Div1", Description = "First legacy category" },
            new Category { Name = " div1 ", Description = "Second legacy category" }
        };
        _db.Categories.AddRange(duplicateCategories);
        foreach (var name in new[] { "CSS", "ASP.NET Core", "JavaScript", "Web Development Concepts" })
            _db.Categories.Add(new Category { Name = name });
        await _db.SaveChangesAsync();
        var duplicateIds = duplicateCategories.Select(category => category.Id).ToArray();
        foreach (var category in duplicateCategories)
            _db.Questions.Add(new Question
            {
                CategoryId = category.Id, Title = "Existing", Prompt = "Keep", CorrectAnswer = "Keep"
            });
        await _db.SaveChangesAsync();
        var categoryCount = await _db.Categories.CountAsync();
        var originalCategories = await _db.Categories.Where(category => duplicateIds.Contains(category.Id))
            .OrderBy(category => category.Id).Select(category => new { category.Id, category.Name, category.Description }).ToListAsync();
        var originalLinks = await _db.Questions.OrderBy(question => question.Id)
            .Select(question => new { question.Id, question.CategoryId }).ToListAsync();

        // Running twice checks that startup does not keep adding the missing current seed.
        for (var run = 0; run < 2; run++)
        {
            await InitializeCategoriesAsync();
            _db.ChangeTracker.Clear();

            Assert.Equal(categoryCount + 1, await _db.Categories.CountAsync());
            Assert.Equal(1, await _db.Categories.CountAsync(category => category.Name == "Practical Web Development"));
            Assert.Equal(originalCategories, await _db.Categories.Where(category => duplicateIds.Contains(category.Id))
                .OrderBy(category => category.Id).Select(category => new { category.Id, category.Name, category.Description }).ToListAsync());
            Assert.Equal(originalLinks, await _db.Questions.OrderBy(question => question.Id)
                .Select(question => new { question.Id, question.CategoryId }).ToListAsync());
        }
    }

    private CategoriesController CreateController() =>
        new(_repository, NullLogger<CategoriesController>.Instance);

    private Task InitializeCategoriesAsync()
    {
        // Call only category initialization, without running unrelated user and question seeding.
        var method = typeof(DbInitializer).GetMethod("EnsureCategoriesAsync", BindingFlags.NonPublic | BindingFlags.Static)!;
        return (Task)method.Invoke(null, new object[] { _db })!;
    }

    public async Task InitializeAsync()
    {
        await _connection.OpenAsync();
        _db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection).Options);
        await _db.Database.EnsureCreatedAsync();
        _repository = new CategoryRepository(_db);
    }

    public Task DisposeAsync()
    {
        _db?.Dispose();
        _connection.Dispose();
        return Task.CompletedTask;
    }
}
