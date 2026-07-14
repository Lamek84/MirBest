using AutoPartsStore.Core.Entities;
using AutoPartsStore.Core.Interfaces;
using AutoPartsStore.Data;

namespace AutoPartsStore.Infrastructure.Repositories;

public class DetailingPackageRepository : Repository<DetailingPackage>, IDetailingPackageRepository
{
    public DetailingPackageRepository(AppDbContext context) : base(context) { }
}
