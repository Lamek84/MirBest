namespace AutoPartsStore.Core.Entities;

// Фиксированный список служб доставки — как у ServiceType/TimeSlots для
// Termin. Пока задаётся в коде; если понадобится управлять из админки —
// можно будет вынести в таблицу БД, структура Order уже это переживёт
// (там хранится снимок Code/Label/Price на момент заказа).
public static class DeliveryOptions
{
    public static readonly DeliveryOption Pickup = new()
    {
        Code = "Selbstabholung",
        DisplayName = "Selbstabholung (Hinterm Sielhof 4, 28277 Bremen)",
        Price = 0m
    };

    public static readonly DeliveryOption Dhl = new()
    {
        Code = "DHL",
        DisplayName = "DHL Paket",
        Price = 4.99m
    };

    public static readonly DeliveryOption Hermes = new()
    {
        Code = "Hermes",
        DisplayName = "Hermes Paket",
        Price = 4.49m
    };

    public static readonly DeliveryOption Dpd = new()
    {
        Code = "DPD",
        DisplayName = "DPD Paket",
        Price = 5.49m
    };

    public static readonly IReadOnlyList<DeliveryOption> All = new[] { Pickup, Dhl, Hermes, Dpd };

    public static DeliveryOption? GetByCode(string? code) =>
        string.IsNullOrEmpty(code) ? null : All.FirstOrDefault(o => o.Code == code);
}
