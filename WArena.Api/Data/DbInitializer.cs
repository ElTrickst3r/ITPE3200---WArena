using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WArena.Api.Models;

namespace WArena.Api.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

        await context.Database.EnsureCreatedAsync();

        foreach (var role in new[] { "Admin", "Teacher", "Student" })
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        await EnsureUserAsync(userManager, "admin@warena.local", "admin", "Admin123!", "Admin");
        await EnsureUserAsync(userManager, "teacher@warena.local", "teacher", "Teacher123!", "Teacher");
        await EnsureUserAsync(userManager, "student@warena.local", "student", "Student123!", "Student");

        for (var i = 1; i <= 5; i++)
            await EnsureUserAsync(userManager, $"student{i}@warena.local", $"student{i}", "Student123!", "Student");

        await RemoveOldSeedStudentsAsync(context, userManager);

        var categories = await EnsureCategoriesAsync(context);

        await EnsureSeedQuestionsAsync(context, categories["HTML"], HtmlQuestions());
        await EnsureSeedQuestionsAsync(context, categories["CSS"], CssQuestions());
        await EnsureSeedQuestionsAsync(context, categories["ASP.NET Core"], AspNetQuestions());
        await EnsureSeedQuestionsAsync(context, categories["JavaScript"], JavaScriptQuestions());
        await EnsureSeedQuestionsAsync(context, categories["Div1"], Div1Questions());
        await EnsureSeedQuestionsAsync(context, categories["Div2"], Div2Questions());

        await UpdateLegacyQuestionPromptAsync(
            context,
            categories["Div2"],
            "Web application",
            "According to the lecture material, what client application is normally used to access a web application?",
            "What client application is normally used to access a web application?");

        if (!await context.ChallengeSets.AnyAsync())
        {
            var htmlQuestions = await context.Questions.Where(x => x.Category!.Name == "HTML").OrderBy(x => x.Id).Select(x => x.Id).ToListAsync();
            var cssQuestions = await context.Questions.Where(x => x.Category!.Name == "CSS").OrderBy(x => x.Id).Select(x => x.Id).ToListAsync();
            var webQuestions = await context.Questions.Where(x => x.Category!.Name == "ASP.NET Core" || x.Category!.Name == "JavaScript").OrderBy(x => x.Id).Take(10).Select(x => x.Id).ToListAsync();

            AddSet(context, "HTML Basics", "Practice ten core HTML concepts.", htmlQuestions);
            AddSet(context, "CSS Fundamentals", "Practice ten essential CSS concepts.", cssQuestions);
            AddSet(context, "Web Applications Mix", "A mixed set for general course practice.", webQuestions);
            await context.SaveChangesAsync();
        }

        var div1QuestionIds = await context.Questions
            .Where(x => x.Category!.Name == "Div1")
            .OrderBy(x => x.Id)
            .Take(10)
            .Select(x => x.Id)
            .ToListAsync();
        var div2QuestionIds = await context.Questions
            .Where(x => x.Category!.Name == "Div2")
            .OrderBy(x => x.Id)
            .Take(10)
            .Select(x => x.Id)
            .ToListAsync();

        await EnsureChallengeSetAsync(context, "Div1", "Div1", div1QuestionIds);
        await EnsureChallengeSetAsync(context, "Div2", "Div2", div2QuestionIds);

        if (!await context.Achievements.AnyAsync())
        {
            context.Achievements.AddRange(
                new Achievement { Name = "First Steps", Description = "Answer your first question correctly.", Icon = "🎯", RequiredCorrectAnswers = 1 },
                new Achievement { Name = "Ten Correct", Description = "Reach 10 correct answers.", Icon = "🏆", RequiredCorrectAnswers = 10 },
                new Achievement { Name = "On Fire", Description = "Reach a 3-day learning streak.", Icon = "🔥", RequiredStreak = 3 },
                new Achievement { Name = "Streak Master", Description = "Reach a 7-day learning streak.", Icon = "⚡", RequiredStreak = 7 });
            await context.SaveChangesAsync();
        }
    }

    private static async Task<Dictionary<string, Category>> EnsureCategoriesAsync(ApplicationDbContext context)
    {
        var definitions = new[]
        {
            ("HTML", "Structure and semantic HTML."),
            ("CSS", "Styling, layout and responsive design."),
            ("ASP.NET Core", "MVC, APIs, routing and server-side concepts."),
            ("JavaScript", "Browser-side programming and interaction."),
            ("Div1", "Div1"),
            ("Div2", "Div2")
        };

        var existing = (await context.Categories.ToListAsync())
            .ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);

        foreach (var (name, description) in definitions)
        {
            if (existing.TryGetValue(name, out var category))
            {
                if (!string.Equals(category.Description, description, StringComparison.Ordinal))
                    category.Description = description;
                continue;
            }

            category = new Category { Name = name, Description = description };
            context.Categories.Add(category);
            existing[name] = category;
        }

        if (context.ChangeTracker.HasChanges()) await context.SaveChangesAsync();
        return existing;
    }

    private static async Task UpdateLegacyQuestionPromptAsync(
        ApplicationDbContext context,
        Category category,
        string title,
        string oldPrompt,
        string newPrompt)
    {
        var question = await context.Questions.FirstOrDefaultAsync(x =>
            x.CategoryId == category.Id &&
            x.Title == title &&
            x.Prompt == oldPrompt);

        if (question is null) return;

        question.Prompt = newPrompt;
        await context.SaveChangesAsync();
    }

    private static async Task EnsureSeedQuestionsAsync(ApplicationDbContext context, Category category, IReadOnlyList<SeedQuestion> seeds)
    {
        var existingQuestions = await context.Questions.Where(x => x.CategoryId == category.Id).ToListAsync();
        var existingTitles = new HashSet<string>(existingQuestions.Select(x => x.Title), StringComparer.OrdinalIgnoreCase);
        var count = existingQuestions.Count;

        foreach (var seed in seeds)
        {
            if (count >= 10) break;
            if (existingTitles.Contains(seed.Title)) continue;

            context.Questions.Add(new Question
            {
                Title = seed.Title,
                Prompt = seed.Prompt,
                CorrectAnswer = seed.CorrectAnswer,
                CategoryId = category.Id,
                Type = seed.Type,
                Difficulty = seed.Difficulty,
                Points = seed.Points,
                OptionA = seed.OptionA,
                OptionB = seed.OptionB,
                OptionC = seed.OptionC,
                OptionD = seed.OptionD
            });
            existingTitles.Add(seed.Title);
            count++;
        }

        if (context.ChangeTracker.HasChanges()) await context.SaveChangesAsync();
    }

    private static IReadOnlyList<SeedQuestion> HtmlQuestions() => new[]
    {
        Q("Largest heading", "Which HTML element represents the largest heading?", "h1", Difficulty.Easy, 100),
        MC("Hyperlink element", "Which HTML element creates a hyperlink?", "<a>", "<link>", "<a>", "<href>", "<url>", Difficulty.Easy, 100),
        Q("Semantic navigation", "Which semantic HTML element is intended for major navigation links?", "nav", Difficulty.Medium, 150),
        Q("Paragraph element", "Which HTML element is used for a paragraph of text?", "p", Difficulty.Easy, 100),
        Q("Image alternative text", "Which attribute provides alternative text for an HTML image?", "alt", Difficulty.Easy, 100),
        Q("List item", "Which HTML element represents one item inside an ordered or unordered list?", "li", Difficulty.Easy, 100),
        Q("Main content", "Which semantic HTML element represents the main content of a document?", "main", Difficulty.Medium, 150),
        Q("Page title", "Which HTML element inside <head> sets the text shown in the browser tab?", "title", Difficulty.Easy, 100),
        MC("Form label connection", "Which label attribute normally connects a <label> to an input's id?", "for", "name", "for", "target", "rel", Difficulty.Medium, 150),
        MC("Required input", "Which HTML attribute makes a form field mandatory before normal browser submission?", "required", "checked", "selected", "required", "readonly", Difficulty.Medium, 150)
    };

    private static IReadOnlyList<SeedQuestion> CssQuestions() => new[]
    {
        Q("Text colour", "Which CSS property changes text colour?", "color", Difficulty.Easy, 100),
        Q("Flex direction", "Which flex-direction value lays items out vertically from top to bottom?", "column", Difficulty.Medium, 150),
        Q("Class selector", "Which character starts a CSS class selector?", ".", Difficulty.Easy, 100),
        Q("Grid layout", "Which display value enables CSS Grid layout?", "grid", Difficulty.Easy, 100),
        MC("Inside spacing", "Which CSS property controls the space between an element's content and its border?", "padding", "margin", "padding", "gap", "outline", Difficulty.Easy, 100),
        Q("Responsive rule", "Which CSS at-rule is commonly used to apply styles based on viewport conditions?", "@media", Difficulty.Medium, 150),
        Q("Flex main-axis alignment", "Which flexbox property aligns items along the main axis?", "justify-content", Difficulty.Medium, 150),
        Q("Border-box sizing", "Which box-sizing value includes padding and border inside the declared width and height?", "border-box", Difficulty.Medium, 150),
        MC("Fixed positioning", "Which position value keeps an element positioned relative to the browser viewport?", "fixed", "relative", "absolute", "fixed", "static", Difficulty.Medium, 150),
        Q("Root-relative unit", "Which CSS unit is relative to the root element's font size?", "rem", Difficulty.Medium, 150)
    };

    private static IReadOnlyList<SeedQuestion> AspNetQuestions() => new[]
    {
        Q("MVC", "What does MVC stand for?", "Model View Controller", Difficulty.Easy, 100),
        MC("HTTP create", "Which HTTP method is commonly used to create a resource?", "POST", "GET", "POST", "DELETE", "HEAD", Difficulty.Easy, 100),
        Q("Async keyword", "Which C# keyword waits for a Task without blocking the current thread?", "await", Difficulty.Medium, 150),
        Q("Controller responsibility", "In MVC, which component normally handles incoming requests and chooses what response to return?", "Controller", Difficulty.Easy, 100),
        Q("ViewModel purpose", "What type of model is commonly used to pass page-specific data from an MVC controller to a view?", "ViewModel", Difficulty.Medium, 150),
        MC("Dependency injection", "Which ASP.NET Core feature supplies registered services to controller constructors?", "Dependency Injection", "Model binding", "Dependency Injection", "Routing", "Razor syntax", Difficulty.Medium, 150),
        Q("Application settings", "Which JSON file commonly stores ASP.NET Core configuration such as connection strings and logging settings?", "appsettings.json", Difficulty.Easy, 100),
        MC("MVC POST action", "Which attribute marks an MVC action as handling HTTP POST requests?", "[HttpPost]", "[HttpGet]", "[HttpPost]", "[Authorize]", "[Required]", Difficulty.Medium, 150),
        Q("Action result", "What common ASP.NET Core MVC return type can represent different HTTP responses from an action method?", "IActionResult", Difficulty.Medium, 150),
        Q("Routing", "What ASP.NET Core feature maps an incoming URL to a controller action or endpoint?", "Routing", Difficulty.Easy, 100)
    };

    private static IReadOnlyList<SeedQuestion> JavaScriptQuestions() => new[]
    {
        Q("Strict equality", "Which JavaScript operator checks strict equality?", "===", Difficulty.Easy, 100),
        Q("Array mapping", "Which array method creates a new array by transforming every element?", "map", Difficulty.Medium, 150),
        Q("Block-scoped constant", "Which JavaScript keyword declares a block-scoped variable that cannot be reassigned?", "const", Difficulty.Easy, 100),
        Q("Event listener", "Which DOM method is commonly used to attach an event handler to an element?", "addEventListener", Difficulty.Medium, 150),
        Q("Parse JSON", "Which JavaScript method converts a JSON string into a JavaScript value?", "JSON.parse", Difficulty.Medium, 150),
        Q("Async return", "What does an async JavaScript function always return?", "Promise", Difficulty.Medium, 150),
        Q("DOM selector", "Which DOM method returns the first element matching a CSS selector?", "querySelector", Difficulty.Medium, 150),
        Q("Array filtering", "Which array method creates a new array containing elements that pass a test?", "filter", Difficulty.Medium, 150),
        Q("Spread syntax", "Which JavaScript syntax uses three dots to expand iterable values or object properties?", "...", Difficulty.Medium, 150),
        Q("Prevent form reload", "Which event method prevents a form's default browser submission behavior?", "preventDefault", Difficulty.Medium, 150)
    };

    // Div1 questions are based on the practical DEMOS material supplied with the project.
    private static IReadOnlyList<SeedQuestion> Div1Questions() => new[]
    {
        MC("Run a .NET project", "Which command runs a .NET project from the terminal?", "dotnet run", "dotnet start", "dotnet run", "dotnet execute", "dotnet serve", Difficulty.Easy, 100),
        Q("Static files root", "Which ASP.NET Core MVC folder is the public root for CSS, JavaScript and other static files?", "wwwroot", Difficulty.Easy, 100),
        Q("Controller folder", "Which MVC folder normally contains controller classes that handle incoming HTTP requests?", "Controllers", Difficulty.Easy, 100),
        Q("Razor view folder", "Which MVC folder normally contains .cshtml files used to render the user interface?", "Views", Difficulty.Easy, 100),
        Q("Page-specific model", "Which optional MVC folder commonly stores models tailored specifically for passing data to and from a view?", "ViewModels", Difficulty.Medium, 150),
        Q("Register MVC services", "Which Program.cs method registers the services required for MVC controllers and views?", "AddControllersWithViews", Difficulty.Medium, 150),
        Q("Production exception handler", "Which middleware method can route unhandled production exceptions to a dedicated error page?", "UseExceptionHandler", Difficulty.Medium, 150),
        MC("Async EF list", "Which Entity Framework Core method asynchronously loads query results into a list?", "ToListAsync", "ToList", "ToListAsync", "SaveChangesAsync", "FindAsync", Difficulty.Medium, 150),
        Q("EF schema changes", "What Entity Framework Core feature is used to manage database schema changes over time?", "Migrations", Difficulty.Medium, 150),
        MC("Scoped service lifetime", "Which dependency-injection lifetime creates one service instance per HTTP request?", "Scoped", "Transient", "Scoped", "Singleton", "Static", Difficulty.Hard, 200)
    };

    // Div2 questions are based on the conceptual LECTURES material supplied with the project.
    private static IReadOnlyList<SeedQuestion> Div2Questions() => new[]
    {
        Q("Web application", "What client application is normally used to access a web application?", "Web browser", Difficulty.Easy, 100),
        Q("MVC separation", "Which design pattern separates an application into Model, View and Controller?", "MVC", Difficulty.Easy, 100),
        Q("Data access layer", "Which application layer is responsible for reading and writing data to the underlying data storage?", "Data Access Layer", Difficulty.Medium, 150),
        Q("Authentication", "What security process verifies who a user is?", "Authentication", Difficulty.Easy, 100),
        Q("Authorisation", "What security process determines what an authenticated user is allowed to do?", "Authorisation", Difficulty.Easy, 100),
        MC("Robust validation", "Which type of validation occurs after data reaches the server and cannot be trusted to JavaScript alone?", "Server-side validation", "Client-side validation", "Server-side validation", "CSS validation", "Browser caching", Difficulty.Medium, 150),
        Q("API response format", "What structured data format is commonly returned by Web APIs to a client-side application?", "JSON", Difficulty.Easy, 100),
        MC("Client-side rendering", "In a client-side rendered SPA, where is most of the user interface built after the initial page loads?", "In the browser", "In the database", "In the browser", "In the DNS server", "In the compiler", Difficulty.Medium, 150),
        Q("Object-relational mapping", "Which .NET technology taught in the course provides database abstraction and object-relational mapping?", "Entity Framework", Difficulty.Medium, 150),
        Q("Course SPA framework", "Which modern frontend library/framework is used as the main SPA example in the course material?", "React", Difficulty.Easy, 100)
    };

    private static SeedQuestion Q(string title, string prompt, string answer, Difficulty difficulty, int points) =>
        new(title, prompt, answer, QuestionType.ShortAnswer, difficulty, points, null, null, null, null);

    private static SeedQuestion MC(string title, string prompt, string answer, string a, string b, string c, string d, Difficulty difficulty, int points) =>
        new(title, prompt, answer, QuestionType.MultipleChoice, difficulty, points, a, b, c, d);

    private sealed record SeedQuestion(
        string Title,
        string Prompt,
        string CorrectAnswer,
        QuestionType Type,
        Difficulty Difficulty,
        int Points,
        string? OptionA,
        string? OptionB,
        string? OptionC,
        string? OptionD);

    private static void AddSet(ApplicationDbContext context, string name, string description, IReadOnlyList<int> questionIds)
    {
        var set = new ChallengeSet { Name = name, Description = description, IsPublished = true };
        set.ChallengeSetQuestions = questionIds.Select((id, i) => new ChallengeSetQuestion { QuestionId = id, Order = i + 1 }).ToList();
        context.ChallengeSets.Add(set);
    }

    private static async Task EnsureChallengeSetAsync(
        ApplicationDbContext context,
        string name,
        string description,
        IReadOnlyList<int> questionIds)
    {
        if (await context.ChallengeSets.AnyAsync(x => x.Name == name)) return;

        AddSet(context, name, description, questionIds);
        await context.SaveChangesAsync();
    }

    private static async Task EnsureUserAsync(UserManager<ApplicationUser> manager, string email, string username, string password, string role)
    {
        var user = await manager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser { UserName = username, Email = email, EmailConfirmed = true, DisplayName = username };
            var result = await manager.CreateAsync(user, password);
            if (!result.Succeeded) throw new InvalidOperationException(string.Join("; ", result.Errors.Select(x => x.Description)));
        }
        else
        {
            if (!string.Equals(user.UserName, username, StringComparison.Ordinal))
            {
                var nameResult = await manager.SetUserNameAsync(user, username);
                if (!nameResult.Succeeded) throw new InvalidOperationException(string.Join("; ", nameResult.Errors.Select(x => x.Description)));
            }
            if (user.DisplayName != username)
            {
                user.DisplayName = username;
                await manager.UpdateAsync(user);
            }
        }
        if (!await manager.IsInRoleAsync(user, role)) await manager.AddToRoleAsync(user, role);
    }

    private static async Task RemoveOldSeedStudentsAsync(ApplicationDbContext context, UserManager<ApplicationUser> manager)
    {
        for (var i = 6; i <= 10; i++)
        {
            var user = await manager.FindByEmailAsync($"student{i}@warena.local");
            if (user is null) continue;

            var hostedRoomIds = await context.LiveRooms.Where(x => x.HostUserId == user.Id).Select(x => x.Id).ToListAsync();
            context.LiveAnswers.RemoveRange(context.LiveAnswers.Where(x => x.UserId == user.Id || hostedRoomIds.Contains(x.LiveRoomId)));
            context.LiveParticipants.RemoveRange(context.LiveParticipants.Where(x => x.UserId == user.Id || hostedRoomIds.Contains(x.LiveRoomId)));
            context.LiveRooms.RemoveRange(context.LiveRooms.Where(x => hostedRoomIds.Contains(x.Id)));
            context.Submissions.RemoveRange(context.Submissions.Where(x => x.UserId == user.Id));
            context.UserAchievements.RemoveRange(context.UserAchievements.Where(x => x.UserId == user.Id));
            await context.SaveChangesAsync();
            await manager.DeleteAsync(user);
        }
    }

}
