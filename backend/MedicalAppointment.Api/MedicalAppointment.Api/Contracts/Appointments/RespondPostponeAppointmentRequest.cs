using System.ComponentModel.DataAnnotations;

namespace MedicalAppointment.Api.Contracts.Appointments;

public class RespondPostponeAppointmentRequest
{
    [Required]
    [MinLength(4)]
    [MaxLength(30)]
    public string Decision { get; set; } = string.Empty;

    public DateTime? CounterProposedDateTime { get; set; }

    [MaxLength(500)]
    public string? Note { get; set; }
}
