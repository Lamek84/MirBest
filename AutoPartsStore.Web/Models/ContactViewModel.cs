using System.ComponentModel.DataAnnotations;

namespace AutoPartsStore.Web.Models;

public class ContactViewModel
{
    [Required(ErrorMessage = "Bitte gib deinen Namen an.")]
    [StringLength(150)]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Bitte gib deine E-Mail-Adresse an.")]
    [EmailAddress(ErrorMessage = "Ungültige E-Mail-Adresse.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Bitte gib eine Nachricht ein.")]
    [StringLength(4000)]
    [DataType(DataType.MultilineText)]
    public string Message { get; set; } = string.Empty;
}
