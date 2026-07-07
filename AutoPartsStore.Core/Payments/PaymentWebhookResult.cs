namespace AutoPartsStore.Core.Payments;

// Итог разбора webhook-события от провайдера — контроллеру не нужно
// знать детали конкретного API (Stripe и т.п.), только эти два поля.
public class PaymentWebhookResult
{
    public string SessionId { get; set; } = string.Empty;
    public bool IsPaid { get; set; }
}
