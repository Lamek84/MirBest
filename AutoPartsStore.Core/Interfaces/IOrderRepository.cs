using AutoPartsStore.Core.Entities;

namespace AutoPartsStore.Core.Interfaces;

public interface IOrderRepository : IRepository<Order>
{
    Task<IReadOnlyList<Order>> GetByUserAsync(string userId);
    Task<Order?> GetByIdWithItemsAsync(int id);
    Task<Order?> GetBySessionIdAsync(string sessionId);
}
