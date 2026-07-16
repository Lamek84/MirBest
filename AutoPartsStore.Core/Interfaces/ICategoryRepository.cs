using AutoPartsStore.Core.Entities;

namespace AutoPartsStore.Core.Interfaces;

public interface ICategoryRepository : IRepository<Category>
{
    Task<IReadOnlyList<Category>> GetTopLevelAsync();

    Task<IReadOnlyList<Category>> GetSubcategoriesAsync(int parentCategoryId);
}
