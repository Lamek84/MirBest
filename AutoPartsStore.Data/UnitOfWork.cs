using AutoPartsStore.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AutoPartsStore.Data;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;

    public UnitOfWork(AppDbContext context)
    {
        _context = context;
    }

    public async Task ExecuteInTransactionAsync(Func<Task> action)
    {
        // Провайдеры без поддержки транзакций (например EF InMemory в старых
        // тестах) отдают IsRelational=false здесь и не всегда переживают
        // BeginTransactionAsync — но AppDbContext в проде/реальных тестах
        // всегда реляционный (SQL Server / SQLite), так что просто открываем
        // транзакцию напрямую.
        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            await action();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
