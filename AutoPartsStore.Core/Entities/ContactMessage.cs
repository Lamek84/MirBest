using System.ComponentModel.DataAnnotations;

namespace AutoPartsStore.Core.Entities;

// Заявка с формы обратной связи (страница Kontakt). Сохраняется в БД
// независимо от того, удалось ли отправить письмо — так сообщение
// клиента не теряется, даже если SMTP не настроен или недоступен.
public class ContactMessage : BaseEntity
{
    [Required(ErrorMessage = "Name ist erforderlich.")]
    [StringLength(150)]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "E-Mail ist erforderlich.")]
    [StringLength(200)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nachricht ist erforderlich.")]
    [StringLength(4000)]
    public string Message { get; set; } = string.Empty;

    public bool EmailSent { get; set; }
}
