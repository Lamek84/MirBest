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
    private readonly IPaymentService _paymentService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(
        IOrderRepository orderRepository,
        ICartItemRepository cartItemRepository,
        IPaymentService paymentService,
        UserManager<ApplicationUser> userManager,
        ILogger<PaymentsController> logger)
    {
        _orderRepository = orderRepository;
        _cartItemRepository = cartItemRepository;
        _paymentService = paymentService;
        _userManager = userManager;
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
        order.Status = OrderStatus.Paid;
        _orderRepository.Update(order);
        await _orderRepository.SaveChangesAsync();

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
    }
}
