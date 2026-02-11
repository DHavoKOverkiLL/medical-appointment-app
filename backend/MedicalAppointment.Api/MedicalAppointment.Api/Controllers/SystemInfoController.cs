using MedicalAppointment.Api.Services;
using MedicalAppointment.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedicalAppointment.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = SystemRoles.Admin)]
public class SystemInfoController : ControllerBase
{
    private readonly ISystemInfoService _systemInfoService;

    public SystemInfoController(ISystemInfoService systemInfoService)
    {
        _systemInfoService = systemInfoService;
    }

    [HttpGet]
    public async Task<IActionResult> GetSystemInfo()
    {
        var systemInfo = await _systemInfoService.GetSystemInfoAsync(HttpContext.RequestAborted);
        return Ok(systemInfo);
    }
}
