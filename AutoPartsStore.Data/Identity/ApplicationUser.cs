using Microsoft.AspNetCore.Identity;

namespace AutoPartsStore.Data.Identity;

public class ApplicationUser : IdentityUser
{
    // Bonuspunkte-Programm: 1 Punkt je bezahltem Euro, 100 Punkte = 1 € Rabatt
    // bei einer künftigen Bestellung (siehe CartController.Checkout / PaymentsController).
    public int BonusPoints { get; set; }

    // Frei vom Admin vergebene Kundennummer (z. B. aus einem früheren System
    // übernommen) — kein Auto-Increment, keine Eindeutigkeitsprüfung, einfach
    // eine beliebige Zahl zur eigenen Zuordnung (siehe CustomersController).
    public long? CustomerNumber { get; set; }
}
