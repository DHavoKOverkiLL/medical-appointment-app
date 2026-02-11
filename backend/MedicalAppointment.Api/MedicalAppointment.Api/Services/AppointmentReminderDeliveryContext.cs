namespace MedicalAppointment.Api.Services;

public sealed record AppointmentReminderDeliveryContext(
    Guid AppointmentId,
    Guid RecipientUserId,
    string? RecipientEmail,
    string? RecipientPhoneNumber,
    string RecipientDisplayName,
    string ReminderType,
    string Title,
    string Message,
    DateTime AppointmentDateTimeUtc,
    string ClinicName,
    string ClinicTimezoneId,
    string DoctorName);
