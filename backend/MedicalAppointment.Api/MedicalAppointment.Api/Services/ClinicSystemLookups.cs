using MedicalAppointment.Domain.Models;

namespace MedicalAppointment.Api.Services;

public sealed class ClinicSystemLookups
{
    public required IReadOnlyDictionary<int, SysOperation> OperationsById { get; init; }
    public required IReadOnlyDictionary<int, SysAccreditation> AccreditationsById { get; init; }
    public required IReadOnlyDictionary<int, SysClinicType> ClinicTypesById { get; init; }
    public required IReadOnlyDictionary<int, SysOwnershipType> OwnershipTypesById { get; init; }
    public required IReadOnlyDictionary<int, SysSourceSystem> SourceSystemsById { get; init; }
}
