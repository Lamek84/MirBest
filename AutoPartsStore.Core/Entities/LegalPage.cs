using System.ComponentModel.DataAnnotations;

namespace AutoPartsStore.Core.Entities;

// Rechtstexte (Impressum, Datenschutz, AGB) werden nicht mehr im Code/den Views
// hartcodiert, sondern hier in der Datenbank gepflegt — Admins können sie über
// LegalController.Edit anpassen, ohne dass ein Deployment nötig ist.
public class LegalPage : BaseEntity
{
    // Eindeutiger, stabiler Schlüssel für Routing/Lookup, z. B. "impressum".
    // Klein geschrieben, entspricht per Konvention dem Controller-Action-Namen
    // (ASP.NET-Routing ist case-insensitiv, daher funktioniert das direkt).
    [Required]
    [StringLength(50)]
    public string Key { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    // HTML-Inhalt der Seite — wird im View über @Html.Raw gerendert. Das ist hier
    // unkritisch, weil nur Admins (Rolle "Admin") diesen Text bearbeiten können,
    // keine normalen Nutzereingaben landen ungefiltert hier.
    [Required]
    public string Content { get; set; } = string.Empty;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
