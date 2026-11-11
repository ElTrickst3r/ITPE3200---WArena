using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WArena.Mvc.Data;
using WArena.Mvc.Models;
using WArena.Mvc.ViewModels;

namespace WArena.Mvc.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController(
    UserManager<ApplicationUser> userManager,
    ApplicationDbContext db,
    ILogger<AdminController> logger) : Controller
{
    private static readonly string[] AllowedRoles = ["Admin", "Teacher", "Student"];

    public async Task<IActionResult> Users()
    {
        var users = await userManager.Users.OrderBy(x => x.Email).ToListAsync();
        var model = new List<UserListItemViewModel>();
        foreach (var user in users)
        {
            var roles = await userManager.GetRolesAsync(user);
            model.Add(new UserListItemViewModel
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                Username = user.UserName ?? user.DisplayName,
                Role = roles.FirstOrDefault() ?? "Student",
                TotalXp = user.TotalXp
            });
        }
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> EditUser(string id)
    {
        if (id == userManager.GetUserId(User))
        {
            TempData["Error"] = "Use your Profile page to change your own username or password.";
            return RedirectToAction(nameof(Users));
        }

        var user = await userManager.FindByIdAsync(id);
        if (user is null) return NotFound();
        var roles = await userManager.GetRolesAsync(user);
        return View(new AdminEditUserViewModel
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            Username = user.UserName ?? user.DisplayName,
            Role = roles.FirstOrDefault() ?? "Student"
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditUser(AdminEditUserViewModel model)
    {
        if (model.Id == userManager.GetUserId(User))
        {
            TempData["Error"] = "Use your Profile page to change your own account.";
            return RedirectToAction(nameof(Users));
        }

        if (!AllowedRoles.Contains(model.Role)) ModelState.AddModelError(nameof(model.Role), "Invalid role.");
        if (!ModelState.IsValid) return View(model);

        var user = await userManager.FindByIdAsync(model.Id);
        if (user is null) return NotFound();

        var username = model.Username.Trim();
        var duplicate = await userManager.FindByNameAsync(username);
        if (duplicate is not null && duplicate.Id != user.Id)
        {
            ModelState.AddModelError(nameof(model.Username), "That username is already taken.");
            return View(model);
        }

        var currentRoles = await userManager.GetRolesAsync(user);
        if (currentRoles.Contains("Admin") && model.Role != "Admin")
        {
            var admins = await userManager.GetUsersInRoleAsync("Admin");
            if (admins.Count <= 1)
            {
                ModelState.AddModelError(nameof(model.Role), "The last admin cannot be changed to another role.");
                return View(model);
            }
        }

        await using var transaction = await db.Database.BeginTransactionAsync();
        var nameResult = await userManager.SetUserNameAsync(user, username);
        if (!nameResult.Succeeded)
        {
            foreach (var error in nameResult.Errors) ModelState.AddModelError(nameof(model.Username), error.Description);
            return View(model);
        }

        user.DisplayName = username;
        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            foreach (var error in updateResult.Errors) ModelState.AddModelError(string.Empty, error.Description);
            return View(model);
        }

        if (!currentRoles.Contains(model.Role))
        {
            if (currentRoles.Count > 0) await userManager.RemoveFromRolesAsync(user, currentRoles);
            await userManager.AddToRoleAsync(user, model.Role);
        }

        if (!string.IsNullOrWhiteSpace(model.NewPassword))
        {
            var token = await userManager.GeneratePasswordResetTokenAsync(user);
            var passwordResult = await userManager.ResetPasswordAsync(user, token, model.NewPassword);
            if (!passwordResult.Succeeded)
            {
                foreach (var error in passwordResult.Errors) ModelState.AddModelError(nameof(model.NewPassword), error.Description);
                return View(model);
            }
        }
        else
        {
            await userManager.UpdateSecurityStampAsync(user);
        }

        await transaction.CommitAsync();
        logger.LogInformation("Admin updated user {UserId}", user.Id);
        TempData["Message"] = "User updated.";
        return RedirectToAction(nameof(Users));
    }

    [HttpGet]
    public async Task<IActionResult> DeleteUser(string id)
    {
        if (id == userManager.GetUserId(User))
        {
            TempData["Error"] = "You cannot delete your own account while logged in.";
            return RedirectToAction(nameof(Users));
        }

        var user = await userManager.FindByIdAsync(id);
        if (user is null) return NotFound();
        return View(new DeleteUserViewModel
        {
            Id = user.Id,
            Username = user.UserName ?? user.DisplayName,
            Email = user.Email ?? string.Empty
        });
    }

    [HttpPost, ActionName("DeleteUser")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteUserConfirmed(string id)
    {
        if (id == userManager.GetUserId(User))
        {
            TempData["Error"] = "You cannot delete your own account while logged in.";
            return RedirectToAction(nameof(Users));
        }

        var user = await userManager.FindByIdAsync(id);
        if (user is null) return NotFound();

        if (await userManager.IsInRoleAsync(user, "Admin"))
        {
            var admins = await userManager.GetUsersInRoleAsync("Admin");
            if (admins.Count <= 1)
            {
                TempData["Error"] = "The last admin account cannot be deleted.";
                return RedirectToAction(nameof(Users));
            }
        }

        db.Submissions.RemoveRange(db.Submissions.Where(x => x.UserId == id));
        db.UserAchievements.RemoveRange(db.UserAchievements.Where(x => x.UserId == id));
        await db.SaveChangesAsync();

        var result = await userManager.DeleteAsync(user);
        if (!result.Succeeded)
        {
            TempData["Error"] = string.Join("; ", result.Errors.Select(x => x.Description));
            return RedirectToAction(nameof(Users));
        }

        logger.LogInformation("Admin deleted user {UserId}", id);
        TempData["Message"] = "User and related progress data deleted.";
        return RedirectToAction(nameof(Users));
    }
}
