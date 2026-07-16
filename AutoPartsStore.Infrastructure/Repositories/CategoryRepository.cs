using AutoPartsStore.Core.Entities;
using AutoPartsStore.Core.Interfaces;
using AutoPartsStore.Data;
using Microsoft.EntityFrameworkCore;

namespace AutoPartsStore.Infrastructure.Repositories;

public class CategoryRepository : Repository<Category>, ICategoryRepository
{
    public CategoryRepository(AppDbContext context) : base(context) { }

    public async Task<IReadOnlyList<Category>> GetTopLevelAsync()
        => await DbSet.Where(c => c.ParentCategoryId == null).OrderBy(c => c.Name).ToListAsync();

    public async Task<IReadOnlyList<Category>> GetSubcategoriesAsync(int parentCategoryId)
        => await DbSet.Where(c => c.ParentCategoryId == parentCategoryId).OrderBy(c => c.Name).ToListAsync();
}
