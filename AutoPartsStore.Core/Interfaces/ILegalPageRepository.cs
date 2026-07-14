using AutoPartsStore.Core.Entities;

namespace AutoPartsStore.Core.Interfaces;

public interface ILegalPageRepository : IRepository<LegalPage>
{
    Task<LegalPage?> GetByKeyAsync(string key);
}
