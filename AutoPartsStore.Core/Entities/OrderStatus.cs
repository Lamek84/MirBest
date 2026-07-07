namespace AutoPartsStore.Core.Entities;

// Именованные значения для Order.Status, чтобы не разбрасывать строковые
// литералы по контроллерам. Поле в БД остаётся обычной строкой (см. Order.Status).
public static class OrderStatus
{
    public const string PendingPayment = "Wartet auf Zahlung";
    public const string Paid = "Bezahlt";
    public const string PaymentFailed = "Zahlung fehlgeschlagen";
    public const string Cancelled = "Storniert";
}
