using AutoPartsStore.Core.Entities;
using AutoPartsStore.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace AutoPartsStore.Web.Controllers;

public class ProductsController : Controller
{
    private readonly IProductRepository _productRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IVehicleModelRepository _vehicleModelRepository;
    private readonly IProductVehicleFitmentRepository _fitmentRepository;

    public ProductsController(
        IProductRepository productRepository,
        ICategoryRepository categoryRepository,
        IVehicleModelRepository vehicleModelRepository,
        IProductVehicleFitmentRepository fitmentRepository)
    {
        _productRepository = productRepository;
        _categoryRepository = categoryRepository;
        _vehicleModelRepository = vehicleModelRepository;
        _fitmentRepository = fitmentRepository;
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
    public async Task<IActionResult> Create()
    {
        await PopulateCategoriesAsync();
        return View();
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Product product)
    {
        if (!ModelState.IsValid)
        {
            await PopulateCategoriesAsync(product.CategoryId);
            return View(product);
        }

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

        await PopulateCategoriesAsync(product.CategoryId);
        await PopulateFitmentDataAsync(id);
        return View(product);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Product product)
    {
        if (id != product.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            await PopulateCategoriesAsync(product.CategoryId);
            await PopulateFitmentDataAsync(id);
            return View(product);
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
}
