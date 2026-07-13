using Microsoft.AspNetCore.Identity;

namespace AutoPartsStore.Data.Identity;

public class ApplicationUser : IdentityUser
{
    // Bonuspunkte-Programm: 1 Punkt je bezahltem Euro, 100 Punkte = 1 € Rabatt
    // bei einer künftigen Bestellung (siehe CartController.Checkout / PaymentsController).
    public int BonusPoints { get; set; }
}
