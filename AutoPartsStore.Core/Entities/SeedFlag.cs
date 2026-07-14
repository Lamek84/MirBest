namespace AutoPartsStore.Core.Entities;

// Markiert einmalige Seed-Läufe (siehe DbInitializer) als "bereits erledigt" —
// unabhängig davon, ob die geseedeten Zeilen später vom Admin gelöscht wurden.
// Ohne das würde z. B. ein gelöschtes Detailing-Paket oder ein gelöschter
// Partner bei jedem Neustart/Deploy wieder auftauchen, weil der alte Seed nur
// "existiert der Name schon?" geprüft hat, statt "wurde schon einmal geseedet?".
public class SeedFlag
{
    public string Key { get; set; } = string.Empty;
    public DateTime SeededAt { get; set; } = DateTime.UtcNow;
}
