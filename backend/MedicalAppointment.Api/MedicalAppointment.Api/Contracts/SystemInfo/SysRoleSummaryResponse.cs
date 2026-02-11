namespace MedicalAppointment.Api.Contracts.SystemInfo;

public class SysRoleSummaryResponse
{
    public Guid SysRoleId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}
