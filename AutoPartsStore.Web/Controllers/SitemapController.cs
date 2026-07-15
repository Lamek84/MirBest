using System.Text;
using AutoPartsStore.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AutoPartsStore.Web.Controllers;

// Generiert sitemap.xml dynamisch statt als statische Datei — so tauchen neue
// Detailing-Pakete automatisch auf, ohne dass jemand die Datei von Hand pflegen muss.
public class SitemapController : Controller
{
    private readonly IDetailingPackageRepository _detailingPackageRepository;

    public SitemapController(IDetailingPackageRepository detailingPackageRepository)
    {
        _detailingPackageRepository = detailingPackageRepository;
    }

    [HttpGet("sitemap.xml")]
    public async Task<IActionResult> Index()
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";

        var urls = new List<(string Loc, string ChangeFreq, string Priority)>
        {
            ($"{baseUrl}/", "weekly", "1.0"),
            ($"{baseUrl}/Products", "weekly", "0.9"),
            ($"{baseUrl}/Detailing", "weekly", "0.9"),
            ($"{baseUrl}/Appointments/Book", "monthly", "0.8"),
            ($"{baseUrl}/Contact", "monthly", "0.6"),
            ($"{baseUrl}/Legal/Impressum", "yearly", "0.3"),
            ($"{baseUrl}/Legal/Datenschutz", "yearly", "0.3"),
            ($"{baseUrl}/Legal/Agb", "yearly", "0.3"),
            ($"{baseUrl}/Legal/Widerrufsbelehrung", "yearly", "0.3"),
        };

        var packages = await _detailingPackageRepository.GetAllAsync();
        foreach (var package in packages)
        {
            urls.Add(($"{baseUrl}/Detailing/Details/{package.Id}", "monthly", "0.7"));
        }

        var xml = new StringBuilder();
        xml.Append("<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n");
        xml.Append("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">\n");

        foreach (var (loc, changeFreq, priority) in urls)
        {
            xml.Append("  <url>\n");
            xml.Append($"    <loc>{System.Security.SecurityElement.Escape(loc)}</loc>\n");
            xml.Append($"    <changefreq>{changeFreq}</changefreq>\n");
            xml.Append($"    <priority>{priority}</priority>\n");
            xml.Append("  </url>\n");
        }

        xml.Append("</urlset>");

        return Content(xml.ToString(), "application/xml", Encoding.UTF8);
    }
}
