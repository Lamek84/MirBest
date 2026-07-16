using System.Security.Claims;
using AutoPartsStore.Core.Entities;
using AutoPartsStore.Core.Interfaces;
using AutoPartsStore.Core.Payments;
using AutoPartsStore.Data;
using AutoPartsStore.Data.Identity;
using AutoPartsStore.Infrastructure.Repositories;
using AutoPartsStore.Web.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace AutoPartsStore.Tests;

// Тесты покрывают два самых денежно-критичных пути приложения:
// 1) Checkout не должен создавать заказ, если товара не хватает на складе.
// 2) Подтверждение оплаты (webhook) должно атомарно (в транзакции) списывать
//    склад, начислять баллы и очищать корзину — ровно один раз.
public class CheckoutAndPaymentTests
{
    // Для теста Checkout транзакции не нужны — обычный InMemory-провайдер достаточен.
    private static AppDbContext CreateInMemoryContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    // PaymentsController оборачивает MarkOrderPaidAsync в реальную транзакцию
    // (см. IUnitOfWork/UnitOfWork) — EF InMemory-провайдер транзакции не
    // поддерживает и бросит исключение. SQLite in-memory ("DataSource=:memory:")
    // ведёт себя как настоящая реляционная БД (транзакции, автоинкремент), но
    // требует держать соединение открытым на всё время жизни контекста.
    private sealed class SqliteTestDatabase : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;
        public AppDbContext Context { get; }

        public SqliteTestDatabase()
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(_connection)
                .Options;

            Context = new AppDbContext(options);
            Context.Database.EnsureCreated();
        }

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }

    // UserManager<T> не имеет публичного конструктора без зависимостей,
    // но все нужные нам методы (GetUserId/FindByIdAsync/UpdateAsync) виртуальные —
    // мокаем их напрямую, не трогая реальный стор.
    private static Mock<UserManager<ApplicationUser>> CreateUserManagerMock()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        return new Mock<UserManager<ApplicationUser>>(store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
    }

    private static ClaimsPrincipal CreateAuthenticatedUser(string userId) =>
        new(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, userId) }, "TestAuth"));

    [Fact]
    public async Task Checkout_InsufficientStock_DoesNotCreateOrder_AndShowsMessage()
    {
        await using var context = CreateInMemoryContext();

        var category = new Category { Name = "Motor" };
        context.Categories.Add(category);
        await context.SaveChangesAsync();

        var product = new Product { Name = "Ölfilter", Price = 10m, StockQuantity = 1, CategoryId = category.Id };
        context.Products.Add(product);
        await context.SaveChangesAsync();

        const string userId = "user1";
        context.CartItems.Add(new CartItem { UserId = userId, ProductId = product.Id, Quantity = 5 });
        await context.SaveChangesAsync();

        var productRepo = new ProductRepository(context);
        var cartRepo = new CartItemRepository(context);
        var orderRepo = new OrderRepository(context);

        var userManagerMock = CreateUserManagerMock();
        userManagerMock.Setup(m => m.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns(userId);
        userManagerMock.Setup(m => m.FindByIdAsync(userId))
            .ReturnsAsync(new ApplicationUser { Id = userId, BonusPoints = 0 });

        var paymentServiceMock = new Mock<IPaymentService>();

        var controller = new CartController(cartRepo, productRepo, orderRepo, paymentServiceMock.Object, userManagerMock.Object);

        var httpContext = new DefaultHttpContext { User = CreateAuthenticatedUser(userId) };
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        controller.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());

        var result = await controller.Checkout("Selbstabholung");

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.False(await context.Orders.AnyAsync());
        Assert.Contains("Nicht genug Bestand", (string)controller.TempData["CartMessage"]!);

        // Платёжный сервис не должен был даже вызываться — заказ не создавался.
        paymentServiceMock.Verify(
            p => p.CreateCheckoutSessionAsync(It.IsAny<Order>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task Webhook_MarksOrderPaid_DecrementsStock_AwardsPoints_AndClearsCart()
    {
        await using var db = new SqliteTestDatabase();
        var context = db.Context;

        var category = new Category { Name = "Motor" };
        context.Categories.Add(category);
        await context.SaveChangesAsync();

        var product = new Product { Name = "Ölfilter", Price = 10m, StockQuantity = 50, CategoryId = category.Id };
        context.Products.Add(product);
        await context.SaveChangesAsync();

        const string userId = "user1";
        // Bonuspunkte gibt es erst ab einem Bestellwert über 300 € (siehe
        // PaymentsController.MarkOrderPaidAsync) — Summe hier bewusst darüber.
        var order = new Order
        {
            UserId = userId,
            Status = OrderStatus.PendingPayment,
            TotalAmount = 350m,
            PaymentProvider = "Stripe",
            PaymentSessionId = "sess_123",
            Items = new List<OrderItem>
            {
                new() { ProductId = product.Id, ProductName = product.Name, UnitPrice = 10m, Quantity = 35 }
            }
        };
        context.Orders.Add(order);

        context.CartItems.Add(new CartItem { UserId = userId, ProductId = product.Id, Quantity = 35 });
        await context.SaveChangesAsync();

        var productRepo = new ProductRepository(context);
        var cartRepo = new CartItemRepository(context);
        var orderRepo = new OrderRepository(context);
        var unitOfWork = new UnitOfWork(context);

        var user = new ApplicationUser { Id = userId, BonusPoints = 0 };
        var userManagerMock = CreateUserManagerMock();
        userManagerMock.Setup(m => m.FindByIdAsync(userId)).ReturnsAsync(user);
        userManagerMock.Setup(m => m.UpdateAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(IdentityResult.Success);

        var paymentServiceMock = new Mock<IPaymentService>();
        paymentServiceMock
            .Setup(p => p.ParseWebhookEvent(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(new PaymentWebhookResult { SessionId = "sess_123", IsPaid = true });

        var controller = new PaymentsController(
            orderRepo, cartRepo, productRepo, paymentServiceMock.Object, userManagerMock.Object, unitOfWork,
            NullLogger<PaymentsController>.Instance);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Body = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("{}"));
        httpContext.Request.Headers["Stripe-Signature"] = "test-signature";
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

        var result = await controller.Webhook();

        Assert.IsType<OkResult>(result);

        var updatedOrder = await context.Orders.Include(o => o.Items).FirstAsync(o => o.Id == order.Id);
        Assert.Equal(OrderStatus.Paid, updatedOrder.Status);
        Assert.Equal(350, updatedOrder.PointsEarned);

        var updatedProduct = await context.Products.FirstAsync(p => p.Id == product.Id);
        Assert.Equal(15, updatedProduct.StockQuantity);

        Assert.Equal(350, user.BonusPoints);
        Assert.False(await context.CartItems.AnyAsync(c => c.UserId == userId));
    }

    [Fact]
    public async Task Webhook_CalledTwiceForSameSession_DoesNotDoubleDecrementStockOrPoints()
    {
        await using var db = new SqliteTestDatabase();
        var context = db.Context;

        var category = new Category { Name = "Motor" };
        context.Categories.Add(category);
        await context.SaveChangesAsync();

        var product = new Product { Name = "Ölfilter", Price = 10m, StockQuantity = 10, CategoryId = category.Id };
        context.Products.Add(product);
        await context.SaveChangesAsync();

        const string userId = "user1";
        // Auch hier: Bestellwert bewusst über der 300-€-Schwelle, damit Punkte
        // überhaupt anfallen und der Idempotenz-Test aussagekräftig bleibt.
        var order = new Order
        {
            UserId = userId,
            Status = OrderStatus.PendingPayment,
            TotalAmount = 350m,
            PaymentSessionId = "sess_456",
            Items = new List<OrderItem>
            {
                new() { ProductId = product.Id, ProductName = product.Name, UnitPrice = 10m, Quantity = 2 }
            }
        };
        context.Orders.Add(order);
        await context.SaveChangesAsync();

        var productRepo = new ProductRepository(context);
        var cartRepo = new CartItemRepository(context);
        var orderRepo = new OrderRepository(context);
        var unitOfWork = new UnitOfWork(context);

        var user = new ApplicationUser { Id = userId, BonusPoints = 0 };
        var userManagerMock = CreateUserManagerMock();
        userManagerMock.Setup(m => m.FindByIdAsync(userId)).ReturnsAsync(user);
        userManagerMock.Setup(m => m.UpdateAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(IdentityResult.Success);

        var paymentServiceMock = new Mock<IPaymentService>();
        paymentServiceMock
            .Setup(p => p.ParseWebhookEvent(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(new PaymentWebhookResult { SessionId = "sess_456", IsPaid = true });

        var controller = new PaymentsController(
            orderRepo, cartRepo, productRepo, paymentServiceMock.Object, userManagerMock.Object, unitOfWork,
            NullLogger<PaymentsController>.Instance);

        async Task<IActionResult> CallWebhookAsync()
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Body = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("{}"));
            httpContext.Request.Headers["Stripe-Signature"] = "test-signature";
            controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
            return await controller.Webhook();
        }

        // Stripe при отсутствии 2xx повторяет доставку webhook — эндпоинт должен
        // быть идемпотентным (проверяем ветку "order.Status != OrderStatus.Paid").
        await CallWebhookAsync();
        await CallWebhookAsync();

        var updatedProduct = await context.Products.FirstAsync(p => p.Id == product.Id);
        Assert.Equal(8, updatedProduct.StockQuantity); // списано один раз, не дважды

        Assert.Equal(350, user.BonusPoints); // начислено один раз
    }

    [Fact]
    public async Task Webhook_OrderBelowPointsThreshold_DoesNotAwardPoints()
    {
        // Bonuspunkte gibt es erst ab einem Bestellwert über 300 € — hier
        // bewusst darunter, um genau diese Regel abzusichern.
        await using var db = new SqliteTestDatabase();
        var context = db.Context;

        var category = new Category { Name = "Motor" };
        context.Categories.Add(category);
        await context.SaveChangesAsync();

        var product = new Product { Name = "Ölfilter", Price = 10m, StockQuantity = 10, CategoryId = category.Id };
        context.Products.Add(product);
        await context.SaveChangesAsync();

        const string userId = "user1";
        var order = new Order
        {
            UserId = userId,
            Status = OrderStatus.PendingPayment,
            TotalAmount = 250m,
            PaymentProvider = "Stripe",
            PaymentSessionId = "sess_789",
            Items = new List<OrderItem>
            {
                new() { ProductId = product.Id, ProductName = product.Name, UnitPrice = 10m, Quantity = 2 }
            }
        };
        context.Orders.Add(order);
        await context.SaveChangesAsync();

        var productRepo = new ProductRepository(context);
        var cartRepo = new CartItemRepository(context);
        var orderRepo = new OrderRepository(context);
        var unitOfWork = new UnitOfWork(context);

        var user = new ApplicationUser { Id = userId, BonusPoints = 0 };
        var userManagerMock = CreateUserManagerMock();
        userManagerMock.Setup(m => m.FindByIdAsync(userId)).ReturnsAsync(user);
        userManagerMock.Setup(m => m.UpdateAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(IdentityResult.Success);

        var paymentServiceMock = new Mock<IPaymentService>();
        paymentServiceMock
            .Setup(p => p.ParseWebhookEvent(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(new PaymentWebhookResult { SessionId = "sess_789", IsPaid = true });

        var controller = new PaymentsController(
            orderRepo, cartRepo, productRepo, paymentServiceMock.Object, userManagerMock.Object, unitOfWork,
            NullLogger<PaymentsController>.Instance);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Body = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("{}"));
        httpContext.Request.Headers["Stripe-Signature"] = "test-signature";
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

        var result = await controller.Webhook();

        Assert.IsType<OkResult>(result);

        var updatedOrder = await context.Orders.FirstAsync(o => o.Id == order.Id);
        Assert.Equal(OrderStatus.Paid, updatedOrder.Status);
        Assert.Equal(0, updatedOrder.PointsEarned);
        Assert.Equal(0, user.BonusPoints);

        // Lagerbestand wird trotzdem regulär reduziert — nur die Punktevergabe entfällt.
        var updatedProduct = await context.Products.FirstAsync(p => p.Id == product.Id);
        Assert.Equal(8, updatedProduct.StockQuantity);
    }
}
