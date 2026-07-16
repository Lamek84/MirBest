using AutoPartsStore.Core.Catalog;

namespace AutoPartsStore.Core.Interfaces;

// Единая точка интеграции с внешним каталогом запчастей (Parts-Catalogs, 7zap,
// TecDoc, NHTSA и т.п.). Контроллеры и остальной код зависят только от этого
// интерфейса — чтобы подключить реального провайдера, достаточно добавить его
// реализацию и заменить регистрацию в DI, не трогая веб-слой.
//
// Поток поиска по VIN:
//   VIN --DecodeVinAsync--> VehicleInfo (марка/модель/год)
//   VIN --GetPartsByVinAsync--> список CatalogPart с OEM-номерами
//        --> IProductRepository.SearchByReferenceNumbersAsync --> наши товары.
public interface IVehicleCatalogService
{
    // Настроен ли реальный провайдер. У заглушки — false, чтобы веб-слой мог
    // показать понятное сообщение вместо пустого результата.
    bool IsConfigured { get; }

    Task<VehicleInfo?> DecodeVinAsync(string vin, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CatalogPart>> GetPartsByVinAsync(string vin, CancellationToken cancellationToken = default);
}
