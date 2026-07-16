using AutoPartsStore.Core.Interfaces;
using AutoPartsStore.Infrastructure.Catalog;
using AutoPartsStore.Infrastructure.Email;
using AutoPartsStore.Infrastructure.Payments;
using AutoPartsStore.Infrastructure.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AutoPartsStore.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<ICartItemRepository, CartItemRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IContactMessageRepository, ContactMessageRepository>();
        services.AddScoped<IAppointmentRepository, AppointmentRepository>();
        services.AddScoped<IVehicleMakeRepository, VehicleMakeRepository>();
        services.AddScoped<IVehicleModelRepository, VehicleModelRepository>();
        services.AddScoped<IProductVehicleFitmentRepository, ProductVehicleFitmentRepository>();
        services.AddScoped<IProductReferenceNumberRepository, ProductReferenceNumberRepository>();
        services.AddScoped<ILegalPageRepository, LegalPageRepository>();
        services.AddScoped<IPartnerRepository, PartnerRepository>();
        services.AddScoped<IDetailingPackageRepository, DetailingPackageRepository>();

        // SMTP-настройки берутся из секции "Smtp" в appsettings.json.
        // Пока Host пуст, письма не отправляются (см. SmtpEmailSender),
        // но заявки с формы всё равно сохраняются в БД.
        services.Configure<SmtpSettings>(configuration.GetSection("Smtp"));
        services.AddScoped<IEmailSender, SmtpEmailSender>();

        // Stripe-настройки — секция "Stripe". Лучше держать SecretKey/WebhookSecret
        // не в appsettings.json, а в User Secrets (dotnet user-secrets) или
        // переменных окружения хостинга — appsettings.json попадает в git.
        services.Configure<StripeSettings>(configuration.GetSection("Stripe"));
        services.AddScoped<IPaymentService, StripePaymentService>();

        // Внешний каталог запчастей (поиск по VIN). Пока подключена заглушка —
        // поиск по OEM-номеру из своей базы уже работает, а VIN-поиск отдаёт
        // "не настроено". Чтобы включить реального провайдера: зарегистрировать
        // его реализацию IVehicleCatalogService вместо NullVehicleCatalogService
        // (обычно через services.AddHttpClient<IVehicleCatalogService, XxxService>()).
        services.Configure<VehicleCatalogSettings>(configuration.GetSection("VehicleCatalog"));
        services.AddScoped<IVehicleCatalogService, NullVehicleCatalogService>();

        return services;
    }
}
