using MedicalAppointment.Domain.Models;

public class SysRole
{
    public Guid SysRoleId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }  // if using Description
    public bool IsActive { get; set; }

    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
