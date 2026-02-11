using MedicalAppointment.Api.Services;
using MedicalAppointment.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedicalAppointment.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = SystemRoles.Admin)]
public class SysClinicTypesController : ControllerBase
{
    private readonly ISystemInfoService _systemInfoService;

    public SysClinicTypesController(ISystemInfoService systemInfoService)
    {
        _systemInfoService = systemInfoService;
    }

    [HttpGet]
    public async Task<IActionResult> GetActiveClinicTypes()
    {
        var clinicTypes = await _systemInfoService.GetActiveClinicTypesAsync(HttpContext.RequestAborted);
        return Ok(clinicTypes);
    }
}
