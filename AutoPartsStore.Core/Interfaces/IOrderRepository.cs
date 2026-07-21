using AutoPartsStore.Core.Entities;

namespace AutoPartsStore.Core.Interfaces;

public interface IOrderRepository : IRepository<Order>
{
    Task<IReadOnlyList<Order>> GetByUserAsync(string userId);

    // Für die Admin-Übersicht "Alle Bestellungen" — gefiltert, neueste zuerst,
    // seitenweise (sonst wird die Liste mit der Zeit unbrauchbar lang).
    Task<(IReadOnlyList<Order> Orders, int TotalCount)> SearchAllAsync(
        string? status,
        DateTime? fromDate,
        DateTime? toDate,
        int page,
        int pageSize);

    Task<Order?> GetByIdWithItemsAsync(int id);
    Task<Order?> GetBySessionIdAsync(string sessionId);

    // Атомарно переводит заказ в "оплачен" ровно один раз. Возвращает true только
    // тому вызову, который реально сменил статус (единый UPDATE ... WHERE Status
    // != Paid). Защита от гонки между Success-возвратом и вебхуком: второй вызов
    // получит false и не повторит списание склада/начисление баллов.
    Task<bool> TryMarkPaidAsync(int orderId);
}
