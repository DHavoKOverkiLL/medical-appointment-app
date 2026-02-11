using MedicalAppointment.Api.Contracts.Clinics;
using MedicalAppointment.Api.Contracts.SystemInfo;
using MedicalAppointment.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace MedicalAppointment.Api.Services;

public class SystemInfoService : ISystemInfoService
{
    private readonly AppDbContext _context;

    public SystemInfoService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<SysOperationSummaryResponse>> GetActiveOperationsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SysOperations
            .AsNoTracking()
            .Where(operation => operation.IsActive)
            .OrderBy(operation => operation.Name)
            .Select(operation => new SysOperationSummaryResponse
            {
                SysOperationId = operation.SysOperationId,
                Name = operation.Name
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SysAccreditationSummaryResponse>> GetActiveAccreditationsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SysAccreditations
            .AsNoTracking()
            .Where(accreditation => accreditation.IsActive)
            .OrderBy(accreditation => accreditation.Name)
            .Select(accreditation => new SysAccreditationSummaryResponse
            {
                SysAccreditationId = accreditation.SysAccreditationId,
                Name = accreditation.Name
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SysClinicTypeSummaryResponse>> GetActiveClinicTypesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SysClinicTypes
            .AsNoTracking()
            .Where(clinicType => clinicType.IsActive)
            .OrderBy(clinicType => clinicType.Name)
            .Select(clinicType => new SysClinicTypeSummaryResponse
            {
                SysClinicTypeId = clinicType.SysClinicTypeId,
                Name = clinicType.Name
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SysOwnershipTypeSummaryResponse>> GetActiveOwnershipTypesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SysOwnershipTypes
            .AsNoTracking()
            .Where(ownershipType => ownershipType.IsActive)
            .OrderBy(ownershipType => ownershipType.Name)
            .Select(ownershipType => new SysOwnershipTypeSummaryResponse
            {
                SysOwnershipTypeId = ownershipType.SysOwnershipTypeId,
                Name = ownershipType.Name
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SysSourceSystemSummaryResponse>> GetActiveSourceSystemsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SysSourceSystems
            .AsNoTracking()
            .Where(sourceSystem => sourceSystem.IsActive)
            .OrderBy(sourceSystem => sourceSystem.Name)
            .Select(sourceSystem => new SysSourceSystemSummaryResponse
            {
                SysSourceSystemId = sourceSystem.SysSourceSystemId,
                Name = sourceSystem.Name
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SysRoleSummaryResponse>> GetActiveRolesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SysRoles
            .AsNoTracking()
            .Where(role => role.IsActive)
            .OrderBy(role => role.Name)
            .Select(role => new SysRoleSummaryResponse
            {
                SysRoleId = role.SysRoleId,
                Name = role.Name,
                Description = role.Description
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<SystemInfoResponse> GetSystemInfoAsync(CancellationToken cancellationToken = default)
    {
        var roles = await GetActiveRolesAsync(cancellationToken);
        var operations = await GetActiveOperationsAsync(cancellationToken);
        var accreditations = await GetActiveAccreditationsAsync(cancellationToken);
        var clinicTypes = await GetActiveClinicTypesAsync(cancellationToken);
        var ownershipTypes = await GetActiveOwnershipTypesAsync(cancellationToken);
        var sourceSystems = await GetActiveSourceSystemsAsync(cancellationToken);

        return new SystemInfoResponse
        {
            Roles = roles.ToList(),
            Operations = operations.ToList(),
            Accreditations = accreditations.ToList(),
            ClinicTypes = clinicTypes.ToList(),
            OwnershipTypes = ownershipTypes.ToList(),
            SourceSystems = sourceSystems.ToList()
        };
    }

    public async Task<ClinicSystemLookups> GetClinicSystemLookupsAsync(CancellationToken cancellationToken = default)
    {
        var operationsById = await _context.SysOperations
            .AsNoTracking()
            .Where(operation => operation.IsActive)
            .ToDictionaryAsync(operation => operation.SysOperationId, cancellationToken);

        var accreditationsById = await _context.SysAccreditations
            .AsNoTracking()
            .Where(accreditation => accreditation.IsActive)
            .ToDictionaryAsync(accreditation => accreditation.SysAccreditationId, cancellationToken);

        var clinicTypesById = await _context.SysClinicTypes
            .AsNoTracking()
            .Where(clinicType => clinicType.IsActive)
            .ToDictionaryAsync(clinicType => clinicType.SysClinicTypeId, cancellationToken);

        var ownershipTypesById = await _context.SysOwnershipTypes
            .AsNoTracking()
            .Where(ownershipType => ownershipType.IsActive)
            .ToDictionaryAsync(ownershipType => ownershipType.SysOwnershipTypeId, cancellationToken);

        var sourceSystemsById = await _context.SysSourceSystems
            .AsNoTracking()
            .Where(sourceSystem => sourceSystem.IsActive)
            .ToDictionaryAsync(sourceSystem => sourceSystem.SysSourceSystemId, cancellationToken);

        return new ClinicSystemLookups
        {
            OperationsById = operationsById,
            AccreditationsById = accreditationsById,
            ClinicTypesById = clinicTypesById,
            OwnershipTypesById = ownershipTypesById,
            SourceSystemsById = sourceSystemsById
        };
    }
}
