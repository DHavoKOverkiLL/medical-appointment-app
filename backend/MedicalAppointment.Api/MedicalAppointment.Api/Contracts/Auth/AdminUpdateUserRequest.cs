using System.ComponentModel.DataAnnotations;

namespace MedicalAppointment.Api.Contracts.Auth;

public class AdminUpdateUserRequest
{
    [Required]
    [MaxLength(100)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [MaxLength(64)]
    public string PersonalIdentifier { get; set; } = string.Empty;

    [MaxLength(256)]
    public string Address { get; set; } = string.Empty;

    [Phone]
    [MaxLength(32)]
    public string? PhoneNumber { get; set; }

    [Required]
    public DateTime BirthDate { get; set; }

    [Required]
    public Guid ClinicId { get; set; }
}
