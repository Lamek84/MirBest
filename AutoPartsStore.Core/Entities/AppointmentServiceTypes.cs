namespace AutoPartsStore.Core.Entities;

// Фиксированный список услуг для выпадающего списка в форме бронирования —
// проще для клиента, чем свободный текст, и удобнее для админа при сортировке заявок.
public static class AppointmentServiceTypes
{
    public const string Inspection = "Hauptuntersuchung (TÜV)";
    public const string Repair = "Reparatur";
    public const string Maintenance = "Wartung / Inspektion";
    public const string Other = "Sonstiges";

    public static readonly IReadOnlyList<string> All = new[] { Inspection, Repair, Maintenance, Other };
}
