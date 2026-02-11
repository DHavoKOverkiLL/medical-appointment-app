using System.ComponentModel.DataAnnotations;

namespace MedicalAppointment.Api.Contracts.Auth;

public class UpdateMyAccountSettingsRequest
{
    [Required]
    [MaxLength(100)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    [MaxLength(128)]
    public string CurrentPassword { get; set; } = string.Empty;

    [MinLength(8)]
    [MaxLength(128)]
    public string? NewPassword { get; set; }
}
