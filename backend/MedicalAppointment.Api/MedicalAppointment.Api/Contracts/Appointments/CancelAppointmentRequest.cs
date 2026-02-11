using System.ComponentModel.DataAnnotations;

namespace MedicalAppointment.Api.Contracts.Appointments;

public class CancelAppointmentRequest
{
    [Required]
    [MinLength(5)]
    [MaxLength(500)]
    public string Reason { get; set; } = string.Empty;
}
