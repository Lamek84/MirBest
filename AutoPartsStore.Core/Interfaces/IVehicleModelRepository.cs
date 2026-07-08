using AutoPartsStore.Core.Entities;

namespace AutoPartsStore.Core.Interfaces;

public interface IVehicleModelRepository : IRepository<VehicleModel>
{
    Task<IReadOnlyList<VehicleModel>> GetAllWithMakeAsync();
    Task<IReadOnlyList<VehicleModel>> GetByMakeAsync(int makeId);
}
