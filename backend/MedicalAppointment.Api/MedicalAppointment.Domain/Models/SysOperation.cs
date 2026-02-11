namespace MedicalAppointment.Domain.Models;

public class SysOperation
{
    public int SysOperationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public virtual ICollection<ClinicService> ClinicServices { get; set; } = new List<ClinicService>();
}
