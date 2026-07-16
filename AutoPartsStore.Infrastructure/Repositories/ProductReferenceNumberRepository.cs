using AutoPartsStore.Core.Entities;
using AutoPartsStore.Core.Interfaces;
using AutoPartsStore.Data;
using Microsoft.EntityFrameworkCore;

namespace AutoPartsStore.Infrastructure.Repositories;

public class ProductReferenceNumberRepository
    : Repository<ProductReferenceNumber>, IProductReferenceNumberRepository
{
    public ProductReferenceNumberRepository(AppDbContext context) : base(context) { }

    public async Task<IReadOnlyList<ProductReferenceNumber>> GetByProductAsync(int productId) =>
        await DbSet
            .Where(r => r.ProductId == productId)
            .OrderBy(r => r.Type)
            .ThenBy(r => r.Number)
            .ToListAsync();
}
