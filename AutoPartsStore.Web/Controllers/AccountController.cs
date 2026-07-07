using AutoPartsStore.Core.Interfaces;
using AutoPartsStore.Data.Identity;
using AutoPartsStore.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AutoPartsStore.Web.Controllers;

public class AccountController : Controller
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ICartItemRepository _cartItemRepository;

    public AccountController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        ICartItemRepository cartItemRepository)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _cartItemRepository = cartItemRepository;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);
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
            await _signInManager.SignInAsync(user, isPersistent: false);
            await MergeGuestCartAsync(user.Id);

            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            {
                return Redirect(model.ReturnUrl);
            }

            return RedirectToAction("Index", "Home");
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return View(model);
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
