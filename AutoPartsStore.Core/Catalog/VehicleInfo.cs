namespace AutoPartsStore.Core.Catalog;

// Результат декодирования VIN внешним каталогом (Parts-Catalogs, NHTSA и т.п.).
// Нейтральный к провайдеру DTO: реализация IVehicleCatalogService маппит ответ
// своего API в эти поля, остальному коду источник не важен.
public class VehicleInfo
{
    public string Vin { get; set; } = string.Empty;
    public string? Make { get; set; }
    public string? Model { get; set; }
    public int? Year { get; set; }
    public string? Engine { get; set; }
    public string? BodyType { get; set; }
}
