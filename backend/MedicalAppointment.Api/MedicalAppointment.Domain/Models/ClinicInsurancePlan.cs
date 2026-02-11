using System;

namespace MedicalAppointment.Domain.Models;

public class ClinicInsurancePlan
{
    public Guid ClinicInsurancePlanId { get; set; }
    public Guid ClinicId { get; set; }
    public string PayerName { get; set; } = string.Empty;
    public string PlanName { get; set; } = string.Empty;
    public bool IsInNetwork { get; set; } = true;
    public bool IsActive { get; set; } = true;

    public virtual Clinic Clinic { get; set; } = null!;
}
