using AutoPartsStore.Core.Entities;
using AutoPartsStore.Core.Interfaces;
using AutoPartsStore.Data;
using Microsoft.EntityFrameworkCore;

namespace AutoPartsStore.Infrastructure.Repositories;

public class VehicleModelRepository : Repository<VehicleModel>, IVehicleModelRepository
{
    public VehicleModelRepository(AppDbContext context) : base(context) { }

    public async Task<IReadOnlyList<VehicleModel>> GetAllWithMakeAsync() =>
        await DbSet
            .Include(m => m.VehicleMake)
            .OrderBy(m => m.VehicleMake!.Name)
            .ThenBy(m => m.Name)
            .ToListAsync();

    public async Task<IReadOnlyList<VehicleModel>> GetByMakeAsync(int makeId) =>
        await DbSet
            .Where(m => m.VehicleMakeId == makeId)
            .OrderBy(m => m.Name)
            .ToListAsync();
}
