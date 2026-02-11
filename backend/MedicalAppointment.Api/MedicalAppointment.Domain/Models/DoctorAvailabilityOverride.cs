using System;

namespace MedicalAppointment.Domain.Models;

public class DoctorAvailabilityOverride
{
    public Guid DoctorAvailabilityOverrideId { get; set; }
    public Guid DoctorId { get; set; }
    public DateOnly Date { get; set; }
    public TimeSpan? StartTime { get; set; }
    public TimeSpan? EndTime { get; set; }
    public bool IsAvailable { get; set; }
    public string Reason { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public virtual User Doctor { get; set; } = null!;
}
