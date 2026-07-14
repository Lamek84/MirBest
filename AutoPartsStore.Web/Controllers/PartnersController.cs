using AutoPartsStore.Core.Entities;
using AutoPartsStore.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoPartsStore.Web.Controllers;

// Partner-Logos im "Unsere Partner"-Bereich der Startseite — Admins laden hier
// ihr eigenes Logo hoch (kein Zugriff auf Server/Code nötig) und können optional
// einen Link zur Partner-Website hinterlegen (siehe Home/Index.cshtml).
[Authorize(Roles = "Admin")]
public class PartnersController : Controller
{
    private static readonly string[] AllowedExtensions = { ".png", ".jpg", ".jpeg", ".svg", ".webp" };
    private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB

    private readonly IPartnerRepository _partnerRepository;
    private readonly IWebHostEnvironment _env;

    public PartnersController(IPartnerRepository partnerRepository, IWebHostEnvironment env)
    {
        _partnerRepository = partnerRepository;
        _env = env;
    }

    public async Task<IActionResult> Index()
    {
        var partners = await _partnerRepository.GetAllAsync();
        return View(partners.OrderBy(p => p.DisplayOrder).ThenBy(p => p.Name).ToList());
    }

    public IActionResult Create() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string name, string? linkUrl, int displayOrder, IFormFile? logo)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            ModelState.AddModelError(string.Empty, "Name ist erforderlich.");
        }

        if (logo is null || logo.Length == 0)
        {
            ModelState.AddModelError(string.Empty, "Bitte ein Logo hochladen.");
        }
        else if (!IsValidLogo(logo, out var validationError))
        {
            ModelState.AddModelError(string.Empty, validationError!);
        }

        if (!ModelState.IsValid)
        {
            return View();
        }

        var partner = new Partner
        {
            Name = name.Trim(),
            ImageUrl = await SaveLogoAsync(logo!),
            LinkUrl = string.IsNullOrWhiteSpace(linkUrl) ? null : linkUrl.Trim(),
            DisplayOrder = displayOrder
        };

        await _partnerRepository.AddAsync(partner);
        await _partnerRepository.SaveChangesAsync();

        TempData["CartMessage"] = $"Partner \"{partner.Name}\" wurde hinzugefügt.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var partner = await _partnerRepository.GetByIdAsync(id);
        if (partner is null)
        {
            return NotFound();
        }

        return View(partner);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, string name, string? linkUrl, int displayOrder, IFormFile? logo)
    {
        var partner = await _partnerRepository.GetByIdAsync(id);
        if (partner is null)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            ModelState.AddModelError(string.Empty, "Name ist erforderlich.");
        }

        if (logo is not null && logo.Length > 0 && !IsValidLogo(logo, out var validationError))
        {
            ModelState.AddModelError(string.Empty, validationError!);
        }

        if (!ModelState.IsValid)
        {
            return View(partner);
        }

        partner.Name = name.Trim();
        partner.LinkUrl = string.IsNullOrWhiteSpace(linkUrl) ? null : linkUrl.Trim();
        partner.DisplayOrder = displayOrder;

        // Nur ersetzen, wenn tatsächlich eine neue Datei hochgeladen wurde —
        // sonst bleibt das bisherige Logo unangetastet.
        if (logo is not null && logo.Length > 0)
        {
            partner.ImageUrl = await SaveLogoAsync(logo);
        }

        _partnerRepository.Update(partner);
        await _partnerRepository.SaveChangesAsync();

        TempData["CartMessage"] = $"Partner \"{partner.Name}\" wurde aktualisiert.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int id)
    {
        var partner = await _partnerRepository.GetByIdAsync(id);
        if (partner is null)
        {
            return NotFound();
        }

        return View(partner);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var partner = await _partnerRepository.GetByIdAsync(id);
        if (partner is not null)
        {
            _partnerRepository.Remove(partner);
            await _partnerRepository.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }

    private static bool IsValidLogo(IFormFile logo, out string? error)
    {
        var extension = Path.GetExtension(logo.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
        {
            error = "Nicht unterstütztes Bildformat. Erlaubt: PNG, JPG, SVG, WEBP.";
            return false;
        }

        if (logo.Length > MaxFileSizeBytes)
        {
            error = "Die Datei ist zu groß (maximal 5 MB).";
            return false;
        }

        error = null;
        return true;
    }

    // Speichert das hochgeladene Logo unter wwwroot/images/partners/ mit einem
    // zufällig generierten Dateinamen (verhindert Kollisionen und Path-Traversal
    // über den vom Nutzer mitgesendeten Originaldateinamen).
    private async Task<string> SaveLogoAsync(IFormFile logo)
    {
        var extension = Path.GetExtension(logo.FileName).ToLowerInvariant();
        var fileName = $"{Guid.NewGuid():N}{extension}";
        var folder = Path.Combine(_env.WebRootPath, "images", "partners");
        Directory.CreateDirectory(folder);
        var filePath = Path.Combine(folder, fileName);

        await using var stream = new FileStream(filePath, FileMode.Create);
        await logo.CopyToAsync(stream);

        return $"/images/partners/{fileName}";
    }
}
