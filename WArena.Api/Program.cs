using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using WArena.Api.Data;
using WArena.Api.Hubs;
using WArena.Api.Models;
using WArena.Api.Repositories;
using WArena.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Controllers, JSON and API documentation.
// Enums are serialized as strings both in responses and in the OpenAPI description.
builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddSignalR();

// Database and Identity.
builder.Services.AddDbContext<ApplicationDbContext>(o =>
    o.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentityCore<ApplicationUser>(o =>
    {
        o.Password.RequiredLength = 8;
        o.Password.RequireDigit = true;
        o.Password.RequireUppercase = true;
        o.Password.RequireLowercase = true;
        o.Password.RequireNonAlphanumeric = true;
        o.User.RequireUniqueEmail = true;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// JWT authentication. The API refuses to start if any of these settings are missing.
var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key missing.");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("Jwt:Issuer missing.");
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? throw new InvalidOperationException("Jwt:Audience missing.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(o =>
{
    o.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ClockSkew = TimeSpan.FromMinutes(1)
    };

    o.Events = new JwtBearerEvents
    {
        // SignalR cannot send headers over WebSockets, so the hub receives the token as a query parameter.
        OnMessageReceived = context =>
        {
            var token = context.Request.Query["access_token"];
            if (!string.IsNullOrEmpty(token) && context.HttpContext.Request.Path.StartsWithSegments("/hubs/live"))
                context.Token = token;
            return Task.CompletedTask;
        },

        // Rejects tokens whose security stamp no longer matches the database,
        // so role or password changes take effect immediately instead of when the token expires.
        OnTokenValidated = async context =>
        {
            var userId = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            var tokenStamp = context.Principal?.FindFirst("security_stamp")?.Value;
            var userManager = context.HttpContext.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
            var user = userId is null ? null : await userManager.FindByIdAsync(userId);

            if (user is null || !string.Equals(user.SecurityStamp ?? string.Empty, tokenStamp ?? string.Empty, StringComparison.Ordinal))
                context.Fail("The account or session is no longer valid.");
        }
    };
});

builder.Services.AddAuthorization();
builder.Services.AddCors(o => o.AddPolicy("spa", p => p
    .WithOrigins("http://localhost:5173")
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials()));

// Data access layer and services.
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<IQuestionRepository, QuestionRepository>();
builder.Services.AddScoped<IChallengeSetRepository, ChallengeSetRepository>();
builder.Services.AddScoped<IProgressRepository, ProgressRepository>();
builder.Services.AddScoped<ILiveArenaRepository, LiveArenaRepository>();
builder.Services.AddScoped<IGamificationService, GamificationService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<ILiveArenaService, LiveArenaService>();

var app = builder.Build();

// Middleware pipeline.
app.UseExceptionHandler();
if (!app.Environment.IsDevelopment())
    app.UseHttpsRedirection();
app.UseCors("spa");
app.UseAuthentication();
app.UseAuthorization();

// Endpoints.
app.MapControllers();
app.MapHub<LiveArenaHub>("/hubs/live");
if (app.Environment.IsDevelopment())
    app.MapOpenApi();

// Creates the database and seeds roles, test users and sample content on startup.
using (var scope = app.Services.CreateScope())
    await DbInitializer.InitializeAsync(scope.ServiceProvider);

app.Run();

// Makes Program visible to integration tests.
public partial class Program { }