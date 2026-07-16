using AutoPartsStore.Core.Entities;
using AutoPartsStore.Core.Interfaces;
using AutoPartsStore.Core.Payments;
using AutoPartsStore.Data.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AutoPartsStore.Web.Controllers;

// Success/Cancel — куда возвращается покупатель из Stripe Checkout.
// Webhook — серверный эндпоинт, который дёргает сам Stripe (без cookie
// пользователя), поэтому он единственный тут без [Authorize].
public class PaymentsController : Controller
{
    private readonly IOrderRepository _orderRepository;
    private readonly ICartItemRepository _cartItemRepository;
    private readonly IProductRepository _productRepository;
    private readonly IPaymentService _paymentService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(
        IOrderRepository orderRepository,
        ICartItemRepository cartItemRepository,
        IProductRepository productRepository,
        IPaymentService paymentService,
        UserManager<ApplicationUser> userManager,
        IUnitOfWork unitOfWork,
        ILogger<PaymentsController> logger)
    {
        _orderRepository = orderRepository;
        _cartItemRepository = cartItemRepository;
        _productRepository = productRepository;
        _paymentService = paymentService;
        _userManager = userManager;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    [Authorize]
    public async Task<IActionResult> Success(int orderId, string session_id)
    {
        var order = await _orderRepository.GetByIdWithItemsAsync(orderId);
        if (order is null || order.UserId != _userManager.GetUserId(User))
        {
            return NotFound();
        }

        // Webhook — основной источник истины и сработает независимо от того,
        // вернулся ли покупатель на эту страницу. Но подстрахуемся: если
        // webhook почему-то ещё не дошёл, проверим статус сессии прямо тут.
        if (order.Status != OrderStatus.Paid && !string.IsNullOrEmpty(session_id))
        {
            try
            {
                if (await _paymentService.IsSessionPaidAsync(session_id))
                {
                    await MarkOrderPaidAsync(order);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Konnte Zahlungsstatus für Session {SessionId} nicht prüfen.", session_id);
            }
        }

        TempData["OrderSuccess"] = true;
        return RedirectToAction("Details", "Orders", new { id = order.Id });
    }

    [Authorize]
    public async Task<IActionResult> Cancel(int orderId)
    {
        var order = await _orderRepository.GetByIdAsync(orderId);
        if (order is not null && order.UserId == _userManager.GetUserId(User) && order.Status == OrderStatus.PendingPayment)
        {
            order.Status = OrderStatus.Cancelled;
            _orderRepository.Update(order);
            await _orderRepository.SaveChangesAsync();

            // Bezahlung kam nicht zustande — eingelöste Bonuspunkte zurückerstatten.
            if (order.PointsRedeemed > 0)
            {
                var user = await _userManager.FindByIdAsync(order.UserId);
                if (user is not null)
                {
                    user.BonusPoints += order.PointsRedeemed;
                    await _userManager.UpdateAsync(user);
                }
            }
        }

        TempData["CartMessage"] = "Die Zahlung wurde abgebrochen. Deine Artikel sind noch im Warenkorb.";
        return RedirectToAction("Index", "Cart");
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Webhook()
    {
        using var reader = new StreamReader(Request.Body);
        var json = await reader.ReadToEndAsync();
        var signature = Request.Headers["Stripe-Signature"].ToString();

        PaymentWebhookResult? result;
        try
        {
            result = _paymentService.ParseWebhookEvent(json, signature);
        }
        catch (Exception ex)
        {
            // Неверная подпись/битый payload — не наш запрос, отклоняем.
            _logger.LogWarning(ex, "Stripe-Webhook: Signatur ungültig oder Payload fehlerhaft.");
            return BadRequest();
        }

        if (result is { IsPaid: true })
        {
            var order = await _orderRepository.GetBySessionIdAsync(result.SessionId);
            if (order is not null && order.Status != OrderStatus.Paid)
            {
                await MarkOrderPaidAsync(order);
            }
        }

        return Ok();
    }

    private async Task MarkOrderPaidAsync(Order order)
    {
        // Статус заказа, списание склада, начисление баллов и очистка корзины
        // должны либо примениться все вместе, либо не примениться вовсе —
        // иначе при сбое между шагами получим, например, оплаченный заказ
        // без начисленных баллов. Все репозитории/UserManager здесь работают
        // на одном и том же AppDbContext (он Scoped на HTTP-запрос), поэтому
        // одна транзакция вокруг всех SaveChangesAsync ниже действительно
        // атомарна на стороне БД.
        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            order.Status = OrderStatus.Paid;

            // Bonuspunkte-Programm: 1 Punkt je bezahltem Euro (abgerundet) —
            // aber nur, wenn der Bestellwert über der Mindestschwelle liegt.
            const decimal MinPurchaseAmountForPoints = 300m;
            order.PointsEarned = order.TotalAmount > MinPurchaseAmountForPoints
                ? (int)Math.Floor(order.TotalAmount)
                : 0;
            _orderRepository.Update(order);
            await _orderRepository.SaveChangesAsync();

            // Bestand erst jetzt reduzieren — nur bei bestätigter Zahlung, nicht
            // schon bei der Bestellerstellung (siehe CartController.Checkout).
            // Kann bei Überbuchung (Race zwischen mehreren Käufern) auf 0 fallen,
            // wird aber nie negativ.
            foreach (var item in order.Items)
            {
                var product = await _productRepository.GetByIdAsync(item.ProductId);
                if (product is not null)
                {
                    product.StockQuantity = Math.Max(0, product.StockQuantity - item.Quantity);
                    _productRepository.Update(product);
                }
            }
            await _productRepository.SaveChangesAsync();

            if (order.PointsEarned > 0)
            {
                var user = await _userManager.FindByIdAsync(order.UserId);
                if (user is not null)
                {
                    user.BonusPoints += order.PointsEarned;
                    await _userManager.UpdateAsync(user);
                }
            }

            // Заказ оплачен — теперь можно безопасно очистить корзину покупателя
            // (до этого момента мы её нарочно не трогали, см. CartController.Checkout).
            var cartItems = await _cartItemRepository.GetByUserAsync(order.UserId);
            if (cartItems.Any())
            {
                foreach (var item in cartItems)
                {
                    _cartItemRepository.Remove(item);
                }
                await _cartItemRepository.SaveChangesAsync();
            }
        });
    }
}
