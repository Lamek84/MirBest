using AutoPartsStore.Core.Entities;
using AutoPartsStore.Data.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AutoPartsStore.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<ContactMessage> ContactMessages => Set<ContactMessage>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<VehicleMake> VehicleMakes => Set<VehicleMake>();
    public DbSet<VehicleModel> VehicleModels => Set<VehicleModel>();
    public DbSet<ProductVehicleFitment> ProductVehicleFitments => Set<ProductVehicleFitment>();
    public DbSet<ProductReferenceNumber> ProductReferenceNumbers => Set<ProductReferenceNumber>();
    public DbSet<LegalPage> LegalPages => Set<LegalPage>();
    public DbSet<Partner> Partners => Set<Partner>();
    public DbSet<DetailingPackage> DetailingPackages => Set<DetailingPackage>();
    public DbSet<SeedFlag> SeedFlags => Set<SeedFlag>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
