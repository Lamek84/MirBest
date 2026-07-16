namespace AutoPartsStore.Infrastructure.Catalog;

// Настройки внешнего каталога — секция "VehicleCatalog" в appsettings.
// Пока провайдер не подключён, поля пустые и используется NullVehicleCatalogService.
// Реальную реализацию (HTTP-клиент к Parts-Catalogs/TecDoc) конфигурируем отсюда.
public class VehicleCatalogSettings
{
    public string Provider { get; set; } = string.Empty;   // напр. "PartsCatalogs"
    public string BaseUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
}
