using AutoPartsStore.Core.Entities;

namespace AutoPartsStore.Core.Interfaces;

public interface ICartItemRepository : IRepository<CartItem>
{
    Task<IReadOnlyList<CartItem>> GetByUserAsync(string userId);
    Task<CartItem?> GetByUserAndProductAsync(string userId, int productId);
    Task<int> GetItemCountAsync(string userId);
}
