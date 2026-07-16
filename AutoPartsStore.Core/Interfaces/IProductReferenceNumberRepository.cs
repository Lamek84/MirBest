using AutoPartsStore.Core.Entities;

namespace AutoPartsStore.Core.Interfaces;

public interface IProductReferenceNumberRepository : IRepository<ProductReferenceNumber>
{
    // Все OEM/кросс-номера конкретного товара (для страницы редактирования).
    Task<IReadOnlyList<ProductReferenceNumber>> GetByProductAsync(int productId);
}
