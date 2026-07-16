using System.ComponentModel.DataAnnotations;

namespace AutoPartsStore.Core.Entities;

// OEM- или кросс-номер, привязанный к товару. Основа поиска "по VIN / по номеру":
// из внешнего каталога (или ввода пользователя) приходит номер детали, мы находим
// по нему свои товары. Одна деталь может иметь много таких номеров.
public class ProductReferenceNumber : BaseEntity
{
    public int ProductId { get; set; }
    public Product? Product { get; set; }

    // Номер в исходном виде — для отображения (может содержать пробелы/дефисы).
    [Required]
    [StringLength(100)]
    public string Number { get; set; } = string.Empty;

    // Нормализованный номер для поиска: верхний регистр без пробелов/дефисов/точек.
    // Именно по нему строится индекс и идёт сопоставление, чтобы "1K0 615 301 AA",
    // "1K0615301AA" и "1k0-615-301-aa" считались одним номером.
    [Required]
    [StringLength(100)]
    public string NormalizedNumber { get; set; } = string.Empty;

    public ReferenceNumberType Type { get; set; }

    // Бренд, которому принадлежит номер (актуально прежде всего для кросс-номеров,
    // например "BOSCH", "FEBI"). Для OEM обычно совпадает с маркой авто.
    [StringLength(100)]
    public string? Brand { get; set; }

    // Единая нормализация номера — используется и при сохранении, и при поиске,
    // чтобы правила совпадали. Держим здесь, в Core, чтобы не дублировать логику.
    public static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var buffer = new System.Text.StringBuilder(value.Length);

        foreach (var ch in value)
        {
            if (char.IsLetterOrDigit(ch))
            {
                buffer.Append(char.ToUpperInvariant(ch));
            }
        }

        return buffer.ToString();
    }
}
