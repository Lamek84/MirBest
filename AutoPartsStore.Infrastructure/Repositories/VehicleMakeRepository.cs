using AutoPartsStore.Core.Entities;
using AutoPartsStore.Core.Interfaces;
using AutoPartsStore.Data;
using Microsoft.EntityFrameworkCore;

namespace AutoPartsStore.Infrastructure.Repositories;

public class VehicleMakeRepository : Repository<VehicleMake>, IVehicleMakeRepository
{
    public VehicleMakeRepository(AppDbContext context) : base(context) { }

    public async Task<IReadOnlyList<VehicleMake>> GetAllOrderedAsync() =>
        await DbSet.OrderBy(m => m.Name).ToListAsync();
}
