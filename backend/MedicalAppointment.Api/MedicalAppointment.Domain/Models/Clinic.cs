using System;

namespace MedicalAppointment.Domain.Models;

public class Clinic
{
    public const int DefaultSysClinicTypeId = 1;
    public const int DefaultSysOwnershipTypeId = 1;
    public const int DefaultSysSourceSystemId = 1;

    public Guid ClinicId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string LegalName { get; set; } = string.Empty;
    public int SysClinicTypeId { get; set; } = DefaultSysClinicTypeId;
    public int SysOwnershipTypeId { get; set; } = DefaultSysOwnershipTypeId;
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

    public string BookingMethods { get; set; } = string.Empty;
    public int? AvgNewPatientWaitDays { get; set; }
    public bool SameDayAvailable { get; set; } = false;

    public string HipaaNoticeVersion { get; set; } = string.Empty;
    public DateOnly? LastSecurityRiskAssessmentOn { get; set; }
    public int SysSourceSystemId { get; set; } = DefaultSysSourceSystemId;

    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public bool IsActive { get; set; } = true;

    public virtual SysClinicType SysClinicType { get; set; } = null!;
    public virtual SysOwnershipType SysOwnershipType { get; set; } = null!;
    public virtual SysSourceSystem SysSourceSystem { get; set; } = null!;
    public virtual ICollection<User> Users { get; set; } = new List<User>();
    public virtual ICollection<ClinicOperatingHour> OperatingHours { get; set; } = new List<ClinicOperatingHour>();
    public virtual ICollection<ClinicService> Services { get; set; } = new List<ClinicService>();
    public virtual ICollection<ClinicInsurancePlan> InsurancePlans { get; set; } = new List<ClinicInsurancePlan>();
    public virtual ICollection<ClinicAccreditation> Accreditations { get; set; } = new List<ClinicAccreditation>();
}
