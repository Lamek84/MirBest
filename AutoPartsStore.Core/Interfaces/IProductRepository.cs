using AutoPartsStore.Core.Entities;

namespace AutoPartsStore.Core.Interfaces;

public interface IProductRepository : IRepository<Product>
{
    Task<IReadOnlyList<Product>> GetByCategoryAsync(int categoryId);
    Task<Product?> GetByPartNumberAsync(string partNumber);

    Task<IReadOnlyList<Product>> SearchAsync(
        int? categoryId,
        string? search,
        string? manufacturer,
        decimal? minPrice,
        decimal? maxPrice);

    Task<IReadOnlyList<string>> GetManufacturersAsync();
}
