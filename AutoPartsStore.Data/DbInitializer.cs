using AutoPartsStore.Core.Entities;
using AutoPartsStore.Data.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace AutoPartsStore.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(
        AppDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IConfiguration configuration
        )
    {
        // Создаёт/обновляет схему БД по миграциям. На пустой базе построит все таблицы.
        await context.Database.MigrateAsync();

        await SeedIdentityAsync(userManager, roleManager, configuration);
        await SeedCatalogAsync(context);
        await SeedVehicleMakesAsync(context);
        await SeedLegalPagesAsync(context);
        await SeedPartnersAsync(context);
        await SeedDetailingPackagesAsync(context);
    }

    // Rechtstexte (Impressum, Datenschutz, AGB) UND die Kontaktinformationen-Box
    // (Contact/Index) leben jetzt in der DB statt hartcodiert in .cshtml-Views
    // (siehe LegalController). Nur einmal einfügen, falls der Key noch nicht
    // existiert — spätere Bearbeitungen durch den Admin (LegalController.Edit)
    // werden bei jedem Neustart NICHT überschrieben.
    private static async Task SeedLegalPagesAsync(AppDbContext context)
    {
        var desiredPages = new (string Key, string Title, string Content)[]
        {
            ("impressum", "Impressum", ImpressumContent),
            ("datenschutz", "Datenschutzerklärung", DatenschutzContent),
            ("agb", "Allgemeine Geschäftsbedingungen (AGB)", AgbContent),
            ("widerrufsbelehrung", "Widerrufsbelehrung", WiderrufsbelehrungContent),
            ("kontakt-info", "Kontaktinformationen", KontaktInfoContent),
        };

        foreach (var (key, title, content) in desiredPages)
        {
            var exists = await context.LegalPages.AnyAsync(p => p.Key == key);
            if (!exists)
            {
                context.LegalPages.Add(new LegalPage
                {
                    Key = key,
                    Title = title,
                    Content = content,
                    UpdatedAt = DateTime.UtcNow
                });
            }
        }

        await context.SaveChangesAsync();
    }

    // Логотипы в "Unsere Partner" (Home/Index). Läuft dank SeedFlag NUR BEIM
    // ALLERERSTEN Mal — der Admin kann über PartnersController Partner
    // hinzufügen/bearbeiten/löschen, und gelöschte Partner kommen nach einem
    // Neustart/Deploy NICHT mehr zurück (siehe IsSeededAsync/MarkSeededAsync).
    private static async Task SeedPartnersAsync(AppDbContext context)
    {
        const string seedKey = "Partners";
        if (await IsSeededAsync(context, seedKey))
        {
            return;
        }

        var desiredPartners = new (string Name, string ImageUrl, int DisplayOrder)[]
        {
            ("K&K", "/images/partners/kk.png", 1),
            ("LKQ", "/images/partners/lkq.png", 2),
            ("Castrol", "/images/partners/castrol.png", 3),
            ("Sinus Ambulance", "/images/partners/sinus-ambulance.png", 4),
        };

        foreach (var (name, imageUrl, displayOrder) in desiredPartners)
        {
            var exists = await context.Partners.AnyAsync(p => p.Name == name);
            if (!exists)
            {
                context.Partners.Add(new Partner
                {
                    Name = name,
                    ImageUrl = imageUrl,
                    DisplayOrder = displayOrder
                });
            }
        }

        await MarkSeededAsync(context, seedKey);
        await context.SaveChangesAsync();
    }

    // Startpakete für den Detailing-Bereich (siehe DetailingController). Läuft
    // dank SeedFlag NUR BEIM ALLERERSTEN Mal — gelöschte oder umbenannte
    // Pakete kommen nach einem Neustart/Deploy NICHT mehr zurück.
    private static async Task SeedDetailingPackagesAsync(AppDbContext context)
    {
        const string seedKey = "DetailingPackages";
        if (await IsSeededAsync(context, seedKey))
        {
            return;
        }

        const string placeholderImage = "/images/service-detailing.png";

        var desiredPackages = new (string Name, string ShortDescription, string Content, int DisplayOrder)[]
        {
            (
                "Innenreinigung",
                "Gründliche Reinigung von Polstern, Teppichen und allen Innenflächen.",
                "<p>Wir befreien den Innenraum deines Fahrzeugs von Staub, Flecken und Gerüchen: " +
                "Polster und Teppiche werden tiefengereinigt, Kunststoff- und Lederflächen aufbereitet, " +
                "Fußmatten und Verkleidungen gründlich gesäugt.</p>" +
                "<p>Ideal vor dem Verkauf, nach der Urlaubsreise oder einfach für ein frisches Fahrgefühl.</p>",
                1
            ),
            (
                "Außenaufbereitung",
                "Handwäsche, Felgenreinigung und Politur für einen makellosen Glanz.",
                "<p>Von der schonenden Handwäsche über die Felgenreinigung bis zur Politur kleiner " +
                "Lackkratzer — deine Karosserie erstrahlt wieder wie am ersten Tag.</p>" +
                "<p>Enthalten sind unter anderem: Vorwäsche, Handwäsche, Felgen- und Reifenpflege, " +
                "Trocknung und abschließende Politur.</p>",
                2
            ),
            (
                "Lackversiegelung",
                "Langfristiger Schutz für den Lack mit hochwertiger Versiegelung.",
                "<p>Eine professionelle Lackversiegelung schützt den Lack deines Fahrzeugs vor " +
                "Witterungseinflüssen, UV-Strahlung und leichten Kratzern — für lang anhaltenden Glanz " +
                "und einfachere Pflege im Alltag.</p>" +
                "<p>Empfohlen im Anschluss an eine Außenaufbereitung für maximale Wirkung.</p>",
                3
            ),
        };

        foreach (var (name, shortDescription, content, displayOrder) in desiredPackages)
        {
            var exists = await context.DetailingPackages.AnyAsync(p => p.Name == name);
            if (!exists)
            {
                context.DetailingPackages.Add(new DetailingPackage
                {
                    Name = name,
                    ShortDescription = shortDescription,
                    Content = content,
                    ImageUrl = placeholderImage,
                    DisplayOrder = displayOrder
                });
            }
        }

        await MarkSeededAsync(context, seedKey);
        await context.SaveChangesAsync();
    }

    // Prüft, ob ein einmaliger Seed-Lauf (siehe SeedFlag) schon stattgefunden
    // hat. Wenn ja, wird nichts mehr eingefügt — auch dann nicht, wenn der
    // Admin alle betroffenen Zeilen inzwischen gelöscht hat.
    private static Task<bool> IsSeededAsync(AppDbContext context, string key)
        => context.SeedFlags.AnyAsync(f => f.Key == key);

    private static Task MarkSeededAsync(AppDbContext context, string key)
    {
        context.SeedFlags.Add(new SeedFlag { Key = key, SeededAt = DateTime.UtcNow });
        return Task.CompletedTask;
    }

    // Список известных марок для справочника "подобрать деталь под мою машину".
    // Модели заводятся отдельно через админку по мере необходимости.
    private static async Task SeedVehicleMakesAsync(AppDbContext context)
    {
        string[] makes =
        {
            "Volkswagen", "Audi", "BMW", "Mercedes-Benz", "Opel", "Porsche", "Mini", "Smart",
            "Ford", "Skoda", "Seat", "Renault", "Peugeot", "Citroën", "Fiat", "Alfa Romeo", "Lancia",
            "Toyota", "Honda", "Nissan", "Mazda", "Mitsubishi", "Suzuki", "Subaru",
            "Hyundai", "Kia", "Volvo", "Saab", "Land Rover", "Jaguar",
            "Chevrolet", "Chrysler", "Jeep", "Dodge", "Dacia", "Lada", "Tesla",
            "Lexus", "Infiniti", "Bentley", "Maserati", "Ferrari", "Lamborghini", "Rolls-Royce", "DS Automobiles"
        };

        foreach (var name in makes)
        {
            var exists = await context.VehicleMakes.AnyAsync(m => m.Name == name);
            if (!exists)
            {
                context.VehicleMakes.Add(new VehicleMake { Name = name });
            }
        }

        await context.SaveChangesAsync();
    }

    private static async Task SeedCatalogAsync(AppDbContext context)
    {
        // Läuft dank SeedFlag NUR BEIM ALLERERSTEN Mal — sonst kämen vom Admin
        // gelöschte Demo-Kategorien/-Produkte bei jedem Neustart/Deploy zurück
        // (genau das Problem, das SeedPartnersAsync/SeedDetailingPackagesAsync
        // schon per Flag gelöst haben).
        const string seedKey = "Catalog";
        if (await IsSeededAsync(context, seedKey))
        {
            return;
        }

        var desiredCategories = new (string Name, string ImageUrl)[]
        {
            ("Bremsanlage", "/images/categories/bremsanlage.svg"),
            ("Motor", "/images/categories/motor.svg"),
            ("Fahrwerk", "/images/categories/fahrwerk.svg"),
            ("Elektrik", "/images/categories/elektrik.svg"),
            ("Karosserie", "/images/categories/karosserie.svg"),
        };

        var categoryByName = new Dictionary<string, Category>();
        foreach (var (name, imageUrl) in desiredCategories)
        {
            var category = await context.Categories.FirstOrDefaultAsync(c => c.Name == name);
            if (category is null)
            {
                category = new Category { Name = name, ImageUrl = imageUrl };
                context.Categories.Add(category);
            }
            else if (string.IsNullOrEmpty(category.ImageUrl))
            {
                category.ImageUrl = imageUrl;
            }

            categoryByName[name] = category;
        }

        await context.SaveChangesAsync();

        var desiredProducts = new (string Name, string PartNumber, string Manufacturer, decimal Price, int Stock, string CategoryName, string ImageUrl)[]
        {
            ("Vordere Bremsbeläge", "BP-1001", "Bosch", 1500m, 25, "Bremsanlage", "/images/products/bp-1001.svg"),
            ("Bremsscheibe", "BD-2002", "TRW", 2800m, 15, "Bremsanlage", "/images/products/bd-2002.svg"),
            ("Ölfilter", "OF-3003", "Mann", 450m, 50, "Motor", "/images/products/of-3003.svg"),
            ("Zündkerze", "SP-3004", "NGK", 320m, 100, "Motor", "/images/products/sp-3004.svg"),
            ("Vorderer Stoßdämpfer", "SA-4005", "KYB", 4200m, 10, "Fahrwerk", "/images/products/sa-4005.svg"),
            ("Spiralfeder", "CS-4006", "Eibach", 3600m, 12, "Fahrwerk", "/images/products/cs-4006.svg"),
            ("Autobatterie", "EB-5007", "Varta", 5200m, 8, "Elektrik", "/images/products/eb-5007.svg"),
            ("Scheinwerfer", "HL-5008", "Hella", 6100m, 6, "Elektrik", "/images/products/hl-5008.svg"),
            ("Außenspiegel", "MR-6009", "Magna", 2100m, 20, "Karosserie", "/images/products/mr-6009.svg"),
            ("Stoßstange", "BP2-6010", "OEM", 7300m, 5, "Karosserie", "/images/products/bp2-6010.svg"),
        };

        foreach (var (name, partNumber, manufacturer, price, stock, categoryName, imageUrl) in desiredProducts)
        {
            var product = await context.Products.FirstOrDefaultAsync(p => p.PartNumber == partNumber);
            if (product is null)
            {
                context.Products.Add(new Product
                {
                    Name = name,
                    PartNumber = partNumber,
                    Manufacturer = manufacturer,
                    Price = price,
                    StockQuantity = stock,
                    CategoryId = categoryByName[categoryName].Id,
                    ImageUrl = imageUrl
                });
            }
            else if (string.IsNullOrEmpty(product.ImageUrl))
            {
                product.ImageUrl = imageUrl;
            }
        }

        await MarkSeededAsync(context, seedKey);
        await context.SaveChangesAsync();
    }

    private static async Task SeedIdentityAsync(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, IConfiguration configuration)
    {
        string[] roles = { "Admin", "Customer" };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        var adminEmail = configuration["SeedAdmin:Email"];
        var adminPassword = configuration["SeedAdmin:Password"];
        
        if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
            return;

        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser is null)
        {
            adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(adminUser, adminPassword);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }
        }
    }

    // Platzhalter-Texte für den ersten Start — der echte Inhalt wird über die
    // Admin-Oberfläche (LegalController.Edit) bzw. direkt in der DB gepflegt.
    // Wird nur eingefügt, wenn der Key noch nicht existiert (siehe SeedLegalPagesAsync),
    // eine spätere Bearbeitung hier wird also nicht überschrieben.
    private const string ImpressumContent = "<p>Platzhaltertext – bitte über die Admin-Oberfläche durch den echten Impressum-Inhalt ersetzen.</p>";

    private const string DatenschutzContent = "<p>Platzhaltertext – bitte über die Admin-Oberfläche durch die echte Datenschutzerklärung ersetzen.</p>";

    private const string AgbContent = "<p>Platzhaltertext – bitte über die Admin-Oberfläche durch die echten AGB ersetzen.</p>";

    private const string WiderrufsbelehrungContent = "<p>Platzhaltertext – bitte über die Admin-Oberfläche durch die echte Widerrufsbelehrung (Muster-Widerrufsbelehrung + Muster-Widerrufsformular) ersetzen.</p>";

    // Das ist der bisher hartcodierte Inhalt der "Kontaktinformationen"-Box auf der
    // Kontakt-Seite (Telefon/E-Mail/Adresse/Social-Links) — im Gegensatz zu den
    // Rechtstexten oben schon der echte, aktuelle Inhalt (kein Platzhalter nötig).
    private const string KontaktInfoContent = """
        <p><strong>Telefon:</strong> <a href="tel:+4915566446608">+49 15566446608</a></p>
        <p><strong>E-Mail:</strong> <a href="mailto:info@mirbest.de">info@mirbest.de</a></p>
        <p><strong>Adresse:</strong> Hinterm Sielhof 4, 28277 Bremen</p>
        <p>
            <a href="https://www.facebook.com/mirplusde" target="_blank" rel="noopener">Facebook</a>
            ·
            <a href="https://www.instagram.com/mirbest.de/" target="_blank" rel="noopener">Instagram</a>
        </p>
        """;
}
