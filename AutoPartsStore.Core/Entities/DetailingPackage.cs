using System.ComponentModel.DataAnnotations;

namespace AutoPartsStore.Core.Entities;

// Detailing-Pakete (z. B. Innenreinigung, Lackversiegelung) — Karten im
// Detailing-Bereich (siehe DetailingController). Kurzbeschreibung erscheint
// auf der Kartenübersicht, Content ist die vollständige Beschreibung auf der
// Detailseite (HTML, admin-editierbar per Textarea, wie bei LegalPage).
public class DetailingPackage : BaseEntity
{
    [Required(ErrorMessage = "Name ist erforderlich.")]
    [StringLength(150)]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Kurzbeschreibung ist erforderlich.")]
    [StringLength(300)]
    public string ShortDescription { get; set; } = string.Empty;

    [Required(ErrorMessage = "Bild ist erforderlich.")]
    [StringLength(300)]
    public string ImageUrl { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public int DisplayOrder { get; set; }
}
