using AutoPartsStore.Core.Entities;
using AutoPartsStore.Core.Interfaces;
using AutoPartsStore.Data;
using Microsoft.EntityFrameworkCore;

namespace AutoPartsStore.Infrastructure.Repositories;

public class ProductVehicleFitmentRepository : Repository<ProductVehicleFitment>, IProductVehicleFitmentRepository
{
    public ProductVehicleFitmentRepository(AppDbContext context) : base(context) { }

    public async Task<IReadOnlyList<ProductVehicleFitment>> GetByProductAsync(int productId) =>
        await DbSet
            .Where(f => f.ProductId == productId)
            .Include(f => f.VehicleModel)
            .ThenInclude(m => m!.VehicleMake)
            .OrderBy(f => f.VehicleModel!.VehicleMake!.Name)
            .ThenBy(f => f.VehicleModel!.Name)
            .ToListAsync();

    public async Task<IReadOnlyList<ProductVehicleFitment>> GetByModelAndYearAsync(int vehicleModelId, int? year)
    {
        var query = DbSet
            .Where(f => f.VehicleModelId == vehicleModelId)
            .Include(f => f.Product)
            .AsQueryable();

        if (year.HasValue)
        {
            query = query.Where(f =>
                (f.YearFrom == null || f.YearFrom <= year.Value) &&
                (f.YearTo == null || f.YearTo >= year.Value));
        }

        return await query.ToListAsync();
    }
}
