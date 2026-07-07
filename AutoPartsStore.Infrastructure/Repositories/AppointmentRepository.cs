using AutoPartsStore.Core.Entities;
using AutoPartsStore.Core.Interfaces;
using AutoPartsStore.Data;
using Microsoft.EntityFrameworkCore;

namespace AutoPartsStore.Infrastructure.Repositories;

public class AppointmentRepository : Repository<Appointment>, IAppointmentRepository
{
    public AppointmentRepository(AppDbContext context) : base(context) { }

    public async Task<IReadOnlyList<Appointment>> GetAllOrderedAsync() =>
        await DbSet
            .OrderBy(a => a.PreferredDate)
            .ThenBy(a => a.CreatedAt)
            .ToListAsync();
}
