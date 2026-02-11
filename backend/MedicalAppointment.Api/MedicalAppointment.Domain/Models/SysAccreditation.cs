namespace MedicalAppointment.Domain.Models;

public class SysAccreditation
{
    public int SysAccreditationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public virtual ICollection<ClinicAccreditation> ClinicAccreditations { get; set; } = new List<ClinicAccreditation>();
}
