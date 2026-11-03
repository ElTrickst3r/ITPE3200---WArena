using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WArena.Mvc.Models;
using WArena.Mvc.ViewModels;

namespace WArena.Mvc.Controllers;

public class AccountController(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    ILogger<AccountController> logger) : Controller
{
    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel());
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        if (!ModelState.IsValid) return View(model);

        var user = await userManager.FindByEmailAsync(model.Email);
        if (user is null)
        {
            ModelState.AddModelError(string.Empty, "Invalid email or password.");
            logger.LogWarning("Failed login for unknown email {Email}", model.Email);
            return View(model);
        }

        var result = await signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, lockoutOnFailure: false);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, "Invalid email or password.");
            logger.LogWarning("Failed login for user {UserId}", user.Id);
            return View(model);
        }

        logger.LogInformation("User {UserId} logged in", user.Id);
        return LocalRedirect(returnUrl ?? Url.Action("Index", "Home")!);
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Register() => View(new RegisterViewModel());

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        if (await userManager.FindByNameAsync(model.Username.Trim()) is not null)
        {
            ModelState.AddModelError(nameof(model.Username), "That username is already taken.");
            return View(model);
        }

        var username = model.Username.Trim();
        var user = new ApplicationUser
        {
            UserName = username,
            Email = model.Email.Trim(),
            DisplayName = username,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors) ModelState.AddModelError(string.Empty, error.Description);
            return View(model);
        }

        await userManager.AddToRoleAsync(user, "Student");
        await signInManager.SignInAsync(user, isPersistent: false);
        logger.LogInformation("New student {UserId} registered", user.Id);
        return RedirectToAction("Index", "Home");
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null) return Challenge();
        return View(new ProfileViewModel
        {
            Email = user.Email ?? string.Empty,
            Username = user.UserName ?? user.DisplayName
        });
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateUsername(UsernameUpdateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = ModelState.Values.SelectMany(x => x.Errors).FirstOrDefault()?.ErrorMessage ?? "Invalid username.";
            return RedirectToAction(nameof(Profile));
        }

        var user = await userManager.GetUserAsync(User);
        if (user is null) return Challenge();

        var username = model.Username.Trim();
        var existing = await userManager.FindByNameAsync(username);
        if (existing is not null && existing.Id != user.Id)
        {
            TempData["Error"] = "That username is already taken.";
            return RedirectToAction(nameof(Profile));
        }

        var nameResult = await userManager.SetUserNameAsync(user, username);
        if (!nameResult.Succeeded)
        {
            TempData["Error"] = string.Join("; ", nameResult.Errors.Select(x => x.Description));
            return RedirectToAction(nameof(Profile));
        }

        user.DisplayName = username;
        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            TempData["Error"] = string.Join("; ", updateResult.Errors.Select(x => x.Description));
            return RedirectToAction(nameof(Profile));
        }
        await userManager.UpdateSecurityStampAsync(user);

        await signInManager.RefreshSignInAsync(user);
        logger.LogInformation("User {UserId} changed username", user.Id);
        TempData["Message"] = "Username updated.";
        return RedirectToAction(nameof(Profile));
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = ModelState.Values.SelectMany(x => x.Errors).FirstOrDefault()?.ErrorMessage ?? "Invalid password.";
            return RedirectToAction(nameof(Profile));
        }

        var user = await userManager.GetUserAsync(User);
        if (user is null) return Challenge();

        var result = await userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
        if (!result.Succeeded)
        {
            TempData["Error"] = string.Join("; ", result.Errors.Select(x => x.Description));
            return RedirectToAction(nameof(Profile));
        }

        await signInManager.RefreshSignInAsync(user);
        logger.LogInformation("User {UserId} changed password", user.Id);
        TempData["Message"] = "Password changed.";
        return RedirectToAction(nameof(Profile));
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    [AllowAnonymous]
    public IActionResult AccessDenied() => View();
}
