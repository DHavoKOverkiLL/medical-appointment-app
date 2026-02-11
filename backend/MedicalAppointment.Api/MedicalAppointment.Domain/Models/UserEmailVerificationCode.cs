using System.Text.Json.Serialization;

namespace MedicalAppointment.Domain.Models;

public class UserEmailVerificationCode
{
    public Guid UserEmailVerificationCodeId { get; set; }
    public Guid UserId { get; set; }
    public string CodeHash { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? ConsumedAtUtc { get; set; }
    public int AttemptCount { get; set; }
    public DateTime? LastAttemptAtUtc { get; set; }
    public string Trigger { get; set; } = string.Empty;

    [JsonIgnore]
    public virtual User User { get; set; } = null!;
}
