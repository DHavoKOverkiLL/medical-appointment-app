using System.ComponentModel.DataAnnotations;

namespace MedicalAppointment.Api.Contracts.Appointments;

public class RespondToCounterPostponeRequest
{
    [Required]
    [MinLength(6)]
    [MaxLength(10)]
    public string Decision { get; set; } = string.Empty;
}
