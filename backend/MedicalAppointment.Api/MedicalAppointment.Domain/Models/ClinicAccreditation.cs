using System;

namespace MedicalAppointment.Domain.Models;

public class ClinicAccreditation
{
    public Guid ClinicAccreditationId { get; set; }
    public Guid ClinicId { get; set; }
    public int SysAccreditationId { get; set; }
    public DateOnly? EffectiveOn { get; set; }
    public DateOnly? ExpiresOn { get; set; }
    public bool IsActive { get; set; } = true;

    public virtual Clinic Clinic { get; set; } = null!;
    public virtual SysAccreditation SysAccreditation { get; set; } = null!;
}
