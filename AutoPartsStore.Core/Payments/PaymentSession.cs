namespace AutoPartsStore.Core.Payments;

// Результат создания сессии оплаты у провайдера (Stripe Checkout Session и т.п.).
public class PaymentSession
{
    public string SessionId { get; set; } = string.Empty;
    public string CheckoutUrl { get; set; } = string.Empty;
}
