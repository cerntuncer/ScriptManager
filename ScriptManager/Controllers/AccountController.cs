using System.Security.Claims;
using DAL.Context;
using DAL.Entities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ScriptManager.Models.Account;

namespace ScriptManager.Controllers;

[AllowAnonymous]
public class AccountController : Controller
{
    private readonly MyContext _db;
    private readonly PasswordHasher<User> _passwordHasher = new();

    public AccountController(MyContext db)
    {
        _db = db;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToLocal(returnUrl);

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var email = model.Email.Trim();
        var user = await _db.Users
            .Include(u => u.Credential)
            .FirstOrDefaultAsync(u => !u.IsDeleted && u.Email == email);

        if (user == null || !user.IsActive || user.Credential == null || user.Credential.IsDeleted)
        {
            ModelState.AddModelError(string.Empty, "E-posta veya şifre hatalı.");
            return View(model);
        }

        var verify = _passwordHasher.VerifyHashedPassword(user, user.Credential.PasswordHash, model.Password);
        if (verify == PasswordVerificationResult.Failed)
        {
            ModelState.AddModelError(string.Empty, "E-posta veya şifre hatalı.");
            return View(model);
        }

        var cred = await _db.UserCredentials.FirstAsync(c => c.UserId == user.Id && !c.IsDeleted);
        cred.LastLoginAt = DateTime.UtcNow;
        if (verify == PasswordVerificationResult.SuccessRehashNeeded)
            cred.PasswordHash = _passwordHasher.HashPassword(user, model.Password);
        cred.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Name),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role.ToString())
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)),
            new AuthenticationProperties
            {
                IsPersistent = false,
                AllowRefresh = true
            });

        return RedirectToLocal(model.ReturnUrl);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }

    private IActionResult RedirectToLocal(string? returnUrl)
    {
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            // "/" bazen tekrar Login köküne düşüp yönlendirme döngüsü yapar; Dashboard'a sabitle.
            if (returnUrl == "/" || returnUrl == "~/" || string.Equals(returnUrl, Url.Content("~/"), StringComparison.Ordinal))
                return RedirectToAction("Index", "Dashboard");
            return Redirect(returnUrl);
        }

        return RedirectToAction("Index", "Dashboard");
    }
}
