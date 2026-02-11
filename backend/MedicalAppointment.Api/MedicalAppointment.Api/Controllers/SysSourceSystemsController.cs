using MedicalAppointment.Api.Services;
using MedicalAppointment.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedicalAppointment.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = SystemRoles.Admin)]
public class SysSourceSystemsController : ControllerBase
{
    private readonly ISystemInfoService _systemInfoService;

    public SysSourceSystemsController(ISystemInfoService systemInfoService)
    {
        _systemInfoService = systemInfoService;
    }

    [HttpGet]
    public async Task<IActionResult> GetActiveSourceSystems()
    {
        var sourceSystems = await _systemInfoService.GetActiveSourceSystemsAsync(HttpContext.RequestAborted);
        return Ok(sourceSystems);
    }
}
