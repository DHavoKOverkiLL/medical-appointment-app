using System;

namespace MedicalAppointment.Domain.Models;

public class ClinicService
{
    public Guid ClinicServiceId { get; set; }
    public Guid ClinicId { get; set; }
    public int SysOperationId { get; set; }
    public bool IsTelehealthAvailable { get; set; } = false;
    public bool IsActive { get; set; } = true;

    public virtual Clinic Clinic { get; set; } = null!;
    public virtual SysOperation SysOperation { get; set; } = null!;
}
