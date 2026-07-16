using System.Diagnostics;
using AutoPartsStore.Core.Entities;
using AutoPartsStore.Core.Interfaces;
using AutoPartsStore.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace AutoPartsStore.Web.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IPartnerRepository _partnerRepository;
    private readonly IProductRepository _productRepository;

    public HomeController(
        ILogger<HomeController> logger,
        ICategoryRepository categoryRepository,
        IPartnerRepository partnerRepository,
        IProductRepository productRepository)
    {
        _logger = logger;
        _categoryRepository = categoryRepository;
        _partnerRepository = partnerRepository;
        _productRepository = productRepository;
    }

    public async Task<IActionResult> Index()
    {
        // Nur die oberste Ebene auf der Startseite — Unterkategorien sieht man
        // erst nach dem Reinklicken (siehe Category-Action unten).
        var categories = await _categoryRepository.GetTopLevelAsync();

        var partners = await _partnerRepository.GetAllAsync();
        ViewBag.Partners = partners.OrderBy(p => p.DisplayOrder).ThenBy(p => p.Name).ToList();

        return View(categories);
    }

    // Navigation durch den Kategorie-Baum (z. B. Wartungsteile > Filter > Ölfilter):
    // Hat die Kategorie Unterkategorien, zeigen wir die als Karten (plus Produkte,
    // die direkt dieser Kategorie zugeordnet sind, falls vorhanden). Eine reine
    // Blatt-Kategorie ohne Unterkategorien leitet direkt zur vollen Produktliste
    // mit Such-/Filterfunktion weiter.
    public async Task<IActionResult> Category(int id)
    {
        var category = await _categoryRepository.GetByIdAsync(id);
        if (category is null)
        {
            return NotFound();
        }

        var subcategories = await _categoryRepository.GetSubcategoriesAsync(id);
        if (!subcategories.Any())
        {
            return RedirectToAction("Index", "Products", new { categoryId = id });
        }

        // Breadcrumb: Kette der Elternkategorien von der Wurzel bis zur aktuellen.
        var breadcrumb = new List<Category> { category };
        var current = category;
        while (current.ParentCategoryId.HasValue)
        {
            current = await _categoryRepository.GetByIdAsync(current.ParentCategoryId.Value);
            if (current is null)
            {
                break;
            }

            breadcrumb.Insert(0, current);
        }

        // Produkte, die direkt an dieser (nicht-blattartigen) Kategorie hängen —
        // z. B. Ersatzteile unter "Filter", die zu keiner der Unterkategorien
        // Öl-/Luftfilter passen.
        var directProducts = await _productRepository.GetByCategoryAsync(id);

        ViewBag.Breadcrumb = breadcrumb;
        ViewBag.CurrentCategory = category;
        ViewBag.DirectProducts = directProducts;
        return View(subcategories);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
