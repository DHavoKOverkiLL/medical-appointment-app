using MedicalAppointment.Api.Contracts.Clinics;

namespace MedicalAppointment.Api.Contracts.SystemInfo;

public class SystemInfoResponse
{
    public List<SysRoleSummaryResponse> Roles { get; init; } = [];
    public List<SysOperationSummaryResponse> Operations { get; init; } = [];
    public List<SysAccreditationSummaryResponse> Accreditations { get; init; } = [];
    public List<SysClinicTypeSummaryResponse> ClinicTypes { get; init; } = [];
    public List<SysOwnershipTypeSummaryResponse> OwnershipTypes { get; init; } = [];
    public List<SysSourceSystemSummaryResponse> SourceSystems { get; init; } = [];
}
