using System.ComponentModel.DataAnnotations;

namespace AutoPartsStore.Web.Models;

public class LoginViewModel
{
    [Required(ErrorMessage = "E-Mail ist erforderlich.")]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Passwort ist erforderlich.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}
