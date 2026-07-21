using AutoPartsStore.Core.Entities;
using AutoPartsStore.Core.Interfaces;
using AutoPartsStore.Data;
using Microsoft.EntityFrameworkCore;

namespace AutoPartsStore.Infrastructure.Repositories;

public class OrderRepository : Repository<Order>, IOrderRepository
{
    public OrderRepository(AppDbContext context) : base(context) { }

    public async Task<IReadOnlyList<Order>> GetByUserAsync(string userId) =>
        await DbSet
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();

    public async Task<(IReadOnlyList<Order> Orders, int TotalCount)> SearchAllAsync(
        string? status,
        DateTime? fromDate,
        DateTime? toDate,
        int page,
        int pageSize)
    {
        var query = DbSet.AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(o => o.Status == status);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(o => o.OrderDate >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            // Bis-Datum ist inklusive — auf den Tag danach hochrunden, damit
            // Bestellungen vom Bis-Tag selbst nicht rausfallen.
            var exclusiveEnd = toDate.Value.Date.AddDays(1);
            query = query.Where(o => o.OrderDate < exclusiveEnd);
        }

        var totalCount = await query.CountAsync();

        var orders = await query
            .Include(o => o.Items)
            .OrderByDescending(o => o.OrderDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (orders, totalCount);
    }

    public async Task<Order?> GetByIdWithItemsAsync(int id) =>
        await DbSet
            .Include(o => o.Items)
            .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(o => o.Id == id);

    public async Task<Order?> GetBySessionIdAsync(string sessionId) =>
        await DbSet
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.PaymentSessionId == sessionId);

    public async Task<bool> TryMarkPaidAsync(int orderId)
    {
        // Единый атомарный UPDATE: строка блокируется на время апдейта, поэтому
        // из двух конкурентных вызовов (Success + вебхук) статус сменит ровно один,
        // а второй получит 0 обновлённых строк. Значение статуса дублирует
        // OrderStatus.Paid (ExecuteUpdate транслируется в SQL и не может вызвать
        // C#-константу напрямую).
        var affected = await DbSet
            .Where(o => o.Id == orderId && o.Status != OrderStatus.Paid)
            .ExecuteUpdateAsync(s => s.SetProperty(o => o.Status, OrderStatus.Paid));

        return affected == 1;
    }
}
