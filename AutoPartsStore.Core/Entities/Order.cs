namespace AutoPartsStore.Core.Entities;

public class Order : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;

    // Простой статус строкой — см. константы в OrderStatus.
    public string Status { get; set; } = OrderStatus.PendingPayment;

    public decimal TotalAmount { get; set; }

    // Кто и по какой сессии принял оплату — нужно, чтобы найти заказ
    // по webhook-уведомлению от платёжного провайдера (см. PaymentsController).
    public string? PaymentProvider { get; set; }
    public string? PaymentSessionId { get; set; }

    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}
