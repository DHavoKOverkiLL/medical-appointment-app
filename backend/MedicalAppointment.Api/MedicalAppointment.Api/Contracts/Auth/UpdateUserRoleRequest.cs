using System.ComponentModel.DataAnnotations;

namespace MedicalAppointment.Api.Contracts.Auth;

public class UpdateUserRoleRequest
{
    [Required]
    [MaxLength(100)]
    public string RoleName { get; set; } = string.Empty;
}
