using AutoPartsStore.Core.Entities;
using AutoPartsStore.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoPartsStore.Web.Controllers;

// Detailing-Pakete: öffentliche Übersicht (Index) + Detailseite (Details) mit
// vollständiger, admin-editierbarer Beschreibung. Admin-Aktionen (Create/Edit/
// Delete) verwalten die Pakete inkl. Logo-Upload (gleiches Muster wie bei
// PartnersController).
public class DetailingController : Controller
{
    private static readonly string[] AllowedExtensions = { ".png", ".jpg", ".jpeg", ".svg", ".webp" };
    private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB

    private readonly IDetailingPackageRepository _packageRepository;
    private readonly IWebHostEnvironment _env;

    public DetailingController(IDetailingPackageRepository packageRepository, IWebHostEnvironment env)
    {
        _packageRepository = packageRepository;
        _env = env;
    }

    public async Task<IActionResult> Index()
    {
        var packages = await _packageRepository.GetAllAsync();
        return View(packages.OrderBy(p => p.DisplayOrder).ThenBy(p => p.Name).ToList());
    }

    public async Task<IActionResult> Details(int id)
    {
        var package = await _packageRepository.GetByIdAsync(id);
        if (package is null)
        {
            return NotFound();
        }

        return View(package);
    }

    [Authorize(Roles = "Admin")]
    public IActionResult Create() => View();

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string name, string shortDescription, string content, int displayOrder, IFormFile? image)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            ModelState.AddModelError(string.Empty, "Name ist erforderlich.");
        }

        if (string.IsNullOrWhiteSpace(shortDescription))
        {
            ModelState.AddModelError(string.Empty, "Kurzbeschreibung ist erforderlich.");
        }

        if (image is null || image.Length == 0)
        {
            ModelState.AddModelError(string.Empty, "Bitte ein Bild hochladen.");
        }
        else if (!IsValidImage(image, out var validationError))
        {
            ModelState.AddModelError(string.Empty, validationError!);
        }

        if (!ModelState.IsValid)
        {
            return View();
        }

        var package = new DetailingPackage
        {
            Name = name.Trim(),
            ShortDescription = shortDescription.Trim(),
            Content = content ?? string.Empty,
            ImageUrl = await SaveImageAsync(image!),
            DisplayOrder = displayOrder
        };

        await _packageRepository.AddAsync(package);
        await _packageRepository.SaveChangesAsync();

        TempData["CartMessage"] = $"Paket \"{package.Name}\" wurde hinzugefügt.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(int id)
    {
        var package = await _packageRepository.GetByIdAsync(id);
        if (package is null)
        {
            return NotFound();
        }

        return View(package);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, string name, string shortDescription, string content, int displayOrder, IFormFile? image)
    {
        var package = await _packageRepository.GetByIdAsync(id);
        if (package is null)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            ModelState.AddModelError(string.Empty, "Name ist erforderlich.");
        }

        if (string.IsNullOrWhiteSpace(shortDescription))
        {
            ModelState.AddModelError(string.Empty, "Kurzbeschreibung ist erforderlich.");
        }

        if (image is not null && image.Length > 0 && !IsValidImage(image, out var validationError))
        {
            ModelState.AddModelError(string.Empty, validationError!);
        }

        if (!ModelState.IsValid)
        {
            return View(package);
        }

        package.Name = name.Trim();
        package.ShortDescription = shortDescription.Trim();
        package.Content = content ?? string.Empty;
        package.DisplayOrder = displayOrder;

        if (image is not null && image.Length > 0)
        {
            package.ImageUrl = await SaveImageAsync(image);
        }

        _packageRepository.Update(package);
        await _packageRepository.SaveChangesAsync();

        TempData["CartMessage"] = $"Paket \"{package.Name}\" wurde aktualisiert.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var package = await _packageRepository.GetByIdAsync(id);
        if (package is null)
        {
            return NotFound();
        }

        return View(package);
    }

    [HttpPost, ActionName("Delete")]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var package = await _packageRepository.GetByIdAsync(id);
        if (package is not null)
        {
            _packageRepository.Remove(package);
            await _packageRepository.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }

    private static bool IsValidImage(IFormFile image, out string? error)
    {
        var extension = Path.GetExtension(image.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
        {
            error = "Nicht unterstütztes Bildformat. Erlaubt: PNG, JPG, SVG, WEBP.";
            return false;
        }

        if (image.Length > MaxFileSizeBytes)
        {
            error = "Die Datei ist zu groß (maximal 5 MB).";
            return false;
        }

        error = null;
        return true;
    }

    private async Task<string> SaveImageAsync(IFormFile image)
    {
        var extension = Path.GetExtension(image.FileName).ToLowerInvariant();
        var fileName = $"{Guid.NewGuid():N}{extension}";
        var folder = Path.Combine(_env.WebRootPath, "images", "detailing");
        Directory.CreateDirectory(folder);
        var filePath = Path.Combine(folder, fileName);

        await using var stream = new FileStream(filePath, FileMode.Create);
        await image.CopyToAsync(stream);

        return $"/images/detailing/{fileName}";
    }
}
