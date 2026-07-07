using System.ComponentModel.DataAnnotations;

namespace AutoPartsStore.Core.Entities;

public class Product : BaseEntity
{
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
    public Category? Category { get; set; }

    public string? ImageUrl { get; set; }
}
