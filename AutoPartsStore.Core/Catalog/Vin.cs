namespace AutoPartsStore.Core.Catalog;

// Валидация и нормализация VIN. Держим в Core, чтобы правила были едины
// для веб-формы и любого будущего провайдера каталога.
public static class Vin
{
    public const int Length = 17;

    // Приводим к верхнему регистру и убираем пробелы. Полный VIN — 17 символов.
    public static string Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Trim().ToUpperInvariant().Replace(" ", "");

    // VIN — ровно 17 символов, только латиница и цифры, без букв I, O, Q
    // (стандарт ISO 3779, чтобы не путать с 1/0). Контрольную цифру (позиция 9)
    // не проверяем: у европейских VIN она часто не по формуле FMVSS.
    public static bool IsValid(string? value)
    {
        var vin = Normalize(value);
        if (vin.Length != Length)
        {
            return false;
        }

        foreach (var ch in vin)
        {
            var isDigit = ch is >= '0' and <= '9';
            var isAllowedLetter = ch is >= 'A' and <= 'Z' && ch is not ('I' or 'O' or 'Q');
            if (!isDigit && !isAllowedLetter)
            {
                return false;
            }
        }

        return true;
    }
}
