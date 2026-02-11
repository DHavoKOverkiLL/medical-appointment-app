using System;

namespace MedicalAppointment.Domain.Models;

public class DoctorAvailabilityBreak
{
    public Guid DoctorAvailabilityBreakId { get; set; }
    public Guid DoctorId { get; set; }
    public int DayOfWeek { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public bool IsActive { get; set; } = true;

    public virtual User Doctor { get; set; } = null!;
}
