namespace AutoPartsStore.Core.Entities;

public static class AppointmentTimeSlots
{
    public const string Morning = "Vormittag (8:00–12:00)";
    public const string Afternoon = "Nachmittag (12:00–16:00)";

    public static readonly IReadOnlyList<string> All = new[] { Morning, Afternoon };
}
