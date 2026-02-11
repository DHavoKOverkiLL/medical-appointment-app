using System.ComponentModel.DataAnnotations;

namespace MedicalAppointment.Api.Contracts.Auth;

public class UpdateMyProfileRequest
{
    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [MaxLength(256)]
    public string Address { get; set; } = string.Empty;

    [Phone]
    [MaxLength(32)]
    public string? PhoneNumber { get; set; }

    [Required]
    public DateTime BirthDate { get; set; }
}
