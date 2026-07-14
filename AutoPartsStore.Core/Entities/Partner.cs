using System.ComponentModel.DataAnnotations;

namespace AutoPartsStore.Core.Entities;

// Partner-Logos im Bereich "Unsere Partner" auf der Startseite — vom Admin selbst
// über PartnersController verwaltbar (Logo hochladen + optionaler Link), statt
// wie bisher fest im Code/Views hinterlegt.
public class Partner : BaseEntity
{
    [Required(ErrorMessage = "Name ist erforderlich.")]
    [StringLength(150)]
    public string Name { get; set; } = string.Empty;

    // Pfad zur hochgeladenen Logo-Datei (z. B. /images/partners/xyz.png).
    [Required]
    [StringLength(300)]
    public string ImageUrl { get; set; } = string.Empty;

    // Optional: Klick auf das Logo öffnet die Partner-Website (neuer Tab).
    [StringLength(500)]
    public string? LinkUrl { get; set; }

    // Reihenfolge der Anzeige (aufsteigend). Bei Gleichstand nach Name sortiert.
    public int DisplayOrder { get; set; }
}
