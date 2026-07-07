using AutoPartsStore.Core.Entities;
using AutoPartsStore.Core.Interfaces;
using AutoPartsStore.Core.Payments;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;

namespace AutoPartsStore.Infrastructure.Payments;

public class StripePaymentService : IPaymentService
{
    private readonly StripeSettings _settings;

    public StripePaymentService(IOptions<StripeSettings> options)
    {
        _settings = options.Value;
        StripeConfiguration.ApiKey = _settings.SecretKey;
    }

    public async Task<PaymentSession> CreateCheckoutSessionAsync(Order order, string successUrl, string cancelUrl)
    {
        if (string.IsNullOrWhiteSpace(_settings.SecretKey))
        {
            // Stripe не настроен (Stripe:SecretKey пуст) — явная ошибка вместо
            // непонятного сбоя при обращении к API с пустым ключом.
            throw new InvalidOperationException("Stripe ist nicht konfiguriert (Stripe:SecretKey fehlt in appsettings.json / User Secrets).");
        }

        var lineItems = order.Items.Select(item => new SessionLineItemOptions
        {
            Quantity = item.Quantity,
            PriceData = new SessionLineItemPriceDataOptions
            {
                Currency = _settings.Currency,
                UnitAmount = (long)Math.Round(item.UnitPrice * 100, MidpointRounding.AwayFromZero),
                ProductData = new SessionLineItemPriceDataProductDataOptions
                {
                    Name = item.ProductName
                }
            }
        }).ToList();

        // Доставка — отдельной строкой в чеке, чтобы сумма в Stripe совпадала
        // с TotalAmount заказа (товары + DeliveryCost). При самовывозе (0 €)
        // строку не добавляем — Stripe не любит нулевые line items.
        if (order.DeliveryCost > 0)
        {
            lineItems.Add(new SessionLineItemOptions
            {
                Quantity = 1,
                PriceData = new SessionLineItemPriceDataOptions
                {
                    Currency = _settings.Currency,
                    UnitAmount = (long)Math.Round(order.DeliveryCost * 100, MidpointRounding.AwayFromZero),
                    ProductData = new SessionLineItemPriceDataProductDataOptions
                    {
                        Name = "Versand: " + (order.DeliveryLabel ?? order.DeliveryMethod)
                    }
                }
            });
        }

        var options = new SessionCreateOptions
        {
            Mode = "payment",
            SuccessUrl = successUrl,
            CancelUrl = cancelUrl,
            ClientReferenceId = order.Id.ToString(),
            Metadata = new Dictionary<string, string> { { "orderId", order.Id.ToString() } },
            LineItems = lineItems
        };

        var service = new SessionService();
        var session = await service.CreateAsync(options);

        return new PaymentSession { SessionId = session.Id, CheckoutUrl = session.Url };
    }

    public async Task<bool> IsSessionPaidAsync(string sessionId)
    {
        var service = new SessionService();
        var session = await service.GetAsync(sessionId);
        return session.PaymentStatus == "paid";
    }

    public PaymentWebhookResult? ParseWebhookEvent(string requestBody, string signatureHeader)
    {
        // Бросает исключение, если подпись не совпадает — это защищает
        // Webhook-эндпоинт от поддельных запросов (см. PaymentsController.Webhook).
        var stripeEvent = EventUtility.ConstructEvent(requestBody, signatureHeader, _settings.WebhookSecret);

        if ((stripeEvent.Type == "checkout.session.completed" || stripeEvent.Type == "checkout.session.async_payment_succeeded")
            && stripeEvent.Data.Object is Session session)
        {
            return new PaymentWebhookResult
            {
                SessionId = session.Id,
                IsPaid = session.PaymentStatus == "paid"
            };
        }

        return null;
    }
}
