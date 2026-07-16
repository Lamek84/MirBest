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
    private readonly IWebHostEnvironment _env;

    public ProductsController(
        IProductRepository productRepository,
        ICategoryRepository categoryRepository,
        IVehicleModelRepository vehicleModelRepository,
        IProductVehicleFitmentRepository fitmentRepository,
        IWebHostEnvironment env)
    {
        _productRepository = productRepository;
        _categoryRepository = categoryRepository;
        _vehicleModelRepository = vehicleModelRepository;
        _fitmentRepository = fitmentRepository;
        _env = env;
    }

    public async Task<IActionResult> Index(
        int? categoryId,
        string? search,
        string? manufacturer,
        decimal? minPrice,
        decimal? maxPrice)
    {
        var products = await _productRepository.SearchAsync(categoryId, search, manufacturer, minPrice, maxPrice);

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

        return View(products);
    }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create(int? categoryId)
    {
        await PopulateCategoriesAsync(categoryId);
        return View(new ProductViewModel { CategoryId = categoryId ?? 0 });
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProductViewModel model, IFormFile? image)
    {
        if (image is not null && image.Length > 0 && !IsValidImage(image, out var validationError))
        {
            ModelState.AddModelError(string.Empty, validationError!);
        }

        if (!ModelState.IsValid)
        {
            await PopulateCategoriesAsync(model.CategoryId);
            return View(model);
        }

        // Явный маппинг ViewModel → Entity: форма не может проставить ничего,
        // кроме перечисленных здесь полей (защита от overposting).
        var product = new Product
        {
            Name = model.Name,
            Description = model.Description,
            PartNumber = model.PartNumber,
            Manufacturer = model.Manufacturer,
            Price = model.Price,
            StockQuantity = model.StockQuantity,
            CategoryId = model.CategoryId,
            ImageUrl = image is not null && image.Length > 0 ? await SaveImageAsync(image) : null
        };

        await _productRepository.AddAsync(product);
        await _productRepository.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(int id)
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
            PartNumber = product.PartNumber,
            Manufacturer = product.Manufacturer,
            Price = product.Price,
            StockQuantity = product.StockQuantity,
            CategoryId = product.CategoryId,
            ImageUrl = product.ImageUrl
        };

        await PopulateCategoriesAsync(product.CategoryId);
        await PopulateFitmentDataAsync(id);
        return View(model);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ProductViewModel model, IFormFile? image)
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
        return RedirectToAction(nameof(Index));
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

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var product = await _productRepository.GetByIdAsync(id);
        if (product is null)
        {
            return NotFound();
        }

        return View(product);
    }

    [HttpPost, ActionName("Delete")]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var product = await _productRepository.GetByIdAsync(id);
        if (product is not null)
        {
            _productRepository.Remove(product);
            await _productRepository.SaveChangesAsync();
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
