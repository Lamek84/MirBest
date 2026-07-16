using System.ComponentModel.DataAnnotations;

namespace AutoPartsStore.Core.Entities;

public class Product : BaseEntity
{
    [Required(ErrorMessage = "Bezeichnung ist erforderlich.")]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    // Внутренний складской код магазина ("Ваш SKU"). Nullable, потому что
    // старые товары его не имеют; уникальность обеспечивается фильтрованным
    // индексом в ProductConfiguration (только для непустых значений).
    [StringLength(100)]
    public string? Sku { get; set; }

    // Артикул производителя ("Артикул производителя").
    [StringLength(100)]
    public string? PartNumber { get; set; }

    // Бренд товара ("Бренд товара"). Историческое имя поля Manufacturer сохранено,
    // чтобы не ломать существующие данные и миграции.
    [StringLength(100)]
    public string? Manufacturer { get; set; }

    // OEM- и кросс-номера для поиска по VIN/номеру детали. Одна деталь подходит
    // под множество OEM-номеров разных авто, поэтому связь один-ко-многим,
    // а не отдельные строковые поля (см. ProductReferenceNumber).
    public ICollection<ProductReferenceNumber> ReferenceNumbers { get; set; }
        = new List<ProductReferenceNumber>();

    [Range(0, double.MaxValue, ErrorMessage = "Preis darf nicht negativ sein.")]
    public decimal Price { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Bestand darf nicht negativ sein.")]
    public int StockQuantity { get; set; }

    [Required(ErrorMessage = "Kategorie ist erforderlich.")]
    public int CategoryId { get; set; }
    public Category? Category { get; set; }

    public string? ImageUrl { get; set; }
}
