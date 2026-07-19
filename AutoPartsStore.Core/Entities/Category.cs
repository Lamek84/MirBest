using System.ComponentModel.DataAnnotations;

namespace AutoPartsStore.Core.Entities;

public class Category : BaseEntity
{
    [Required(ErrorMessage = "Name ist erforderlich.")]
    [StringLength(150)]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? ImageUrl { get; set; }

    // Reihenfolge der Anzeige unter Geschwistern mit demselben ParentCategoryId
    // (aufsteigend, bei Gleichstand nach Name). Wird über "Nach oben/unten"
    // in CategoriesController.MoveUp/MoveDown gepflegt.
    public int DisplayOrder { get; set; }

    // Selbstreferenz für die Kategorie-Hierarchie (z. B. Wartungsteile > Filter >
    // Ölfilter). Null = oberste Ebene. Siehe HomeController.Category für die
    // öffentliche Navigation durch den Baum.
    public int? ParentCategoryId { get; set; }
    public Category? ParentCategory { get; set; }
    public ICollection<Category> Subcategories { get; set; } = new List<Category>();

    public ICollection<Product> Products { get; set; } = new List<Product>();
}
