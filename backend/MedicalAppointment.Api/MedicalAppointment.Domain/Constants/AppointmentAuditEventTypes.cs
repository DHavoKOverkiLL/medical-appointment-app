namespace MedicalAppointment.Domain.Constants;

public static class AppointmentAuditEventTypes
{
    public const string Created = "Created";
    public const string PostponeRequested = "PostponeRequested";
    public const string PostponeApprovedByDoctor = "PostponeApprovedByDoctor";
    public const string PostponeRejectedByDoctor = "PostponeRejectedByDoctor";
    public const string PostponeCounterProposedByDoctor = "PostponeCounterProposedByDoctor";
    public const string PostponeCounterAcceptedByPatient = "PostponeCounterAcceptedByPatient";
    public const string PostponeCounterRejectedByPatient = "PostponeCounterRejectedByPatient";
    public const string Cancelled = "Cancelled";
    public const string AttendanceMarkedCompleted = "AttendanceMarkedCompleted";
    public const string AttendanceMarkedNoShow = "AttendanceMarkedNoShow";
}
