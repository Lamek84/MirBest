using AutoPartsStore.Core.Entities;

namespace AutoPartsStore.Core.Interfaces;

public interface IVehicleMakeRepository : IRepository<VehicleMake>
{
    Task<IReadOnlyList<VehicleMake>> GetAllOrderedAsync();
}
