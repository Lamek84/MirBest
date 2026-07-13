using System.ComponentModel.DataAnnotations;

namespace AutoPartsStore.Web.Models;

public class RegisterViewModel
{
    [Required(ErrorMessage = "E-Mail ist erforderlich.")]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Passwort ist erforderlich.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Passwort muss mindestens 8 Zeichen haben, mit Groß-/Kleinbuchstabe, Ziffer und Sonderzeichen.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "Passwörter stimmen nicht überein.")]
    public string ConfirmPassword { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}
