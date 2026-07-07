namespace AutoPartsStore.Core.Entities;

public class CartItem : BaseEntity
{
    // Скалярная ссылка на пользователя (Identity живёт в Data, поэтому здесь
    // только строковый Id — без навигационного свойства на ApplicationUser).
    public string UserId { get; set; } = string.Empty;

    public int ProductId { get; set; }
    public Product? Product { get; set; }

    public int Quantity { get; set; }
}
