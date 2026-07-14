using AutoPartsStore.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoPartsStore.Web.Controllers;

// Impressum, Datenschutzerklärung und AGB liegen jetzt als LegalPage-Datensätze
// in der DB (siehe DbInitializer.SeedLegalPagesAsync) statt hartcodiert in Views.
// Öffentliche Aktionen bleiben unter ihren bisherigen Namen, damit vorhandene
// Links (Footer, Widerrufsbelehrung-Verweise usw.) nicht brechen.
public class LegalController : Controller
{
    private readonly ILegalPageRepository _legalPageRepository;

    public LegalController(ILegalPageRepository legalPageRepository)
    {
        _legalPageRepository = legalPageRepository;
    }

    public Task<IActionResult> Impressum() => ShowPageAsync("impressum");

    public Task<IActionResult> Datenschutz() => ShowPageAsync("datenschutz");

    public Task<IActionResult> Agb() => ShowPageAsync("agb");

    public Task<IActionResult> Widerrufsbelehrung() => ShowPageAsync("widerrufsbelehrung");

    private async Task<IActionResult> ShowPageAsync(string key)
    {
        var page = await _legalPageRepository.GetByKeyAsync(key);
        if (page is null)
        {
            return NotFound();
        }

        return View("Page", page);
    }

    // Admin-Übersicht + Bearbeitung. "id" entspricht hier bewusst dem string-Key
    // (z. B. "impressum"), nicht der numerischen LegalPage.Id — so lässt sich per
    // Standardroute /Legal/Edit/impressum direkt darauf zugreifen.
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Index()
    {
        var pages = await _legalPageRepository.GetAllAsync();
        return View(pages.OrderBy(p => p.Title).ToList());
    }

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> Edit(string id)
    {
        var page = await _legalPageRepository.GetByKeyAsync(id);
        if (page is null)
        {
            return NotFound();
        }

        return View(page);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, string title, string content)
    {
        var page = await _legalPageRepository.GetByKeyAsync(id);
        if (page is null)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(content))
        {
            TempData["CartMessage"] = "Titel und Inhalt dürfen nicht leer sein.";
            return RedirectToAction(nameof(Edit), new { id });
        }

        page.Title = title.Trim();
        page.Content = content;
        page.UpdatedAt = DateTime.UtcNow;
        _legalPageRepository.Update(page);
        await _legalPageRepository.SaveChangesAsync();

        TempData["CartMessage"] = "Rechtstext gespeichert.";
        return RedirectToAction(nameof(Edit), new { id });
    }
}
