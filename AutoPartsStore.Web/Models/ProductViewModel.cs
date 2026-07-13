using System.ComponentModel.DataAnnotations;

namespace AutoPartsStore.Web.Models;

// Модель формы Create/Edit для товаров. Отдельно от Product (Core.Entities),
// чтобы биндер модели не мог проставить через форму поля, которых тут нет
// (Id, CreatedAt и любые будущие служебные свойства сущности) — overposting-защита.
public class ProductViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Bezeichnung ist erforderlich.")]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    [StringLength(100)]
    public string? PartNumber { get; set; }

    [StringLength(100)]
    public string? Manufacturer { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Preis darf nicht negativ sein.")]
    public decimal Price { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Bestand darf nicht negativ sein.")]
    public int StockQuantity { get; set; }

    [Required(ErrorMessage = "Kategorie ist erforderlich.")]
    public int CategoryId { get; set; }

    public string? ImageUrl { get; set; }
}
