using System;

namespace MedicalAppointment.Domain.Models;

public class UserNotification
{
    public Guid UserNotificationId { get; set; }
    public Guid UserId { get; set; }
    public Guid? AppointmentId { get; set; }
    public Guid? ActorUserId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ReadAtUtc { get; set; }

    public virtual User User { get; set; } = null!;
    public virtual User? ActorUser { get; set; }
    public virtual Appointment? Appointment { get; set; }
}
