namespace AutoPartsStore.Core.Catalog;

// Деталь из внешнего каталога, найденная по VIN. Ключевое поле — OemNumber:
// именно по нему мы сопоставляем позицию со своими товарами
// (IProductRepository.SearchByReferenceNumbersAsync).
public class CatalogPart
{
    public string OemNumber { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string? Brand { get; set; }
    public string? Category { get; set; }
}
