using System.Security.Cryptography;
using System.Text;

namespace MedicalAppointment.Api.Services;

public static class EmailVerificationCodeHasher
{
    public static string ComputeHash(Guid userId, string code, string key)
    {
        var normalizedCode = NormalizeCode(code);
        var payload = $"{userId:N}:{normalizedCode}";
        var keyBytes = Encoding.UTF8.GetBytes(key);
        var payloadBytes = Encoding.UTF8.GetBytes(payload);

        using var hmac = new HMACSHA256(keyBytes);
        var hash = hmac.ComputeHash(payloadBytes);
        return Convert.ToHexString(hash);
    }

    public static string NormalizeCode(string code)
    {
        return code.Trim().Replace(" ", string.Empty);
    }
}
