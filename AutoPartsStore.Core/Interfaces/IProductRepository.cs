using AutoPartsStore.Core.Entities;

namespace AutoPartsStore.Core.Interfaces;

public interface IProductRepository : IRepository<Product>
{
    Task<IReadOnlyList<Product>> GetByCategoryAsync(int categoryId);
    Task<Product?> GetByPartNumberAsync(string partNumber);

    // Поиск товаров по OEM/кросс-номеру (или собственному артикулу/SKU).
    // Ввод нормализуется так же, как хранимые номера, поэтому пробелы, дефисы
    // и регистр не мешают совпадению. Основа поиска "по VIN": каталог отдаёт
    // OEM-номер, мы находим по нему свои позиции.
    Task<IReadOnlyList<Product>> SearchByReferenceNumberAsync(string number);

    // Пакетный поиск по набору номеров одним запросом — под результат декода VIN,
    // где каталог возвращает сразу много OEM-номеров. Возвращает товары, у которых
    // совпал хотя бы один номер.
    Task<IReadOnlyList<Product>> SearchByReferenceNumbersAsync(IEnumerable<string> numbers);

    Task<IReadOnlyList<Product>> SearchAsync(
        int? categoryId,
        string? search,
        string? manufacturer,
        decimal? minPrice,
        decimal? maxPrice);

    Task<IReadOnlyList<string>> GetManufacturersAsync();
}
