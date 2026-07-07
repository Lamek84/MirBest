using System.ComponentModel.DataAnnotations;

namespace AutoPartsStore.Web.Models;

public class AppointmentViewModel
{
    [Required(ErrorMessage = "Bitte gib deinen Namen an.")]
    [StringLength(150)]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Bitte gib deine E-Mail-Adresse an.")]
    [EmailAddress(ErrorMessage = "Ungültige E-Mail-Adresse.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Bitte gib deine Telefonnummer an.")]
    [StringLength(50)]
    public string Phone { get; set; } = string.Empty;

    [StringLength(150)]
    [Display(Name = "Fahrzeug (Marke, Modell)")]
    public string? VehicleInfo { get; set; }

    [Required(ErrorMessage = "Bitte wähle eine Leistung aus.")]
    [Display(Name = "Leistung")]
    public string ServiceType { get; set; } = string.Empty;

    [Required(ErrorMessage = "Bitte wähle ein Wunschdatum aus.")]
    [DataType(DataType.Date)]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
    [Display(Name = "Wunschtermin")]
    public DateTime PreferredDate { get; set; } = DateTime.Today.AddDays(1);

    [Required(ErrorMessage = "Bitte wähle ein Zeitfenster aus.")]
    [Display(Name = "Zeitfenster")]
    public string TimeSlot { get; set; } = string.Empty;

    [StringLength(2000)]
    [DataType(DataType.MultilineText)]
    [Display(Name = "Was muss gemacht werden? (optional)")]
    public string? Message { get; set; }
}
