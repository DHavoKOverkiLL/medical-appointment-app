namespace MedicalAppointment.Api.Contracts.Auth;

public class ResendEmailVerificationResponse
{
    public bool VerificationEmailSent { get; set; }
    public DateTime? NextAllowedAtUtc { get; set; }
    public string Message { get; set; } = string.Empty;
}
