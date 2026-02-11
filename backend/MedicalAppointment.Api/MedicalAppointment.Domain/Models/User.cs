using System;
using System.Text.Json.Serialization;

namespace MedicalAppointment.Domain.Models
{
    public class User
    {
        public Guid UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        [JsonIgnore]
        public string PasswordHash { get; set; } = string.Empty;

        [JsonIgnore]
        public virtual SysRole? SysRole { get; set; }
        public Guid SysRoleId { get; set; }                  // FK     

        [JsonIgnore]
        public virtual Clinic Clinic { get; set; } = null!;
        public Guid ClinicId { get; set; }

        public Guid PersonId { get; set; }
        public virtual Person Person { get; set; } = null!;
    }
}
