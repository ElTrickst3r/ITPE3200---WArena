using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WArena.Api.Dtos;
using WArena.Api.Models;
using WArena.Api.Services;

namespace WArena.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(UserManager<ApplicationUser> users, ITokenService tokens, ILogger<AuthController> logger) : ControllerBase
{
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponseDto>> Register(RegisterDto dto)
    {
        var username = dto.Username.Trim();
        if (await users.FindByNameAsync(username) is not null)
            return BadRequest(new { message = "That username is already taken." });

        var user = new ApplicationUser
        {
            UserName = username,
            Email = dto.Email.Trim(),
            DisplayName = username,
            EmailConfirmed = true
        };
        var result = await users.CreateAsync(user, dto.Password);
        if (!result.Succeeded) return BadRequest(new { errors = result.Errors.Select(x => x.Description) });
        await users.AddToRoleAsync(user, "Student");
        logger.LogInformation("SPA user {UserId} registered", user.Id);
        return Ok(await ResponseAsync(user));
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login(LoginDto dto)
    {
        var user = await users.FindByEmailAsync(dto.Email);
        if (user is null || !await users.CheckPasswordAsync(user, dto.Password))
            return Unauthorized(new { message = "Invalid email or password." });
        logger.LogInformation("SPA user {UserId} logged in", user.Id);
        return Ok(await ResponseAsync(user));
    }

    [Authorize]
    [HttpPut("username")]
    public async Task<ActionResult<AuthResponseDto>> UpdateUsername(UpdateUsernameDto dto)
    {
        var user = await CurrentUserAsync();
        if (user is null) return Unauthorized();

        var username = dto.Username.Trim();
        var duplicate = await users.FindByNameAsync(username);
        if (duplicate is not null && duplicate.Id != user.Id)
            return BadRequest(new { message = "That username is already taken." });

        var nameResult = await users.SetUserNameAsync(user, username);
        if (!nameResult.Succeeded) return BadRequest(new { errors = nameResult.Errors.Select(x => x.Description) });
        user.DisplayName = username;
        var updateResult = await users.UpdateAsync(user);
        if (!updateResult.Succeeded) return BadRequest(new { errors = updateResult.Errors.Select(x => x.Description) });
        await users.UpdateSecurityStampAsync(user);

        logger.LogInformation("SPA user {UserId} changed username", user.Id);
        return Ok(await ResponseAsync(user));
    }

    [Authorize]
    [HttpPut("password")]
    public async Task<ActionResult<AuthResponseDto>> ChangePassword(ChangePasswordDto dto)
    {
        var user = await CurrentUserAsync();
        if (user is null) return Unauthorized();
        var result = await users.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);
        if (!result.Succeeded) return BadRequest(new { errors = result.Errors.Select(x => x.Description) });
        logger.LogInformation("SPA user {UserId} changed password", user.Id);
        return Ok(await ResponseAsync(user));
    }

    private async Task<ApplicationUser?> CurrentUserAsync()
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return id is null ? null : await users.FindByIdAsync(id);
    }

    private async Task<AuthResponseDto> ResponseAsync(ApplicationUser user)
    {
        var roles = await users.GetRolesAsync(user);
        var role = roles.FirstOrDefault() ?? "Student";
        return new AuthResponseDto(tokens.CreateToken(user, roles), user.Id, user.UserName ?? user.DisplayName, user.Email ?? string.Empty, role, user.TotalXp, user.CurrentStreak);
    }
}
