using MedicalAppointment.Api.Services;
using MedicalAppointment.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedicalAppointment.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = SystemRoles.Admin)]
public class SysOwnershipTypesController : ControllerBase
{
    private readonly ISystemInfoService _systemInfoService;

    public SysOwnershipTypesController(ISystemInfoService systemInfoService)
    {
        _systemInfoService = systemInfoService;
    }

    [HttpGet]
    public async Task<IActionResult> GetActiveOwnershipTypes()
    {
        var ownershipTypes = await _systemInfoService.GetActiveOwnershipTypesAsync(HttpContext.RequestAborted);
        return Ok(ownershipTypes);
    }
}
