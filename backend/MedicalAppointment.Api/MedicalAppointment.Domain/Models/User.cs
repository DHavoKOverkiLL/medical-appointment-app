using System;
using System.Text.Json.Serialization;

namespace MedicalAppointment.Domain.Models
{
    public class User
    {
        public Guid UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool IsEmailVerified { get; set; }
        public DateTime? EmailVerifiedAtUtc { get; set; }
        public DateTime? VerificationEmailLastSentAtUtc { get; set; }
        [JsonIgnore]
        public string PasswordHash { get; set; } = string.Empty;
        public int FailedLoginAttempts { get; set; }
        public DateTime? LockoutEndUtc { get; set; }
        public DateTime? LastFailedLoginAtUtc { get; set; }

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
