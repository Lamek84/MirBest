using AutoPartsStore.Core.Entities;
using AutoPartsStore.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace AutoPartsStore.Web.Controllers;

[Authorize(Roles = "Admin")]
public class CategoriesController : Controller
{
    private static readonly string[] AllowedExtensions = { ".png", ".jpg", ".jpeg", ".svg", ".webp" };
    private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB

    private readonly ICategoryRepository _categoryRepository;
    private readonly IWebHostEnvironment _env;

    public CategoriesController(ICategoryRepository categoryRepository, IWebHostEnvironment env)
    {
        _categoryRepository = categoryRepository;
        _env = env;
    }

    public async Task<IActionResult> Index()
    {
        var categories = await _categoryRepository.GetAllAsync();

        // ParentCategory-Navigation ist hier nicht geladen (kein Include/Lazy Loading),
        // daher Elternnamen selbst per Dictionary auflösen statt c.ParentCategory zu nutzen.
        var namesById = categories.ToDictionary(c => c.Id, c => c.Name);
        ViewBag.ParentNames = namesById;

        var sorted = categories
            .OrderBy(c => c.ParentCategoryId.HasValue && namesById.TryGetValue(c.ParentCategoryId.Value, out var parentName) ? parentName : c.Name)
            .ThenBy(c => c.Name)
            .ToList();

        return View(sorted);
    }

    public async Task<IActionResult> Create(int? parentId)
    {
        await PopulateParentCategoriesAsync(parentId);
        return View(new Category { ParentCategoryId = parentId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Category category, IFormFile? image)
    {
        if (image is not null && image.Length > 0 && !IsValidImage(image, out var validationError))
        {
            ModelState.AddModelError(string.Empty, validationError!);
        }

        if (!ModelState.IsValid)
        {
            await PopulateParentCategoriesAsync(category.ParentCategoryId);
            return View(category);
        }

        if (image is not null && image.Length > 0)
        {
            category.ImageUrl = await SaveImageAsync(image);
        }

        await _categoryRepository.AddAsync(category);
        await _categoryRepository.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var category = await _categoryRepository.GetByIdAsync(id);
        if (category is null)
        {
            return NotFound();
        }

        await PopulateParentCategoriesAsync(category.ParentCategoryId, excludeId: id);
        return View(category);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Category category, IFormFile? image)
    {
        if (id != category.Id)
        {
            return BadRequest();
        }

        if (category.ParentCategoryId == id)
        {
            ModelState.AddModelError(string.Empty, "Eine Kategorie kann nicht ihre eigene Übergeordnete sein.");
        }

        if (image is not null && image.Length > 0 && !IsValidImage(image, out var validationError))
        {
            ModelState.AddModelError(string.Empty, validationError!);
        }

        if (!ModelState.IsValid)
        {
            await PopulateParentCategoriesAsync(category.ParentCategoryId, excludeId: id);
            return View(category);
        }

        // Bisheriges Bild bleibt erhalten, falls keine neue Datei hochgeladen wurde —
        // der aktuelle Pfad kommt über das hidden ImageUrl-Feld im Formular mit
        // (siehe Edit.cshtml), wir müssen ihn hier nicht extra nachladen.
        if (image is not null && image.Length > 0)
        {
            category.ImageUrl = await SaveImageAsync(image);
        }

        _categoryRepository.Update(category);
        await _categoryRepository.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int id)
    {
        var category = await _categoryRepository.GetByIdAsync(id);
        if (category is null)
        {
            return NotFound();
        }

        return View(category);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var category = await _categoryRepository.GetByIdAsync(id);
        if (category is not null)
        {
            try
            {
                _categoryRepository.Remove(category);
                await _categoryRepository.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // У категории ещё есть товары ИЛИ подкатегории (Restrict/защита от
                // каскадного удаления на стороне БД) — сообщаем понятно вместо 500-й ошибки.
                ModelState.AddModelError(string.Empty, "Diese Kategorie kann nicht gelöscht werden, solange ihr noch Ersatzteile oder Unterkategorien zugeordnet sind.");
                return View(category);
            }
        }

        return RedirectToAction(nameof(Index));
    }

    // Auswahlliste für "Übergeordnete Kategorie" in Create/Edit — auf Edit schließen
    // wir die Kategorie selbst aus (kann nicht ihr eigenes Elternteil sein).
    // Tiefere Zyklen (z. B. Enkel als Elternteil) werden hier bewusst nicht geprüft —
    // bei der überschaubaren Kategorienanzahl reicht die Admin-Sorgfalt aus.
    private async Task PopulateParentCategoriesAsync(int? selectedParentId, int? excludeId = null)
    {
        var categories = await _categoryRepository.GetAllAsync();
        var options = categories.Where(c => c.Id != excludeId).OrderBy(c => c.Name);
        ViewBag.ParentCategoryId = new SelectList(options, "Id", "Name", selectedParentId);
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
        var folder = Path.Combine(_env.WebRootPath, "images", "categories");
        Directory.CreateDirectory(folder);
        var filePath = Path.Combine(folder, fileName);

        await using var stream = new FileStream(filePath, FileMode.Create);
        await image.CopyToAsync(stream);

        return $"/images/categories/{fileName}";
    }
}
