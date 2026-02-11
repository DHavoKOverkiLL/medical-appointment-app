using MedicalAppointment.Api.Services;
using MedicalAppointment.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedicalAppointment.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = SystemRoles.Admin)]
public class SysAccreditationsController : ControllerBase
{
    private readonly ISystemInfoService _systemInfoService;

    public SysAccreditationsController(ISystemInfoService systemInfoService)
    {
        _systemInfoService = systemInfoService;
    }

    [HttpGet]
    public async Task<IActionResult> GetActiveAccreditations()
    {
        var accreditations = await _systemInfoService.GetActiveAccreditationsAsync(HttpContext.RequestAborted);
        return Ok(accreditations);
    }
}
