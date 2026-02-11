namespace MedicalAppointment.Api.Services;

public sealed record TransactionalEmailMessage(
    string RecipientEmail,
    string? RecipientDisplayName,
    string Subject,
    string Body);
