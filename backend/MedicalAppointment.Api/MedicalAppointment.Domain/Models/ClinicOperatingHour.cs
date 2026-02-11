using System;

namespace MedicalAppointment.Domain.Models;

public class ClinicOperatingHour
{
    public Guid ClinicOperatingHourId { get; set; }
    public Guid ClinicId { get; set; }
    public int DayOfWeek { get; set; }
    public TimeSpan? OpenTime { get; set; }
    public TimeSpan? CloseTime { get; set; }
    public bool IsClosed { get; set; } = false;

    public virtual Clinic Clinic { get; set; } = null!;
}
