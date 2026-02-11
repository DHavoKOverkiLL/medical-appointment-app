namespace MedicalAppointment.Api.Contracts.Auth;

public class RegisterResponse
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public Guid ClinicId { get; set; }
    public string ClinicName { get; set; } = string.Empty;
    public bool RequiresEmailVerification { get; set; } = true;
    public bool VerificationEmailSent { get; set; }
    public DateTime? VerificationCodeExpiresAtUtc { get; set; }
}
