using System;

namespace MedicalAppointment.Domain.Models;

public class AppointmentAuditEvent
{
    public Guid AppointmentAuditEventId { get; set; }
    public Guid AppointmentId { get; set; }
    public Guid ClinicId { get; set; }
    public Guid? ActorUserId { get; set; }
    public string ActorRole { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public DateTime OccurredAtUtc { get; set; }

    public virtual Appointment Appointment { get; set; } = null!;
    public virtual Clinic Clinic { get; set; } = null!;
    public virtual User? ActorUser { get; set; }
}
