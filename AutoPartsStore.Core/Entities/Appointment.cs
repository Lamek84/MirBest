using System.ComponentModel.DataAnnotations;

namespace AutoPartsStore.Core.Entities;

// Заявка на Termin (техосмотр/ремонт/обслуживание) из формы бронирования
// на разделе KFZ Autowerkstatt. Без привязки к аккаунту — гость тоже
// может записаться, как и через форму контактов.
public class Appointment : BaseEntity
{
    [Required(ErrorMessage = "Name ist erforderlich.")]
    [StringLength(150)]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "E-Mail ist erforderlich.")]
    [StringLength(200)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Telefon ist erforderlich.")]
    [StringLength(50)]
    public string Phone { get; set; } = string.Empty;

    [StringLength(150)]
    public string? VehicleInfo { get; set; }

    [Required(ErrorMessage = "Leistung ist erforderlich.")]
    [StringLength(100)]
    public string ServiceType { get; set; } = string.Empty;

    [Required]
    public DateTime PreferredDate { get; set; }

    [Required(ErrorMessage = "Zeitfenster ist erforderlich.")]
    [StringLength(50)]
    public string TimeSlot { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Message { get; set; }

    public string Status { get; set; } = AppointmentStatus.New;
}
