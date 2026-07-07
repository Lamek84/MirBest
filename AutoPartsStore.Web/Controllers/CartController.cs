using AutoPartsStore.Core.Entities;
using AutoPartsStore.Core.Interfaces;
using AutoPartsStore.Data.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AutoPartsStore.Web.Controllers;

// Без [Authorize] на уровне класса: гости тоже могут смотреть корзину,
// добавлять и убирать товары. Авторизация нужна только в Checkout —
// именно там мы просим войти/зарегистрироваться (см. GetCartOwnerId/Checkout).
public class CartController : Controller
{
    private readonly ICartItemRepository _cartItemRepository;
    private readonly IProductRepository _productRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly IPaymentService _paymentService;
    private readonly UserManager<ApplicationUser> _userManager;

    public CartController(
        ICartItemRepository cartItemRepository,
        IProductRepository productRepository,
        IOrderRepository orderRepository,
        IPaymentService paymentService,
        UserManager<ApplicationUser> userManager)
    {
        _cartItemRepository = cartItemRepository;
        _productRepository = productRepository;
        _orderRepository = orderRepository;
        _paymentService = paymentService;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var ownerId = GetCartOwnerId();
        var items = await _cartItemRepository.GetByUserAsync(ownerId);
        return View(items);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(int productId, int quantity = 1)
    {
        if (quantity < 1)
        {
            quantity = 1;
        }

        var product = await _productRepository.GetByIdAsync(productId);
        if (product is null)
        {
            return NotFound();
        }

        var ownerId = GetCartOwnerId();
        var existing = await _cartItemRepository.GetByUserAndProductAsync(ownerId, productId);

        if (existing is null)
        {
            await _cartItemRepository.AddAsync(new CartItem
            {
                UserId = ownerId,
                ProductId = productId,
                Quantity = quantity
            });
        }
        else
        {
            existing.Quantity += quantity;
            _cartItemRepository.Update(existing);
        }

        await _cartItemRepository.SaveChangesAsync();

        TempData["CartMessage"] = $"\"{product.Name}\" wurde in den Warenkorb gelegt.";
        return RedirectToAction("Index", "Products", new { categoryId = product.CategoryId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateQuantity(int cartItemId, int quantity)
    {
        var ownerId = GetCartOwnerId();
        var item = await _cartItemRepository.GetByIdAsync(cartItemId);
        if (item is null || item.UserId != ownerId)
        {
            return NotFound();
        }

        if (quantity < 1)
        {
            _cartItemRepository.Remove(item);
        }
        else
        {
            item.Quantity = quantity;
            _cartItemRepository.Update(item);
        }

        await _cartItemRepository.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(int cartItemId)
    {
        var ownerId = GetCartOwnerId();
        var item = await _cartItemRepository.GetByIdAsync(cartItemId);
        if (item is not null && item.UserId == ownerId)
        {
            _cartItemRepository.Remove(item);
            await _cartItemRepository.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Checkout()
    {
        // Заказывать может только вошедший пользователь — гостя отправляем
        // на вход/регистрацию и возвращаем обратно в корзину (не на сам
        // Checkout, т.к. это POST-эндпоинт и его нельзя открыть через редирект).
        if (!User.Identity!.IsAuthenticated)
        {
            TempData["CartMessage"] = "Bitte melde dich an oder registriere dich, um die Bestellung abzuschließen.";
            return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("Index", "Cart") });
        }

        var userId = _userManager.GetUserId(User)!;
        var items = await _cartItemRepository.GetByUserAsync(userId);

        if (!items.Any())
        {
            return RedirectToAction(nameof(Index));
        }

        var order = new Order
        {
            UserId = userId,
            OrderDate = DateTime.UtcNow,
            Status = OrderStatus.PendingPayment,
            TotalAmount = items.Sum(i => i.Quantity * i.Product!.Price)
        };

        foreach (var item in items)
        {
            order.Items.Add(new OrderItem
            {
                ProductId = item.ProductId,
                ProductName = item.Product!.Name,
                UnitPrice = item.Product.Price,
                Quantity = item.Quantity
            });
        }

        await _orderRepository.AddAsync(order);
        await _orderRepository.SaveChangesAsync();

        // Корзину пока не трогаем — очистим только после подтверждённой оплаты
        // (см. PaymentsController.MarkOrderPaidAsync), чтобы не терять товары
        // при отмене оплаты или ошибке на стороне Stripe.

        var successUrl = Url.Action("Success", "Payments", new { orderId = order.Id }, Request.Scheme)
            + "&session_id={CHECKOUT_SESSION_ID}";
        var cancelUrl = Url.Action("Cancel", "Payments", new { orderId = order.Id }, Request.Scheme)!;

        try
        {
            var session = await _paymentService.CreateCheckoutSessionAsync(order, successUrl!, cancelUrl);

            order.PaymentProvider = "Stripe";
            order.PaymentSessionId = session.SessionId;
            _orderRepository.Update(order);
            await _orderRepository.SaveChangesAsync();

            return Redirect(session.CheckoutUrl);
        }
        catch (InvalidOperationException)
        {
            // Stripe не настроен (пустой SecretKey) — не оставляем покупателя
            // с "зависшим" заказом, откатываем статус и объясняем, что не так.
            order.Status = OrderStatus.PaymentFailed;
            _orderRepository.Update(order);
            await _orderRepository.SaveChangesAsync();

            TempData["CartMessage"] = "Online-Zahlung ist derzeit nicht verfügbar. Bitte versuche es später erneut.";
            return RedirectToAction(nameof(Index));
        }
    }

    // Возвращает Id владельца корзины: настоящий UserId для вошедших,
    // либо анонимный guest-id из cookie (создаём при первом обращении).
    private string GetCartOwnerId()
    {
        if (User.Identity!.IsAuthenticated)
        {
            return _userManager.GetUserId(User)!;
        }

        if (Request.Cookies.TryGetValue(CartConstants.GuestCookieName, out var guestId) && !string.IsNullOrEmpty(guestId))
        {
            return guestId;
        }

        guestId = "guest_" + Guid.NewGuid().ToString("N");
        Response.Cookies.Append(CartConstants.GuestCookieName, guestId, new CookieOptions
        {
            HttpOnly = true,
            IsEssential = true,
            Expires = DateTimeOffset.UtcNow.AddDays(30)
        });

        return guestId;
    }
}
