using AutoPartsStore.Core.Entities;
using AutoPartsStore.Core.Interfaces;
using AutoPartsStore.Data;
using Microsoft.EntityFrameworkCore;

namespace AutoPartsStore.Infrastructure.Repositories;

public class ProductRepository : Repository<Product>, IProductRepository
{
    public ProductRepository(AppDbContext context) : base(context) { }

    public async Task<IReadOnlyList<Product>> GetByCategoryAsync(int categoryId) =>
        await DbSet.Where(p => p.CategoryId == categoryId).ToListAsync();

    public async Task<Product?> GetByPartNumberAsync(string partNumber) =>
        await DbSet.FirstOrDefaultAsync(p => p.PartNumber == partNumber);

    public async Task<IReadOnlyList<Product>> SearchByReferenceNumberAsync(string number)
    {
        var normalized = ProductReferenceNumber.Normalize(number);
        if (normalized.Length == 0)
        {
            return Array.Empty<Product>();
        }

        // Совпадение по OEM/кросс-номеру, либо по собственному артикулу/SKU
        // (тоже нормализуем на стороне БД, убирая пробелы и дефисы).
        return await DbSet
            .Where(p =>
                p.ReferenceNumbers.Any(r => r.NormalizedNumber == normalized) ||
                (p.PartNumber != null &&
                    EF.Functions.Like(
                        p.PartNumber.Replace(" ", "").Replace("-", ""),
                        normalized)) ||
                (p.Sku != null &&
                    EF.Functions.Like(
                        p.Sku.Replace(" ", "").Replace("-", ""),
                        normalized)))
            .OrderBy(p => p.Name)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Product>> SearchByReferenceNumbersAsync(IEnumerable<string> numbers)
    {
        var normalizedSet = numbers
            .Select(ProductReferenceNumber.Normalize)
            .Where(n => n.Length > 0)
            .Distinct()
            .ToList();

        if (normalizedSet.Count == 0)
        {
            return Array.Empty<Product>();
        }

        return await DbSet
            .Where(p => p.ReferenceNumbers.Any(r => normalizedSet.Contains(r.NormalizedNumber)))
            .OrderBy(p => p.Name)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Product>> SearchAsync(
        int? categoryId,
        string? search,
        string? manufacturer,
        decimal? minPrice,
        decimal? maxPrice)
    {
        var query = DbSet.AsQueryable();

        if (categoryId.HasValue)
        {
            query = query.Where(p => p.CategoryId == categoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            var normalized = ProductReferenceNumber.Normalize(term);
            query = query.Where(p =>
                EF.Functions.Like(p.Name, $"%{term}%") ||
                (p.PartNumber != null && EF.Functions.Like(p.PartNumber, $"%{term}%")) ||
                (p.Sku != null && EF.Functions.Like(p.Sku, $"%{term}%")) ||
                (normalized != "" &&
                    p.ReferenceNumbers.Any(r =>
                        EF.Functions.Like(r.NormalizedNumber, $"%{normalized}%"))));
        }

        if (!string.IsNullOrWhiteSpace(manufacturer))
        {
            query = query.Where(p => p.Manufacturer == manufacturer);
        }

        if (minPrice.HasValue)
        {
            query = query.Where(p => p.Price >= minPrice.Value);
        }

        if (maxPrice.HasValue)
        {
            query = query.Where(p => p.Price <= maxPrice.Value);
        }

        return await query.OrderBy(p => p.Name).ToListAsync();
    }

    public async Task<IReadOnlyList<string>> GetManufacturersAsync() =>
        await DbSet
            .Where(p => p.Manufacturer != null)
            .Select(p => p.Manufacturer!)
            .Distinct()
            .OrderBy(m => m)
            .ToListAsync();
}
