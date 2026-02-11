namespace MedicalAppointment.Api.Contracts.Auth;

public class EmailVerificationRequiredResponse
{
    public bool RequiresEmailVerification { get; set; } = true;
    public string Email { get; set; } = string.Empty;
    public bool VerificationEmailSent { get; set; }
    public DateTime? NextAllowedAtUtc { get; set; }
    public string Message { get; set; } = string.Empty;
}
