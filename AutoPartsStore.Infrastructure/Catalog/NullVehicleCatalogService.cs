using AutoPartsStore.Core.Catalog;
using AutoPartsStore.Core.Interfaces;

namespace AutoPartsStore.Infrastructure.Catalog;

// Заглушка каталога: провайдер ещё не подключён. Позволяет всему приложению
// собираться и работать (поиск по OEM-номеру из своей базы функционирует уже
// сейчас), а поиск по VIN отдаёт "не настроено" вместо ошибки.
//
// Чтобы подключить реальный каталог: создать, например, PartsCatalogsService
// : IVehicleCatalogService на HttpClient и заменить регистрацию в
// DependencyInjection.AddInfrastructureServices — веб-слой менять не нужно.
public class NullVehicleCatalogService : IVehicleCatalogService
{
    public bool IsConfigured => false;

    public Task<VehicleInfo?> DecodeVinAsync(string vin, CancellationToken cancellationToken = default) =>
        Task.FromResult<VehicleInfo?>(null);

    public Task<IReadOnlyList<CatalogPart>> GetPartsByVinAsync(string vin, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<CatalogPart>>(Array.Empty<CatalogPart>());
}
