namespace MedicalAppointment.Domain.Models;

public class SysClinicType
{
    public int SysClinicTypeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public virtual ICollection<Clinic> Clinics { get; set; } = new List<Clinic>();
}
