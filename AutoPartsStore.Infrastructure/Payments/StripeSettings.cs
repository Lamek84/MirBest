namespace AutoPartsStore.Infrastructure.Payments;

// Биндится из секции "Stripe" в appsettings.json (или из user-secrets/
// переменных окружения — так безопаснее для боевых ключей). Пока SecretKey
// пуст, StripePaymentService выбрасывает понятную ошибку вместо попытки
// обратиться к Stripe с пустым ключом.
public class StripeSettings
{
    public string SecretKey { get; set; } = string.Empty;
    public string PublishableKey { get; set; } = string.Empty;
    public string WebhookSecret { get; set; } = string.Empty;
    public string Currency { get; set; } = "eur";
}
