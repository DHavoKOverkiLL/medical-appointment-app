namespace MedicalAppointment.Api.Contracts.Auth;

public class UserSummaryResponse
{
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string PersonalIdentifier { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public DateTime BirthDate { get; set; }
    public Guid ClinicId { get; set; }
    public string ClinicName { get; set; } = string.Empty;
}
