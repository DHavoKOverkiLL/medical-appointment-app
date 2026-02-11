using System.Security.Claims;

namespace MedicalAppointment.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static bool TryGetUserId(this ClaimsPrincipal user, out Guid userId)
    {
        var claim = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(claim, out userId);
    }

    public static bool TryGetClinicId(this ClaimsPrincipal user, out Guid clinicId)
    {
        var claim = user.FindFirstValue("clinic_id");
        return Guid.TryParse(claim, out clinicId);
    }
}
