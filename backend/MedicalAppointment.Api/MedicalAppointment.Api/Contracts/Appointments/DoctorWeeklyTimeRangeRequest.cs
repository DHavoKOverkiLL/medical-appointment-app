using System.ComponentModel.DataAnnotations;

namespace MedicalAppointment.Api.Contracts.Appointments;

public class DoctorWeeklyTimeRangeRequest
{
    [Range(0, 6)]
    public int DayOfWeek { get; set; }

    [Required]
    [MaxLength(5)]
    public string Start { get; set; } = string.Empty;

    [Required]
    [MaxLength(5)]
    public string End { get; set; } = string.Empty;
}
