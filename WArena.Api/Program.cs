using System.Security.Claims;
using System.Text.Json.Serialization;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using WArena.Api.Data;
using WArena.Api.Hubs;
using WArena.Api.Models;
using WArena.Api.Repositories;
using WArena.Api.Services;

var builder=WebApplication.CreateBuilder(args);
builder.Services.AddControllers().AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddOpenApi();
builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddProblemDetails();
builder.Services.AddSignalR();
builder.Services.AddDbContext<ApplicationDbContext>(o=>o.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddIdentityCore<ApplicationUser>(o=>{o.Password.RequiredLength=8;o.Password.RequireDigit=true;o.Password.RequireUppercase=true;o.Password.RequireLowercase=true;o.Password.RequireNonAlphanumeric=true;o.User.RequireUniqueEmail=true;})
    .AddRoles<IdentityRole>().AddEntityFrameworkStores<ApplicationDbContext>().AddDefaultTokenProviders();

var jwtKey=builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key missing.");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("Jwt:Issuer missing.");
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? throw new InvalidOperationException("Jwt:Audience missing.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(o=>
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
    o.Events=new JwtBearerEvents
    {
        OnMessageReceived=context=>
        {
            var token=context.Request.Query["access_token"];
            if(!string.IsNullOrEmpty(token)&&context.HttpContext.Request.Path.StartsWithSegments("/hubs/live"))context.Token=token;
            return Task.CompletedTask;
        },
        OnTokenValidated=async context=>
        {
            var userId=context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            var tokenStamp=context.Principal?.FindFirst("security_stamp")?.Value;
            var userManager=context.HttpContext.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
            var user=userId is null?null:await userManager.FindByIdAsync(userId);
            if(user is null||!string.Equals(user.SecurityStamp??string.Empty,tokenStamp??string.Empty,StringComparison.Ordinal))
                context.Fail("The account or session is no longer valid.");
        }
    };
});
builder.Services.AddAuthorization();
builder.Services.AddCors(o=>o.AddPolicy("spa",p=>p.WithOrigins("http://localhost:5173").AllowAnyHeader().AllowAnyMethod().AllowCredentials()));

builder.Services.AddScoped<ICategoryRepository,CategoryRepository>();
builder.Services.AddScoped<IQuestionRepository,QuestionRepository>();
builder.Services.AddScoped<IChallengeSetRepository,ChallengeSetRepository>();
builder.Services.AddScoped<IProgressRepository,ProgressRepository>();
builder.Services.AddScoped<ILiveArenaRepository,LiveArenaRepository>();
builder.Services.AddScoped<IGamificationService,GamificationService>();
builder.Services.AddScoped<ITokenService,TokenService>();
builder.Services.AddScoped<ILiveArenaService,LiveArenaService>();

var app=builder.Build();
if (app.Environment.IsDevelopment())
    app.MapOpenApi();
app.UseExceptionHandler();
if (!app.Environment.IsDevelopment()) app.UseHttpsRedirection();
app.UseCors("spa");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<LiveArenaHub>("/hubs/live");
using(var scope=app.Services.CreateScope())await DbInitializer.InitializeAsync(scope.ServiceProvider);
app.Run();
public partial class Program { }
