using System.ComponentModel.DataAnnotations;

namespace AutoPartsStore.Web.Models;

public class ChangePasswordViewModel
{
    [Required(ErrorMessage = "Aktuelles Passwort ist erforderlich.")]
    [DataType(DataType.Password)]
    [Display(Name = "Aktuelles Passwort")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Neues Passwort ist erforderlich.")]
    [DataType(DataType.Password)]
    [Display(Name = "Neues Passwort")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Bitte bestätigen Sie das neue Passwort.")]
    [DataType(DataType.Password)]
    [Display(Name = "Neues Passwort bestätigen")]
    [Compare(nameof(NewPassword), ErrorMessage = "Die Passwörter stimmen nicht überein.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
