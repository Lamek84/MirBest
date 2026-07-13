using System.Threading.RateLimiting;
using AutoPartsStore.Data;
using AutoPartsStore.Data.Identity;
using AutoPartsStore.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Serilog;

// Serilog настраивается до WebApplication.CreateBuilder, чтобы ловить и логировать
// падения ещё на этапе старта хоста (Log.Logger — статический bootstrap-логгер,
// ниже он заменяется на полноценный через UseSerilog/ReadFrom.Configuration).
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starte AutoPartsStore.Web...");

    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.File(
            "logs/log-.txt",
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 14));

    builder.Services.AddControllersWithViews();

    // Rate limiting для публичных форм без CAPTCHA (login/register — от брутфорса
    // и credential stuffing; contact/appointments — от спам-ботов). Партиционируем
    // по IP, поэтому за NAT/офисным прокси все пользователи делят один лимит —
    // если это станет проблемой, добавить CAPTCHA как более точную защиту.
    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

        options.AddPolicy("AuthPolicy", context => RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));

        options.AddPolicy("FormPolicy", context => RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
    });

    builder.Services.AddDataServices(builder.Configuration);
    builder.Services.AddInfrastructureServices(builder.Configuration);

    // Health-check дёргает реальный SELECT через AppDbContext — годится и как
    // liveness (процесс жив), и как readiness (БД доступна) для одного эндпоинта.
    // Если понадобится разделить — вынести DbContext-проверку в отдельный тег "ready".
    builder.Services.AddHealthChecks()
        .AddDbContextCheck<AppDbContext>();

    builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
        {
            // Продакшен-требования к паролю.
            options.Password.RequiredLength = 8;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireDigit = true;
            options.User.RequireUniqueEmail = true;

            // Вход разрешён только после подтверждения email (см. AccountController.Register/ConfirmEmail).
            // Lokal (Development) ist kein SMTP konfiguriert, daher würde die Bestätigungsmail
            // nie ankommen — dort überspringen wir die Pflicht, damit man sich sofort nach der
            // Registrierung einloggen kann. In Produktion bleibt sie zwingend aktiv.
            options.SignIn.RequireConfirmedAccount = !builder.Environment.IsDevelopment();

            // Блокировка после серии неудачных попыток входа — защита от брутфорса.
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            options.Lockout.AllowedForNewUsers = true;
        })
        .AddEntityFrameworkStores<AppDbContext>()
        .AddDefaultTokenProviders();

    var app = builder.Build();

    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        await DbInitializer.SeedAsync(context, userManager, roleManager, configuration);
    }

    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Home/Error");
        app.UseHsts();
    }

    app.UseHttpsRedirection();

    // Security-заголовки — единой точкой, до статики и роутинга, чтобы применялись
    // ко всем ответам (включая ошибки). CSP оставлена умеренно строгой под текущий
    // Bootstrap/inline-стиль вёрстки; если добавятся сторонние скрипты — расширить allowlist.
    app.Use(async (context, next) =>
    {
        var headers = context.Response.Headers;
        headers["X-Content-Type-Options"] = "nosniff";
        headers["X-Frame-Options"] = "DENY";
        headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
        headers["Content-Security-Policy"] =
            "default-src 'self'; " +
            "script-src 'self'; " +
            "style-src 'self' 'unsafe-inline'; " +
            "img-src 'self' data: https:; " +
            "font-src 'self'; " +
            "frame-ancestors 'none'; " +
            "form-action 'self'; " +
            "connect-src 'self' https://api.stripe.com; " +
            "frame-src https://checkout.stripe.com https://js.stripe.com";

        await next();
    });

    app.UseStaticFiles();

    app.UseRouting();

    app.UseRateLimiter();

    app.UseAuthentication();
    app.UseAuthorization();

    app.UseSerilogRequestLogging();

    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}");

    app.MapHealthChecks("/health");

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    // HostAbortedException прилетает от `dotnet ef` (design-time build хоста
    // для миграций) — это не сбой приложения, логировать как ошибку не нужно.
    Log.Fatal(ex, "AutoPartsStore.Web unerwartet beendet.");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
