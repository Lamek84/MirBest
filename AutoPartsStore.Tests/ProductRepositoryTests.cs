using AutoPartsStore.Core.Entities;
using AutoPartsStore.Data;
using AutoPartsStore.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AutoPartsStore.Tests;

public class ProductRepositoryTests
{
    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task AddAsync_Then_GetAllAsync_ReturnsProduct()
    {
        await using var context = CreateContext();
        var repository = new ProductRepository(context);

        var category = new Category { Name = "Тормозная система" };
        context.Categories.Add(category);
        await context.SaveChangesAsync();

        var product = new Product
        {
            Name = "Тормозные колодки",
            PartNumber = "BP-1001",
            Price = 1500m,
            StockQuantity = 10,
            CategoryId = category.Id
        };

        await repository.AddAsync(product);
        await repository.SaveChangesAsync();

        var all = await repository.GetAllAsync();

        Assert.Single(all);
        Assert.Equal("Тормозные колодки", all[0].Name);
    }
}
