using System.ComponentModel.DataAnnotations;

namespace MedicalAppointment.Api.Contracts.Auth;

public class VerifyEmailRequest
{
    [Required]
    [EmailAddress]
    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MaxLength(16)]
    public string Code { get; set; } = string.Empty;
}
