namespace AutoPartsStore.Core.Interfaces;

// Минимальная абстракция транзакции — позволяет контроллерам/сервисам
// объединить несколько SaveChangesAsync (в разных репозиториях, но на одном
// AppDbContext за запрос) в одну атомарную операцию, не завязываясь напрямую
// на EF Core (Core-проект остаётся persistence-ignorant).
public interface IUnitOfWork
{
    Task ExecuteInTransactionAsync(Func<Task> action);
}
