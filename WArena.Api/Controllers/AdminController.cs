using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WArena.Api.Data;
using WArena.Api.Dtos;
using WArena.Api.Models;

namespace WArena.Api.Controllers;

[ApiController, Route("api/admin"), Authorize(Roles = "Admin")]
public class AdminController(
    UserManager<ApplicationUser> users,
    ApplicationDbContext db,
    ILogger<AdminController> logger) : ControllerBase
{
    private static readonly string[] AllowedRoles = ["Admin", "Teacher", "Student"];

    [HttpGet("users")]
    public async Task<ActionResult> GetUsers()
    {
        var list = await users.Users.OrderBy(x => x.Email).ToListAsync();
        var result = new List<object>();
        foreach (var user in list)
        {
            var roles = await users.GetRolesAsync(user);
            result.Add(new
            {
                user.Id,
                Username = user.UserName ?? user.DisplayName,
                user.Email,
                user.TotalXp,
                Role = roles.FirstOrDefault() ?? "Student"
            });
        }
        return Ok(result);
    }

    [HttpPut("users/{id}")]
    public async Task<IActionResult> UpdateUser(string id, AdminUpdateUserDto dto)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (id == currentUserId) return BadRequest(new { message = "Use your Profile page to change your own account." });
        if (!AllowedRoles.Contains(dto.Role)) return BadRequest(new { message = "Invalid role." });

        var user = await users.FindByIdAsync(id);
        if (user is null) return NotFound();

        var username = dto.Username.Trim();
        var duplicate = await users.FindByNameAsync(username);
        if (duplicate is not null && duplicate.Id != user.Id)
            return BadRequest(new { message = "That username is already taken." });

        var currentRoles = await users.GetRolesAsync(user);
        if (currentRoles.Contains("Admin") && dto.Role != "Admin")
        {
            var admins = await users.GetUsersInRoleAsync("Admin");
            if (admins.Count <= 1) return BadRequest(new { message = "The last admin cannot be changed to another role." });
        }

        // Alt under skjer i én transaksjon. Returnerer metoden før CommitAsync, rulles alle endringer tilbake.
        await using var transaction = await db.Database.BeginTransactionAsync();

        var nameResult = await users.SetUserNameAsync(user, username);
        if (!nameResult.Succeeded) return BadRequest(new { errors = nameResult.Errors.Select(x => x.Description) });
        user.DisplayName = username;
        var updateResult = await users.UpdateAsync(user);
        if (!updateResult.Succeeded) return BadRequest(new { errors = updateResult.Errors.Select(x => x.Description) });

        if (!currentRoles.Contains(dto.Role))
        {
            if (currentRoles.Count > 0)
            {
                var removeResult = await users.RemoveFromRolesAsync(user, currentRoles);
                if (!removeResult.Succeeded) return BadRequest(new { errors = removeResult.Errors.Select(x => x.Description) });
            }
            var addResult = await users.AddToRoleAsync(user, dto.Role);
            if (!addResult.Succeeded) return BadRequest(new { errors = addResult.Errors.Select(x => x.Description) });
        }

        if (!string.IsNullOrWhiteSpace(dto.NewPassword))
        {
            var token = await users.GeneratePasswordResetTokenAsync(user);
            var passwordResult = await users.ResetPasswordAsync(user, token, dto.NewPassword);
            if (!passwordResult.Succeeded) return BadRequest(new { errors = passwordResult.Errors.Select(x => x.Description) });
        }
        else
        {
            // Username/role changes should invalidate any old JWT that still contains stale claims.
            await users.UpdateSecurityStampAsync(user);
        }

        await transaction.CommitAsync();
        logger.LogInformation("Admin updated user {UserId}", id);
        return NoContent();
    }

    [HttpDelete("users/{id}")]
    public async Task<IActionResult> DeleteUser(string id)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (id == currentUserId) return BadRequest(new { message = "You cannot delete your own account while logged in." });

        var user = await users.FindByIdAsync(id);
        if (user is null) return NotFound();
        if (await users.IsInRoleAsync(user, "Admin"))
        {
            var admins = await users.GetUsersInRoleAsync("Admin");
            if (admins.Count <= 1) return BadRequest(new { message = "The last admin account cannot be deleted." });
        }

        // Alt under skjer i én transaksjon, slik at læringsdata ikke slettes hvis selve brukeren ikke kan slettes.
        await using var transaction = await db.Database.BeginTransactionAsync();

        // Remove the user's learning data and any rooms they hosted before deleting the Identity account.
        var hostedRoomIds = await db.LiveRooms.Where(x => x.HostUserId == id).Select(x => x.Id).ToListAsync();
        db.LiveAnswers.RemoveRange(db.LiveAnswers.Where(x => x.UserId == id || hostedRoomIds.Contains(x.LiveRoomId)));
        db.LiveParticipants.RemoveRange(db.LiveParticipants.Where(x => x.UserId == id || hostedRoomIds.Contains(x.LiveRoomId)));
        db.LiveRooms.RemoveRange(db.LiveRooms.Where(x => hostedRoomIds.Contains(x.Id)));
        db.Submissions.RemoveRange(db.Submissions.Where(x => x.UserId == id));
        db.UserAchievements.RemoveRange(db.UserAchievements.Where(x => x.UserId == id));
        await db.SaveChangesAsync();

        var result = await users.DeleteAsync(user);
        if (!result.Succeeded) return BadRequest(new { errors = result.Errors.Select(x => x.Description) });

        await transaction.CommitAsync();
        logger.LogInformation("Admin deleted user {UserId}", id);
        return NoContent();
    }
}