using System.ComponentModel.DataAnnotations;

namespace MedicalAppointment.Api.Contracts.Clinics;

public class UpdateClinicRequest
{
    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(32)]
    public string Code { get; set; } = string.Empty;

    [MaxLength(200)]
    public string LegalName { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int SysClinicTypeId { get; set; } = 1;

    [Range(1, int.MaxValue)]
    public int SysOwnershipTypeId { get; set; } = 1;

    public DateOnly? FoundedOn { get; set; }

    [MaxLength(20)]
    public string NpiOrganization { get; set; } = string.Empty;

    [MaxLength(20)]
    public string Ein { get; set; } = string.Empty;

    [MaxLength(32)]
    public string TaxonomyCode { get; set; } = string.Empty;

    [MaxLength(64)]
    public string StateLicenseFacility { get; set; } = string.Empty;

    [MaxLength(32)]
    public string CliaNumber { get; set; } = string.Empty;

    [MaxLength(200)]
    public string AddressLine1 { get; set; } = string.Empty;

    [MaxLength(200)]
    public string AddressLine2 { get; set; } = string.Empty;

    [MaxLength(100)]
    public string City { get; set; } = string.Empty;

    [MaxLength(100)]
    public string State { get; set; } = string.Empty;

    [MaxLength(32)]
    public string PostalCode { get; set; } = string.Empty;

    [MaxLength(2)]
    public string CountryCode { get; set; } = "US";

    [MaxLength(64)]
    public string Timezone { get; set; } = "America/Chicago";

    [MaxLength(32)]
    public string MainPhone { get; set; } = string.Empty;

    [MaxLength(32)]
    public string Fax { get; set; } = string.Empty;

    [MaxLength(256)]
    [EmailAddress]
    public string MainEmail { get; set; } = string.Empty;

    [MaxLength(256)]
    [Url]
    public string WebsiteUrl { get; set; } = string.Empty;

    [MaxLength(256)]
    [Url]
    public string PatientPortalUrl { get; set; } = string.Empty;

    public List<string> BookingMethods { get; set; } = [];

    [Range(0, 365)]
    public int? AvgNewPatientWaitDays { get; set; }

    public bool SameDayAvailable { get; set; }

    [MaxLength(32)]
    public string HipaaNoticeVersion { get; set; } = string.Empty;

    public DateOnly? LastSecurityRiskAssessmentOn { get; set; }

    [Range(1, int.MaxValue)]
    public int SysSourceSystemId { get; set; } = 1;

    public List<ClinicOperatingHourContract> OperatingHours { get; set; } = [];
    public List<ClinicServiceContract> Services { get; set; } = [];
    public List<ClinicInsurancePlanContract> InsurancePlans { get; set; } = [];
    public List<ClinicAccreditationContract> Accreditations { get; set; } = [];

    public bool IsActive { get; set; } = true;
}
