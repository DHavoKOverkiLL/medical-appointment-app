namespace MedicalAppointment.Api.Contracts.Clinics;

public class ClinicSummaryResponse
{
    public Guid ClinicId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string LegalName { get; set; } = string.Empty;
    public int SysClinicTypeId { get; set; }
    public string ClinicType { get; set; } = string.Empty;
    public int SysOwnershipTypeId { get; set; }
    public string OwnershipType { get; set; } = string.Empty;
    public DateOnly? FoundedOn { get; set; }

    public string NpiOrganization { get; set; } = string.Empty;
    public string Ein { get; set; } = string.Empty;
    public string TaxonomyCode { get; set; } = string.Empty;
    public string StateLicenseFacility { get; set; } = string.Empty;
    public string CliaNumber { get; set; } = string.Empty;

    public string AddressLine1 { get; set; } = string.Empty;
    public string AddressLine2 { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string CountryCode { get; set; } = "US";
    public string Timezone { get; set; } = "America/Chicago";

    public string MainPhone { get; set; } = string.Empty;
    public string Fax { get; set; } = string.Empty;
    public string MainEmail { get; set; } = string.Empty;
    public string WebsiteUrl { get; set; } = string.Empty;
    public string PatientPortalUrl { get; set; } = string.Empty;

    public List<string> BookingMethods { get; set; } = [];
    public int? AvgNewPatientWaitDays { get; set; }
    public bool SameDayAvailable { get; set; }

    public string HipaaNoticeVersion { get; set; } = string.Empty;
    public DateOnly? LastSecurityRiskAssessmentOn { get; set; }
    public int SysSourceSystemId { get; set; }
    public string SourceSystem { get; set; } = "EHR";

    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public List<ClinicOperatingHourContract> OperatingHours { get; set; } = [];
    public List<ClinicServiceContract> Services { get; set; } = [];
    public List<ClinicInsurancePlanContract> InsurancePlans { get; set; } = [];
    public List<ClinicAccreditationContract> Accreditations { get; set; } = [];

    public bool IsActive { get; set; }
    public int UsersCount { get; set; }
}
