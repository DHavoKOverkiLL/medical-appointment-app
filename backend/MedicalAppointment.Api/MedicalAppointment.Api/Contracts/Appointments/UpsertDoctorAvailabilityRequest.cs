using System.ComponentModel.DataAnnotations;

namespace MedicalAppointment.Api.Contracts.Appointments;

public class UpsertDoctorAvailabilityRequest
{
    [Required]
    public List<DoctorWeeklyTimeRangeRequest> WeeklyAvailability { get; set; } = [];

    [Required]
    public List<DoctorWeeklyTimeRangeRequest> WeeklyBreaks { get; set; } = [];

    [Required]
    public List<DoctorDateOverrideRequest> Overrides { get; set; } = [];
}
