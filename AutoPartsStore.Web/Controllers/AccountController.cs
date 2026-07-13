using System.Text;
using AutoPartsStore.Core.Interfaces;
using AutoPartsStore.Data.Identity;
using AutoPartsStore.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.WebUtilities;

namespace AutoPartsStore.Web.Controllers;

public class AccountController : Controller
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ICartItemRepository _cartItemRepository;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        ICartItemRepository cartItemRepository,
        IEmailSender emailSender,
        ILogger<AccountController> logger)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _cartItemRepository = cartItemRepository;
        _emailSender = emailSender;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("AuthPolicy")]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: true);
        if (result.Succeeded)
        {
            var userId = _userManager.GetUserId(User)!;
            await MergeGuestCartAsync(userId);

            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            {
                return Redirect(model.ReturnUrl);
            }

            return RedirectToAction("Index", "Home");
        }

        if (result.IsLockedOut)
        {
            ModelState.AddModelError(string.Empty, "Zu viele Fehlversuche. Bitte versuche es in ein paar Minuten erneut.");
            return View(model);
        }

        if (result.IsNotAllowed)
        {
            // Häufigster Grund bei RequireConfirmedAccount=true: E-Mail noch nicht bestätigt.
            ModelState.AddModelError(string.Empty,
                "Bitte bestätige zuerst deine E-Mail-Adresse (siehe Link in der Bestätigungsmail).");
            return View(model);
        }

        ModelState.AddModelError(string.Empty, "Ungültige Anmeldedaten.");
        return View(model);
    }

    [HttpGet]
    public IActionResult Register(string? returnUrl = null)
    {
        return View(new RegisterViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("AuthPolicy")]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (result.Succeeded)
        {
            await _userManager.AddToRoleAsync(user, "Customer");

            // Kein sofortiges Einloggen mehr — Konto ist erst nach Bestätigung
            // der E-Mail-Adresse aktiv (RequireConfirmedAccount, siehe Program.cs).
            await SendConfirmationEmailAsync(user);

            return RedirectToAction(nameof(RegisterConfirmation), new { email = user.Email });
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return View(model);
    }

    [HttpGet]
    public IActionResult RegisterConfirmation(string email)
    {
        ViewData["Email"] = email;
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> ConfirmEmail(string userId, string token)
    {
        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
        {
            return RedirectToAction(nameof(Login));
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return View("ConfirmEmail", model: false);
        }

        var decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token));
        var result = await _userManager.ConfirmEmailAsync(user, decodedToken);

        return View("ConfirmEmail", model: result.Succeeded);
    }

    [HttpGet]
    public IActionResult ResendConfirmation() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("AuthPolicy")]
    public async Task<IActionResult> ResendConfirmation(string email)
    {
        // Immer dieselbe Meldung, egal ob die Adresse existiert oder schon
        // bestätigt ist — sonst lässt sich über diese Form erraten, welche
        // E-Mails registriert sind (User-Enumeration).
        var user = await _userManager.FindByEmailAsync(email);
        if (user is not null && !user.EmailConfirmed)
        {
            await SendConfirmationEmailAsync(user);
        }

        TempData["CartMessage"] = "Falls diese Adresse registriert und noch nicht bestätigt ist, haben wir eine neue Bestätigungsmail gesendet.";
        return RedirectToAction(nameof(Login));
    }

    private async Task SendConfirmationEmailAsync(ApplicationUser user)
    {
        var rawToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(rawToken));

        var confirmationLink = Url.Action(
            nameof(ConfirmEmail), "Account",
            new { userId = user.Id, token = encodedToken },
            Request.Scheme)!;

        try
        {
            await _emailSender.SendEmailAsync(
                user.Email!,
                "Bestätige deine E-Mail-Adresse – MIRBEST",
                $"Willkommen bei MIRBEST!\n\nBitte bestätige deine E-Mail-Adresse über diesen Link:\n{confirmationLink}\n\nWenn du dich nicht registriert hast, ignoriere diese E-Mail.");
        }
        catch (Exception ex)
        {
            // SMTP nicht konfiguriert/erreichbar — Konto existiert bereits (siehe CreateAsync
            // oben), also nicht den ganzen Request fehlschlagen lassen, nur loggen.
            _logger.LogWarning(ex, "Bestätigungsmail für {Email} konnte nicht gesendet werden.", user.Email);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }

    [Authorize]
    [HttpGet]
    public IActionResult ChangePassword()
    {
        return View(new ChangePasswordViewModel());
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return RedirectToAction("Login");
        }

        var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
        if (result.Succeeded)
        {
            await _signInManager.RefreshSignInAsync(user);
            TempData["CartMessage"] = "Passwort erfolgreich geändert.";
            return RedirectToAction("Index", "Home");
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return View(model);
    }

    // Переносит товары из гостевой корзины (по cookie) в корзину только что
    // вошедшего/зарегистрированного пользователя. Если один и тот же товар
    // уже есть в обеих корзинах — суммируем количество.
    private async Task MergeGuestCartAsync(string realUserId)
    {
        if (!Request.Cookies.TryGetValue(CartConstants.GuestCookieName, out var guestId) || string.IsNullOrEmpty(guestId))
        {
            return;
        }

        var guestItems = await _cartItemRepository.GetByUserAsync(guestId);
        foreach (var guestItem in guestItems)
        {
            var existing = await _cartItemRepository.GetByUserAndProductAsync(realUserId, guestItem.ProductId);
            if (existing is null)
            {
                guestItem.UserId = realUserId;
                _cartItemRepository.Update(guestItem);
            }
            else
            {
                existing.Quantity += guestItem.Quantity;
                _cartItemRepository.Update(existing);
                _cartItemRepository.Remove(guestItem);
            }
        }

        if (guestItems.Any())
        {
            await _cartItemRepository.SaveChangesAsync();
        }

        Response.Cookies.Delete(CartConstants.GuestCookieName);
    }
}
