using MedicalAppointment.Api.Services;
using MedicalAppointment.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedicalAppointment.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = SystemRoles.Admin)]
public class SysOperationsController : ControllerBase
{
    private readonly ISystemInfoService _systemInfoService;

    public SysOperationsController(ISystemInfoService systemInfoService)
    {
        _systemInfoService = systemInfoService;
    }

    [HttpGet]
    public async Task<IActionResult> GetActiveOperations()
    {
        var operations = await _systemInfoService.GetActiveOperationsAsync(HttpContext.RequestAborted);
        return Ok(operations);
    }
}
