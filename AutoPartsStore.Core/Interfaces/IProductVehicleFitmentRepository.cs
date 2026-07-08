using AutoPartsStore.Core.Entities;

namespace AutoPartsStore.Core.Interfaces;

public interface IProductVehicleFitmentRepository : IRepository<ProductVehicleFitment>
{
    Task<IReadOnlyList<ProductVehicleFitment>> GetByProductAsync(int productId);

    // Задел на будущий поиск "подобрать деталь под мою машину":
    // все товары, совместимые с конкретной моделью в заданном году.
    Task<IReadOnlyList<ProductVehicleFitment>> GetByModelAndYearAsync(int vehicleModelId, int? year);
}
