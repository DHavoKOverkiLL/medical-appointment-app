using System.ComponentModel.DataAnnotations;

namespace MedicalAppointment.Api.Contracts.Appointments;

public class DoctorDateOverrideRequest
{
    [Required]
    public DateOnly Date { get; set; }

    [MaxLength(5)]
    public string? Start { get; set; }

    [MaxLength(5)]
    public string? End { get; set; }

    public bool IsAvailable { get; set; }

    [MaxLength(200)]
    public string? Reason { get; set; }
}
