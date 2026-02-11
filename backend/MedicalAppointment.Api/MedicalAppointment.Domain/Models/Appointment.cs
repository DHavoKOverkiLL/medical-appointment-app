using MedicalAppointment.Domain.Constants;
using MedicalAppointment.Domain.Models;

public class Appointment
{
    public Guid AppointmentId { get; set; }
    public Guid PatientId { get; set; }
    public Guid DoctorId { get; set; }
    public Guid ClinicId { get; set; }
    public DateTime AppointmentDateTime { get; set; }
    public string Status { get; set; } = AppointmentStatuses.Scheduled;
    public string PostponeRequestStatus { get; set; } = AppointmentPostponeStatuses.None;
    public DateTime? ProposedDateTime { get; set; }
    public string? PostponeReason { get; set; }
    public DateTime? PostponeRequestedAtUtc { get; set; }
    public string? DoctorResponseNote { get; set; }
    public DateTime? DoctorRespondedAtUtc { get; set; }
    public DateTime? PatientRespondedAtUtc { get; set; }
    public DateTime? CancelledAtUtc { get; set; }
    public Guid? CancelledByUserId { get; set; }
    public string? CancellationReason { get; set; }

    // Navigation properties
    public User Doctor { get; set; } = null!;
    public User Patient { get; set; } = null!;
    public Clinic Clinic { get; set; } = null!;
    public User? CancelledByUser { get; set; }
}
