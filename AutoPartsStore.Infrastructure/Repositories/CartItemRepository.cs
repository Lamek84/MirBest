using AutoPartsStore.Core.Entities;
using AutoPartsStore.Core.Interfaces;
using AutoPartsStore.Data;
using Microsoft.EntityFrameworkCore;

namespace AutoPartsStore.Infrastructure.Repositories;

public class CartItemRepository : Repository<CartItem>, ICartItemRepository
{
    public CartItemRepository(AppDbContext context) : base(context) { }

    public async Task<IReadOnlyList<CartItem>> GetByUserAsync(string userId) =>
        await DbSet
            .Where(c => c.UserId == userId)
            .Include(c => c.Product)
            .ToListAsync();

    public async Task<CartItem?> GetByUserAndProductAsync(string userId, int productId) =>
        await DbSet.FirstOrDefaultAsync(c => c.UserId == userId && c.ProductId == productId);

    public async Task<int> GetItemCountAsync(string userId) =>
        await DbSet.Where(c => c.UserId == userId).SumAsync(c => c.Quantity);
}
