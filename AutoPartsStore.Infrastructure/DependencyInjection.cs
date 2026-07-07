using AutoPartsStore.Core.Interfaces;
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

        return services;
    }
}
