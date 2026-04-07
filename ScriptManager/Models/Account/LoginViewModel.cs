using System.ComponentModel.DataAnnotations;

namespace ScriptManager.Models.Account;

public class LoginViewModel
{
    [Required(ErrorMessage = "E-posta girin.")]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Şifre girin.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}
