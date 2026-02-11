namespace MedicalAppointment.Api.Contracts.Auth;

public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public Guid ClinicId { get; set; }
    public string ClinicName { get; set; } = string.Empty;
}
