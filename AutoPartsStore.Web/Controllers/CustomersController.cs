using AutoPartsStore.Data.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AutoPartsStore.Web.Controllers;

// Verwaltung von Kundenkonten durch Admins: Übersicht + manuelle Anpassung
// der Bonuspunkte (z. B. als Treuebonus oder Kulanz-Rabatt für Stammkunden).
[Authorize(Roles = "Admin")]
public class CustomersController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;

    public CustomersController(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var customers = await _userManager.GetUsersInRoleAsync("Customer");
        var sorted = customers.OrderBy(c => c.Email).ToList();
        return View(sorted);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AdjustPoints(string userId, int amount)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return NotFound();
        }

        // Punktestand darf nicht negativ werden, auch wenn ein Admin versehentlich
        // mehr abzieht, als der Kunde hat.
        user.BonusPoints = Math.Max(0, user.BonusPoints + amount);
        await _userManager.UpdateAsync(user);

        TempData["CartMessage"] = amount >= 0
            ? $"{amount} Bonuspunkte wurden {user.Email} gutgeschrieben."
            : $"{Math.Abs(amount)} Bonuspunkte wurden {user.Email} abgezogen.";

        return RedirectToAction(nameof(Index));
    }

    // Freie Kundennummer setzen/ändern — keine automatische Vergabe, keine
    // Eindeutigkeitsprüfung, der Admin kann jede beliebige Zahl eintragen
    // (oder das Feld leeren, um die Nummer wieder zu entfernen).
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetCustomerNumber(string userId, long? customerNumber)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return NotFound();
        }

        user.CustomerNumber = customerNumber;
        await _userManager.UpdateAsync(user);

        TempData["CartMessage"] = customerNumber.HasValue
            ? $"Kundennummer {customerNumber} wurde {user.Email} zugewiesen."
            : $"Kundennummer von {user.Email} wurde entfernt.";

        return RedirectToAction(nameof(Index));
    }
}
