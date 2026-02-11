using MedicalAppointment.Domain.Models;

namespace MedicalAppointment.Api.Services;

public interface IJwtTokenService
{
    (string Token, DateTime ExpiresAtUtc) CreateToken(User user, string roleName);
}

