using Microsoft.AspNetCore.Mvc;

namespace AutoPartsStore.Web.Controllers;

// Impressum und Datenschutzerklärung — rein statische, informative Seiten.
// Kein Zugriff auf Repositories nötig.
public class LegalController : Controller
{
    public IActionResult Impressum()
    {
        return View();
    }

    public IActionResult Datenschutz()
    {
        return View();
    }
}
