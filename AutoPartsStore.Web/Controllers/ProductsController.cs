using AutoPartsStore.Core.Catalog;
using AutoPartsStore.Core.Entities;
using AutoPartsStore.Core.Interfaces;
using AutoPartsStore.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace AutoPartsStore.Web.Controllers;

public class ProductsController : Controller
{
    private static readonly string[] AllowedExtensions = { ".png", ".jpg", ".jpeg", ".svg", ".webp" };
    private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB

    private readonly IProductRepository _productRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IVehicleModelRepository _vehicleModelRepository;
    private readonly IProductVehicleFitmentRepository _fitmentRepository;
    private readonly IProductReferenceNumberRepository _referenceNumberRepository;
    private readonly IVehicleCatalogService _catalog;
    private readonly IWebHostEnvironment _env;

    public ProductsController(
        IProductRepository productRepository,
        ICategoryRepository categoryRepository,
        IVehicleModelRepository vehicleModelRepository,
        IProductVehicleFitmentRepository fitmentRepository,
        IProductReferenceNumberRepository referenceNumberRepository,
        IVehicleCatalogService catalog,
        IWebHostEnvironment env)
    {
        _productRepository = productRepository;
        _categoryRepository = categoryRepository;
        _vehicleModelRepository = vehicleModelRepository;
        _fitmentRepository = fitmentRepository;
        _referenceNumberRepository = referenceNumberRepository;
        _catalog = catalog;
        _env = env;
    }

    public async Task<IActionResult> Index(
        int? categoryId,
        string? search,
        string? manufacturer,
        decimal? minPrice,
        decimal? maxPrice,
        string? vin)
    {
        // Поиск по VIN — отдельная ветка того же каталога: VIN распознаётся во
        // внешнем каталоге, оттуда берутся OEM-номера, по ним ищем свои товары.
        // Остальные фильтры при VIN-поиске не применяются.
        IReadOnlyList<Product> products;
        if (!string.IsNullOrWhiteSpace(vin))
        {
            products = await SearchByVinAsync(vin);
        }
        else
        {
            products = await _productRepository.SearchAsync(categoryId, search, manufacturer, minPrice, maxPrice);
        }

        if (categoryId.HasValue)
        {
            var category = await _categoryRepository.GetByIdAsync(categoryId.Value);
            ViewData["CategoryName"] = category?.Name;
        }

        var manufacturers = await _productRepository.GetManufacturersAsync();
        ViewBag.Manufacturers = new SelectList(manufacturers, manufacturer);

        ViewData["CurrentCategoryId"] = categoryId;
        ViewData["CurrentSearch"] = search;
        ViewData["CurrentManufacturer"] = manufacturer;
        ViewData["CurrentMinPrice"] = minPrice;
        ViewData["CurrentMaxPrice"] = maxPrice;
        ViewData["CurrentVin"] = vin;

        return View(products);
    }

    // Öffentliche Detailseite eines Ersatzteils — Klick auf eine Produktkarte
    // führt hierher statt direkt in den Warenkorb (siehe Products/Index.cshtml
    // und Home/Category.cshtml).
    public async Task<IActionResult> Details(int id)
    {
        var product = await _productRepository.GetByIdAsync(id);
        if (product is null)
        {
            return NotFound();
        }

        var category = await _categoryRepository.GetByIdAsync(product.CategoryId);
        ViewBag.CategoryName = category?.Name;
        ViewBag.Fitments = await _fitmentRepository.GetByProductAsync(id);

        return View(product);
    }

    // Поиск товаров по VIN через внешний каталог. Пока подключена заглушка
    // (IVehicleCatalogService.IsConfigured == false), показываем подсказку.
    // После подключения реального провайдера этот метод менять не нужно.
    private async Task<IReadOnlyList<Product>> SearchByVinAsync(string vin)
    {
        if (!Vin.IsValid(vin))
        {
            ViewData["VinMessage"] = "Ungültige VIN. Eine VIN besteht aus 17 Zeichen (ohne I, O, Q).";
            return Array.Empty<Product>();
        }

        if (!_catalog.IsConfigured)
        {
            ViewData["VinMessage"] = "Die Suche per VIN ist noch nicht aktiviert. Bitte suchen Sie vorerst über die OEM-Nummer.";
            return Array.Empty<Product>();
        }

        var vehicle = await _catalog.DecodeVinAsync(vin);
        if (vehicle is not null)
        {
            ViewData["VinVehicle"] = $"{vehicle.Make} {vehicle.Model}"
                + (vehicle.Year is not null ? $" ({vehicle.Year})" : string.Empty);
        }

        var parts = await _catalog.GetPartsByVinAsync(vin);
        var products = await _productRepository.SearchByReferenceNumbersAsync(parts.Select(p => p.OemNumber));

        if (products.Count == 0)
        {
            ViewData["VinMessage"] = "Zu dieser VIN wurden keine passenden Teile in unserem Sortiment gefunden.";
        }

        return products;
    }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create(int? categoryId, string? returnUrl)
    {
        await PopulateCategoriesAsync(categoryId);
        ViewBag.ReturnUrl = returnUrl;
        return View(new ProductViewModel { CategoryId = categoryId ?? 0 });
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProductViewModel model, IFormFile? image, string? returnUrl)
    {
        if (image is not null && image.Length > 0 && !IsValidImage(image, out var validationError))
        {
            ModelState.AddModelError(string.Empty, validationError!);
        }

        if (!ModelState.IsValid)
        {
            await PopulateCategoriesAsync(model.CategoryId);
            ViewBag.ReturnUrl = returnUrl;
            return View(model);
        }

        // Явный маппинг ViewModel → Entity: форма не может проставить ничего,
        // кроме перечисленных здесь полей (защита от overposting).
        var product = new Product
        {
            Name = model.Name,
            Description = model.Description,
            Sku = model.Sku,
            PartNumber = model.PartNumber,
            Manufacturer = model.Manufacturer,
            Price = model.Price,
            StockQuantity = model.StockQuantity,
            CategoryId = model.CategoryId,
            ImageUrl = image is not null && image.Length > 0 ? await SaveImageAsync(image) : null
        };

        await _productRepository.AddAsync(product);
        await _productRepository.SaveChangesAsync();
        return RedirectToReturnUrlOrIndex(returnUrl);
    }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(int id, string? returnUrl)
    {
        var product = await _productRepository.GetByIdAsync(id);
        if (product is null)
        {
            return NotFound();
        }

        var model = new ProductViewModel
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            Sku = product.Sku,
            PartNumber = product.PartNumber,
            Manufacturer = product.Manufacturer,
            Price = product.Price,
            StockQuantity = product.StockQuantity,
            CategoryId = product.CategoryId,
            ImageUrl = product.ImageUrl
        };

        await PopulateCategoriesAsync(product.CategoryId);
        await PopulateFitmentDataAsync(id);
        await PopulateReferenceNumbersAsync(id);
        ViewBag.ReturnUrl = returnUrl;
        return View(model);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ProductViewModel model, IFormFile? image, string? returnUrl)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        if (image is not null && image.Length > 0 && !IsValidImage(image, out var validationError))
        {
            ModelState.AddModelError(string.Empty, validationError!);
        }

        if (!ModelState.IsValid)
        {
            await PopulateCategoriesAsync(model.CategoryId);
            await PopulateFitmentDataAsync(id);
            await PopulateReferenceNumbersAsync(id);
            ViewBag.ReturnUrl = returnUrl;
            return View(model);
        }

        // Дозагружаем существующую сущность и переносим только разрешённые
        // поля — вместо биндинга формы напрямую в отслеживаемую EF-сущность.
        var product = await _productRepository.GetByIdAsync(id);
        if (product is null)
        {
            return NotFound();
        }

        product.Name = model.Name;
        product.Description = model.Description;
        product.Sku = model.Sku;
        product.PartNumber = model.PartNumber;
        product.Manufacturer = model.Manufacturer;
        product.Price = model.Price;
        product.StockQuantity = model.StockQuantity;
        product.CategoryId = model.CategoryId;

        // Bisheriges Bild bleibt erhalten, falls keine neue Datei hochgeladen wurde.
        if (image is not null && image.Length > 0)
        {
            product.ImageUrl = await SaveImageAsync(image);
        }

        _productRepository.Update(product);
        await _productRepository.SaveChangesAsync();
        return RedirectToReturnUrlOrIndex(returnUrl);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddFitment(int productId, int vehicleModelId, int? yearFrom, int? yearTo)
    {
        if (yearFrom.HasValue && yearTo.HasValue && yearFrom > yearTo)
        {
            TempData["FitmentError"] = "Das Startjahr darf nicht nach dem Endjahr liegen.";
            return RedirectToAction(nameof(Edit), new { id = productId });
        }

        var model = await _vehicleModelRepository.GetByIdAsync(vehicleModelId);
        if (model is null)
        {
            return NotFound();
        }

        await _fitmentRepository.AddAsync(new ProductVehicleFitment
        {
            ProductId = productId,
            VehicleModelId = vehicleModelId,
            YearFrom = yearFrom,
            YearTo = yearTo
        });

        try
        {
            await _fitmentRepository.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // Такая же связка Modell+Baujahre для этого товара уже есть
            // (сработал уникальный индекс) — не 500-я ошибка, а понятное сообщение.
            TempData["FitmentError"] = "Diese Fahrzeug-Zuordnung existiert für dieses Ersatzteil bereits.";
        }

        return RedirectToAction(nameof(Edit), new { id = productId });
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveFitment(int id, int productId)
    {
        var fitment = await _fitmentRepository.GetByIdAsync(id);
        if (fitment is not null && fitment.ProductId == productId)
        {
            _fitmentRepository.Remove(fitment);
            await _fitmentRepository.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Edit), new { id = productId });
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddReferenceNumber(int productId, string number, ReferenceNumberType type, string? brand)
    {
        if (string.IsNullOrWhiteSpace(number))
        {
            TempData["ReferenceError"] = "Bitte eine Nummer eingeben.";
            return RedirectToAction(nameof(Edit), new { id = productId });
        }

        var normalized = ProductReferenceNumber.Normalize(number);
        if (normalized.Length == 0)
        {
            TempData["ReferenceError"] = "Die Nummer enthält keine gültigen Zeichen.";
            return RedirectToAction(nameof(Edit), new { id = productId });
        }

        await _referenceNumberRepository.AddAsync(new ProductReferenceNumber
        {
            ProductId = productId,
            Number = number.Trim(),
            NormalizedNumber = normalized,
            Type = type,
            Brand = string.IsNullOrWhiteSpace(brand) ? null : brand.Trim()
        });

        try
        {
            await _referenceNumberRepository.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // Такой же номер того же типа у товара уже есть (уникальный индекс).
            TempData["ReferenceError"] = "Diese Nummer ist für dieses Ersatzteil bereits hinterlegt.";
        }

        return RedirectToAction(nameof(Edit), new { id = productId });
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveReferenceNumber(int id, int productId)
    {
        var reference = await _referenceNumberRepository.GetByIdAsync(id);
        if (reference is not null && reference.ProductId == productId)
        {
            _referenceNumberRepository.Remove(reference);
            await _referenceNumberRepository.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Edit), new { id = productId });
    }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id, string? returnUrl)
    {
        var product = await _productRepository.GetByIdAsync(id);
        if (product is null)
        {
            return NotFound();
        }

        ViewBag.ReturnUrl = returnUrl;
        return View(product);
    }

    [HttpPost, ActionName("Delete")]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id, string? returnUrl)
    {
        var product = await _productRepository.GetByIdAsync(id);
        if (product is not null)
        {
            _productRepository.Remove(product);
            await _productRepository.SaveChangesAsync();
        }

        return RedirectToReturnUrlOrIndex(returnUrl);
    }

    // Zurück zur Seite, von der aus die Aktion gestartet wurde (Products/Index
    // mit Filtern oder Home/Category), statt immer starr zur Produktliste.
    // Url.IsLocalUrl schützt vor Open-Redirect über einen manipulierten Parameter.
    private IActionResult RedirectToReturnUrlOrIndex(string? returnUrl)
    {
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateCategoriesAsync(int? selectedCategoryId = null)
    {
        var categories = await _categoryRepository.GetAllAsync();
        ViewBag.CategoryId = new SelectList(categories, "Id", "Name", selectedCategoryId);
    }

    // Список совместимых авто для товара + выпадающий список "Marke – Modell"
    // для формы добавления новой совместимости (см. Products/Edit.cshtml).
    private async Task PopulateFitmentDataAsync(int productId)
    {
        var fitments = await _fitmentRepository.GetByProductAsync(productId);
        ViewBag.Fitments = fitments;

        var models = await _vehicleModelRepository.GetAllWithMakeAsync();
        var modelOptions = models.Select(m => new { m.Id, DisplayText = $"{m.VehicleMake?.Name} – {m.Name}" });
        ViewBag.VehicleModelId = new SelectList(modelOptions, "Id", "DisplayText");
    }

    // OEM/кросс-номера товара для страницы редактирования (см. Products/Edit.cshtml).
    private async Task PopulateReferenceNumbersAsync(int productId)
    {
        ViewBag.ReferenceNumbers = await _referenceNumberRepository.GetByProductAsync(productId);
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
        var folder = Path.Combine(_env.WebRootPath, "images", "products");
        Directory.CreateDirectory(folder);
        var filePath = Path.Combine(folder, fileName);

        await using var stream = new FileStream(filePath, FileMode.Create);
        await image.CopyToAsync(stream);

        return $"/images/products/{fileName}";
    }
}
