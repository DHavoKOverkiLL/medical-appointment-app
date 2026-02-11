namespace MedicalAppointment.Domain.Constants;

public static class NotificationTypes
{
    public const string AppointmentReminder = "AppointmentReminder";
    public const string PostponeRequested = "PostponeRequested";
    public const string PostponeApproved = "PostponeApproved";
    public const string PostponeRejected = "PostponeRejected";
    public const string PostponeCounterProposed = "PostponeCounterProposed";
    public const string PostponeCounterAccepted = "PostponeCounterAccepted";
    public const string PostponeCounterRejected = "PostponeCounterRejected";
    public const string AppointmentCancelled = "AppointmentCancelled";
}
