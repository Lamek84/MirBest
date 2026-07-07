using AutoPartsStore.Core.Entities;
using AutoPartsStore.Core.Payments;

namespace AutoPartsStore.Core.Interfaces;

// Абстракция платёжного провайдера — контроллеры зависят только от неё,
// конкретная реализация (Stripe и т.п.) живёт в Infrastructure.
public interface IPaymentService
{
    Task<PaymentSession> CreateCheckoutSessionAsync(Order order, string successUrl, string cancelUrl);

    Task<bool> IsSessionPaidAsync(string sessionId);

    // Проверяет подпись запроса и разбирает событие. Возвращает null,
    // если событие не относится к оплате заказа.
    PaymentWebhookResult? ParseWebhookEvent(string requestBody, string signatureHeader);
}
