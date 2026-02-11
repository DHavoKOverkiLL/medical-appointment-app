using MedicalAppointment.Domain.Models;

namespace MedicalAppointment.Api.Services;

public interface IEmailVerificationService
{
    Task<EmailVerificationIssueResult> IssueCodeAsync(
        User user,
        string trigger,
        CancellationToken cancellationToken = default);

    Task<EmailVerificationCheckResult> VerifyCodeAsync(
        string email,
        string code,
        CancellationToken cancellationToken = default);
}

public sealed record EmailVerificationIssueResult(
    EmailVerificationIssueStatus Status,
    DateTime? ExpiresAtUtc = null,
    DateTime? NextAllowedAtUtc = null);

public enum EmailVerificationIssueStatus
{
    Sent = 0,
    CooldownActive = 1,
    DailyLimitReached = 2,
    UserAlreadyVerified = 3,
    FeatureDisabled = 4,
    DeliveryNotConfigured = 5,
    DeliveryFailed = 6
}

public sealed record EmailVerificationCheckResult(EmailVerificationCheckStatus Status);

public enum EmailVerificationCheckStatus
{
    Success = 0,
    InvalidOrExpired = 1,
    UserAlreadyVerified = 2,
    FeatureDisabled = 3
}

public static class EmailVerificationTriggers
{
    public const string Registration = "registration";
    public const string LoginUnverified = "login_unverified";
    public const string ManualResend = "manual_resend";
}
