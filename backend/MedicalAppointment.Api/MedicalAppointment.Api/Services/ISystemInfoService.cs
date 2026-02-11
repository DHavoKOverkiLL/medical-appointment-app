using MedicalAppointment.Api.Contracts.Clinics;
using MedicalAppointment.Api.Contracts.SystemInfo;

namespace MedicalAppointment.Api.Services;

public interface ISystemInfoService
{
    Task<IReadOnlyList<SysOperationSummaryResponse>> GetActiveOperationsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SysAccreditationSummaryResponse>> GetActiveAccreditationsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SysClinicTypeSummaryResponse>> GetActiveClinicTypesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SysOwnershipTypeSummaryResponse>> GetActiveOwnershipTypesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SysSourceSystemSummaryResponse>> GetActiveSourceSystemsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SysRoleSummaryResponse>> GetActiveRolesAsync(CancellationToken cancellationToken = default);
    Task<SystemInfoResponse> GetSystemInfoAsync(CancellationToken cancellationToken = default);
    Task<ClinicSystemLookups> GetClinicSystemLookupsAsync(CancellationToken cancellationToken = default);
}
