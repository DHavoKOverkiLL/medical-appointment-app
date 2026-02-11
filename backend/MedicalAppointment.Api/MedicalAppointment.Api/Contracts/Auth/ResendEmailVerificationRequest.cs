using System.ComponentModel.DataAnnotations;

namespace MedicalAppointment.Api.Contracts.Auth;

public class ResendEmailVerificationRequest
{
    [Required]
    [EmailAddress]
    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;
}
