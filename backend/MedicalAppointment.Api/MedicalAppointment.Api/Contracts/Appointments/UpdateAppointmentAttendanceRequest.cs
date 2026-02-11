using System.ComponentModel.DataAnnotations;

namespace MedicalAppointment.Api.Contracts.Appointments;

public class UpdateAppointmentAttendanceRequest
{
    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = string.Empty;
}
