using AutoPartsStore.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AutoPartsStore.Data;

public static class DependencyInjection
{
    public static IServiceCollection AddDataServices(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }

    // AddIdentity() специально не регистрируем здесь: этот метод требует
    // сборку Microsoft.AspNetCore.Identity, которая идёт только в составе
    // ASP.NET Core shared framework (доступна в Sdk.Web-проектах).
    // Data — обычная class library, поэтому регистрация Identity вынесена
    // в AutoPartsStore.Web/Program.cs.
}
