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
        // Идемпотентно: не просто "если пусто — вставить", а "докатить" то, чего не хватает,
        // и подставить ImageUrl там, где он ещё не задан. Так повторные запуски безопасны
        // даже если каталог уже частично заполнен старыми данными.
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
}
