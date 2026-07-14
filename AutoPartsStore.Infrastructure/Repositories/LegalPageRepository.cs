using AutoPartsStore.Core.Entities;
using AutoPartsStore.Core.Interfaces;
using AutoPartsStore.Data;
using Microsoft.EntityFrameworkCore;

namespace AutoPartsStore.Infrastructure.Repositories;

public class LegalPageRepository : Repository<LegalPage>, ILegalPageRepository
{
    public LegalPageRepository(AppDbContext context) : base(context) { }

    public async Task<LegalPage?> GetByKeyAsync(string key) =>
        await DbSet.FirstOrDefaultAsync(p => p.Key == key);
}
