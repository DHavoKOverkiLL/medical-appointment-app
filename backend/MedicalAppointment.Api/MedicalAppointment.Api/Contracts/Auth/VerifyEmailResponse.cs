namespace MedicalAppointment.Api.Contracts.Auth;

public class VerifyEmailResponse
{
    public bool EmailVerified { get; set; }
    public string Message { get; set; } = string.Empty;
}
