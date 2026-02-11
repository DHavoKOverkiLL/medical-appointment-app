using System;

namespace MedicalAppointment.Domain.Models;

public class AppointmentReminderDispatch
{
    public Guid AppointmentReminderDispatchId { get; set; }
    public Guid AppointmentId { get; set; }
    public Guid RecipientUserId { get; set; }
    public string ReminderType { get; set; } = string.Empty;
    public DateTime ScheduledForUtc { get; set; }
    public DateTime SentAtUtc { get; set; }

    public virtual Appointment Appointment { get; set; } = null!;
    public virtual User RecipientUser { get; set; } = null!;
}
