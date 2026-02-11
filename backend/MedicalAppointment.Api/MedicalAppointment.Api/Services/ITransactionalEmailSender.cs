namespace MedicalAppointment.Api.Services;

public interface ITransactionalEmailSender
{
    Task<TransactionalEmailSendResult> SendAsync(
        TransactionalEmailMessage message,
        CancellationToken cancellationToken = default);
}

public sealed record TransactionalEmailSendResult(
    TransactionalEmailSendStatus Status,
    string? Error = null);

public enum TransactionalEmailSendStatus
{
    Sent = 0,
    SkippedNotConfigured = 1,
    Failed = 2
}
