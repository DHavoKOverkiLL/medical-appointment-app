using MedicalAppointment.Api.Contracts.Clinics;
using MedicalAppointment.Api.Services;
using MedicalAppointment.Domain.Constants;
using MedicalAppointment.Domain.Models;
using MedicalAppointment.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace MedicalAppointment.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ClinicController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ISystemInfoService _systemInfoService;

    public ClinicController(AppDbContext context, ISystemInfoService systemInfoService)
    {
        _context = context;
        _systemInfoService = systemInfoService;
    }

    [HttpGet("public")]
    [AllowAnonymous]
    public async Task<IActionResult> GetActiveClinics()
    {
        var clinics = await _context.Clinics
            .AsNoTracking()
            .Include(c => c.SysClinicType)
            .Include(c => c.SysOwnershipType)
            .Include(c => c.SysSourceSystem)
            .Include(c => c.OperatingHours)
            .Include(c => c.Services)
                .ThenInclude(s => s.SysOperation)
            .Include(c => c.InsurancePlans)
            .Include(c => c.Accreditations)
                .ThenInclude(a => a.SysAccreditation)
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .ToListAsync();

        return Ok(clinics.Select(c => MapClinic(c, 0)));
    }

    [HttpGet]
    [Authorize(Roles = SystemRoles.Admin)]
    public async Task<IActionResult> GetClinics()
    {
        var userCounts = await _context.Users
            .AsNoTracking()
            .GroupBy(u => u.ClinicId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count);

        var clinics = await _context.Clinics
            .AsNoTracking()
            .Include(c => c.SysClinicType)
            .Include(c => c.SysOwnershipType)
            .Include(c => c.SysSourceSystem)
            .Include(c => c.OperatingHours)
            .Include(c => c.Services)
                .ThenInclude(s => s.SysOperation)
            .Include(c => c.InsurancePlans)
            .Include(c => c.Accreditations)
                .ThenInclude(a => a.SysAccreditation)
            .OrderBy(c => c.Name)
            .ToListAsync();

        return Ok(clinics.Select(c => MapClinic(c, userCounts.GetValueOrDefault(c.ClinicId, 0))));
    }

    [HttpPost]
    [Authorize(Roles = SystemRoles.Admin)]
    public async Task<IActionResult> CreateClinic([FromBody] CreateClinicRequest request)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var normalizedName = request.Name.Trim();
        var normalizedCode = NormalizeCode(request.Code);
        if (string.IsNullOrWhiteSpace(normalizedName) || string.IsNullOrWhiteSpace(normalizedCode))
        {
            return BadRequest("Clinic name and code are required.");
        }

        if (!TryBuildOperatingHours(Guid.Empty, request.OperatingHours, out var operatingHours, out var operatingHoursError))
        {
            return BadRequest(operatingHoursError);
        }

        var lookups = await _systemInfoService.GetClinicSystemLookupsAsync(HttpContext.RequestAborted);
        var clinicTypeLookup = lookups.ClinicTypesById;
        if (clinicTypeLookup.Count == 0)
        {
            return Problem(
                title: "No clinic types configured",
                detail: "At least one active SysClinicType is required before creating clinics.",
                statusCode: StatusCodes.Status500InternalServerError);
        }

        if (!clinicTypeLookup.ContainsKey(request.SysClinicTypeId))
        {
            return BadRequest($"sysClinicTypeId '{request.SysClinicTypeId}' is invalid or inactive.");
        }

        var ownershipTypeLookup = lookups.OwnershipTypesById;
        if (ownershipTypeLookup.Count == 0)
        {
            return Problem(
                title: "No ownership types configured",
                detail: "At least one active SysOwnershipType is required before creating clinics.",
                statusCode: StatusCodes.Status500InternalServerError);
        }

        if (!ownershipTypeLookup.ContainsKey(request.SysOwnershipTypeId))
        {
            return BadRequest($"sysOwnershipTypeId '{request.SysOwnershipTypeId}' is invalid or inactive.");
        }

        var sourceSystemLookup = lookups.SourceSystemsById;
        if (sourceSystemLookup.Count == 0)
        {
            return Problem(
                title: "No source systems configured",
                detail: "At least one active SysSourceSystem is required before creating clinics.",
                statusCode: StatusCodes.Status500InternalServerError);
        }

        if (!sourceSystemLookup.ContainsKey(request.SysSourceSystemId))
        {
            return BadRequest($"sysSourceSystemId '{request.SysSourceSystemId}' is invalid or inactive.");
        }

        var operationLookup = lookups.OperationsById;
        if (operationLookup.Count == 0)
        {
            return Problem(
                title: "No service operations configured",
                detail: "At least one active SysOperation is required before saving clinic services.",
                statusCode: StatusCodes.Status500InternalServerError);
        }

        if (!TryBuildServices(Guid.Empty, request.Services, operationLookup, out var services, out var servicesError))
        {
            return BadRequest(servicesError);
        }

        if (!TryBuildInsurancePlans(Guid.Empty, request.InsurancePlans, out var insurancePlans, out var insurancePlansError))
        {
            return BadRequest(insurancePlansError);
        }

        var accreditationLookup = lookups.AccreditationsById;
        if (accreditationLookup.Count == 0)
        {
            return Problem(
                title: "No accreditation types configured",
                detail: "At least one active SysAccreditation is required before saving clinic accreditations.",
                statusCode: StatusCodes.Status500InternalServerError);
        }

        if (!TryBuildAccreditations(Guid.Empty, request.Accreditations, accreditationLookup, out var accreditations, out var accreditationsError))
        {
            return BadRequest(accreditationsError);
        }

        if (await _context.Clinics.AnyAsync(c => c.Name.ToLower() == normalizedName.ToLower()))
        {
            return Conflict("A clinic with this name already exists.");
        }

        if (await _context.Clinics.AnyAsync(c => c.Code.ToLower() == normalizedCode.ToLower()))
        {
            return Conflict("A clinic with this code already exists.");
        }

        var nowUtc = DateTime.UtcNow;
        var clinicId = Guid.NewGuid();
        var clinic = new Clinic
        {
            ClinicId = clinicId,
            Name = normalizedName,
            Code = normalizedCode,
            LegalName = NormalizeTextOrFallback(request.LegalName, normalizedName),
            SysClinicTypeId = request.SysClinicTypeId,
            SysOwnershipTypeId = request.SysOwnershipTypeId,
            FoundedOn = request.FoundedOn,
            NpiOrganization = NormalizeCodeLikeIdentifier(request.NpiOrganization),
            Ein = NormalizeText(request.Ein),
            TaxonomyCode = NormalizeCodeLikeIdentifier(request.TaxonomyCode),
            StateLicenseFacility = NormalizeCodeLikeIdentifier(request.StateLicenseFacility),
            CliaNumber = NormalizeCodeLikeIdentifier(request.CliaNumber),
            AddressLine1 = NormalizeText(request.AddressLine1),
            AddressLine2 = NormalizeText(request.AddressLine2),
            City = NormalizeText(request.City),
            State = NormalizeText(request.State),
            PostalCode = NormalizeText(request.PostalCode),
            CountryCode = NormalizeCountryCode(request.CountryCode),
            Timezone = NormalizeTextOrFallback(request.Timezone, "America/Chicago"),
            MainPhone = NormalizeText(request.MainPhone),
            Fax = NormalizeText(request.Fax),
            MainEmail = NormalizeEmailOptional(request.MainEmail),
            WebsiteUrl = NormalizeText(request.WebsiteUrl),
            PatientPortalUrl = NormalizeText(request.PatientPortalUrl),
            BookingMethods = string.Join(',', NormalizeBookingMethods(request.BookingMethods)),
            AvgNewPatientWaitDays = request.AvgNewPatientWaitDays,
            SameDayAvailable = request.SameDayAvailable,
            HipaaNoticeVersion = NormalizeText(request.HipaaNoticeVersion),
            LastSecurityRiskAssessmentOn = request.LastSecurityRiskAssessmentOn,
            SysSourceSystemId = request.SysSourceSystemId,
            CreatedAtUtc = nowUtc,
            UpdatedAtUtc = nowUtc,
            IsActive = true
        };

        foreach (var operatingHour in operatingHours)
        {
            operatingHour.ClinicId = clinicId;
            clinic.OperatingHours.Add(operatingHour);
        }

        foreach (var service in services)
        {
            service.ClinicId = clinicId;
            clinic.Services.Add(service);
        }

        foreach (var insurancePlan in insurancePlans)
        {
            insurancePlan.ClinicId = clinicId;
            clinic.InsurancePlans.Add(insurancePlan);
        }

        foreach (var accreditation in accreditations)
        {
            accreditation.ClinicId = clinicId;
            clinic.Accreditations.Add(accreditation);
        }

        _context.Clinics.Add(clinic);
        await _context.SaveChangesAsync();

        var createdClinic = await _context.Clinics
            .AsNoTracking()
            .Include(c => c.SysClinicType)
            .Include(c => c.SysOwnershipType)
            .Include(c => c.SysSourceSystem)
            .Include(c => c.OperatingHours)
            .Include(c => c.Services)
                .ThenInclude(s => s.SysOperation)
            .Include(c => c.InsurancePlans)
            .Include(c => c.Accreditations)
                .ThenInclude(a => a.SysAccreditation)
            .FirstOrDefaultAsync(c => c.ClinicId == clinicId);

        if (createdClinic == null)
        {
            return NotFound("Clinic could not be loaded after creation.");
        }

        return StatusCode(StatusCodes.Status201Created, MapClinic(createdClinic, 0));
    }

    [HttpPut("{clinicId:guid}")]
    [Authorize(Roles = SystemRoles.Admin)]
    public async Task<IActionResult> UpdateClinic(Guid clinicId, [FromBody] UpdateClinicRequest request)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        if (!TryBuildOperatingHours(clinicId, request.OperatingHours, out var operatingHours, out var operatingHoursError))
        {
            return BadRequest(operatingHoursError);
        }

        var lookups = await _systemInfoService.GetClinicSystemLookupsAsync(HttpContext.RequestAborted);
        var clinicTypeLookup = lookups.ClinicTypesById;
        if (clinicTypeLookup.Count == 0)
        {
            return Problem(
                title: "No clinic types configured",
                detail: "At least one active SysClinicType is required before updating clinics.",
                statusCode: StatusCodes.Status500InternalServerError);
        }

        if (!clinicTypeLookup.ContainsKey(request.SysClinicTypeId))
        {
            return BadRequest($"sysClinicTypeId '{request.SysClinicTypeId}' is invalid or inactive.");
        }

        var ownershipTypeLookup = lookups.OwnershipTypesById;
        if (ownershipTypeLookup.Count == 0)
        {
            return Problem(
                title: "No ownership types configured",
                detail: "At least one active SysOwnershipType is required before updating clinics.",
                statusCode: StatusCodes.Status500InternalServerError);
        }

        if (!ownershipTypeLookup.ContainsKey(request.SysOwnershipTypeId))
        {
            return BadRequest($"sysOwnershipTypeId '{request.SysOwnershipTypeId}' is invalid or inactive.");
        }

        var sourceSystemLookup = lookups.SourceSystemsById;
        if (sourceSystemLookup.Count == 0)
        {
            return Problem(
                title: "No source systems configured",
                detail: "At least one active SysSourceSystem is required before updating clinics.",
                statusCode: StatusCodes.Status500InternalServerError);
        }

        if (!sourceSystemLookup.ContainsKey(request.SysSourceSystemId))
        {
            return BadRequest($"sysSourceSystemId '{request.SysSourceSystemId}' is invalid or inactive.");
        }

        var operationLookup = lookups.OperationsById;
        if (operationLookup.Count == 0)
        {
            return Problem(
                title: "No service operations configured",
                detail: "At least one active SysOperation is required before saving clinic services.",
                statusCode: StatusCodes.Status500InternalServerError);
        }

        if (!TryBuildServices(clinicId, request.Services, operationLookup, out var services, out var servicesError))
        {
            return BadRequest(servicesError);
        }

        if (!TryBuildInsurancePlans(clinicId, request.InsurancePlans, out var insurancePlans, out var insurancePlansError))
        {
            return BadRequest(insurancePlansError);
        }

        var accreditationLookup = lookups.AccreditationsById;
        if (accreditationLookup.Count == 0)
        {
            return Problem(
                title: "No accreditation types configured",
                detail: "At least one active SysAccreditation is required before saving clinic accreditations.",
                statusCode: StatusCodes.Status500InternalServerError);
        }

        if (!TryBuildAccreditations(clinicId, request.Accreditations, accreditationLookup, out var accreditations, out var accreditationsError))
        {
            return BadRequest(accreditationsError);
        }

        var clinic = await _context.Clinics
            .FirstOrDefaultAsync(c => c.ClinicId == clinicId);

        if (clinic == null)
        {
            return NotFound("Clinic not found.");
        }

        var normalizedName = request.Name.Trim();
        var normalizedCode = NormalizeCode(request.Code);
        if (string.IsNullOrWhiteSpace(normalizedName) || string.IsNullOrWhiteSpace(normalizedCode))
        {
            return BadRequest("Clinic name and code are required.");
        }

        var duplicateName = await _context.Clinics.AnyAsync(c => c.ClinicId != clinicId && c.Name.ToLower() == normalizedName.ToLower());
        if (duplicateName)
        {
            return Conflict("A clinic with this name already exists.");
        }

        var duplicateCode = await _context.Clinics.AnyAsync(c => c.ClinicId != clinicId && c.Code.ToLower() == normalizedCode.ToLower());
        if (duplicateCode)
        {
            return Conflict("A clinic with this code already exists.");
        }

        clinic.Name = normalizedName;
        clinic.Code = normalizedCode;
        clinic.LegalName = NormalizeTextOrFallback(request.LegalName, normalizedName);
        clinic.SysClinicTypeId = request.SysClinicTypeId;
        clinic.SysOwnershipTypeId = request.SysOwnershipTypeId;
        clinic.FoundedOn = request.FoundedOn;
        clinic.NpiOrganization = NormalizeCodeLikeIdentifier(request.NpiOrganization);
        clinic.Ein = NormalizeText(request.Ein);
        clinic.TaxonomyCode = NormalizeCodeLikeIdentifier(request.TaxonomyCode);
        clinic.StateLicenseFacility = NormalizeCodeLikeIdentifier(request.StateLicenseFacility);
        clinic.CliaNumber = NormalizeCodeLikeIdentifier(request.CliaNumber);
        clinic.AddressLine1 = NormalizeText(request.AddressLine1);
        clinic.AddressLine2 = NormalizeText(request.AddressLine2);
        clinic.City = NormalizeText(request.City);
        clinic.State = NormalizeText(request.State);
        clinic.PostalCode = NormalizeText(request.PostalCode);
        clinic.CountryCode = NormalizeCountryCode(request.CountryCode);
        clinic.Timezone = NormalizeTextOrFallback(request.Timezone, "America/Chicago");
        clinic.MainPhone = NormalizeText(request.MainPhone);
        clinic.Fax = NormalizeText(request.Fax);
        clinic.MainEmail = NormalizeEmailOptional(request.MainEmail);
        clinic.WebsiteUrl = NormalizeText(request.WebsiteUrl);
        clinic.PatientPortalUrl = NormalizeText(request.PatientPortalUrl);
        clinic.BookingMethods = string.Join(',', NormalizeBookingMethods(request.BookingMethods));
        clinic.AvgNewPatientWaitDays = request.AvgNewPatientWaitDays;
        clinic.SameDayAvailable = request.SameDayAvailable;
        clinic.HipaaNoticeVersion = NormalizeText(request.HipaaNoticeVersion);
        clinic.LastSecurityRiskAssessmentOn = request.LastSecurityRiskAssessmentOn;
        clinic.SysSourceSystemId = request.SysSourceSystemId;
        clinic.UpdatedAtUtc = DateTime.UtcNow;
        clinic.IsActive = request.IsActive;

        foreach (var operatingHour in operatingHours)
        {
            operatingHour.ClinicId = clinicId;
        }

        foreach (var service in services)
        {
            service.ClinicId = clinicId;
        }

        foreach (var insurancePlan in insurancePlans)
        {
            insurancePlan.ClinicId = clinicId;
        }

        foreach (var accreditation in accreditations)
        {
            accreditation.ClinicId = clinicId;
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            await _context.ClinicOperatingHours
                .Where(h => h.ClinicId == clinicId)
                .ExecuteDeleteAsync();

            await _context.ClinicServices
                .Where(s => s.ClinicId == clinicId)
                .ExecuteDeleteAsync();

            await _context.ClinicInsurancePlans
                .Where(p => p.ClinicId == clinicId)
                .ExecuteDeleteAsync();

            await _context.ClinicAccreditations
                .Where(a => a.ClinicId == clinicId)
                .ExecuteDeleteAsync();

            _context.ClinicOperatingHours.AddRange(operatingHours);
            _context.ClinicServices.AddRange(services);
            _context.ClinicInsurancePlans.AddRange(insurancePlans);
            _context.ClinicAccreditations.AddRange(accreditations);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync();
            return Conflict("Clinic update could not be completed because the clinic was modified or removed. Refresh and try again.");
        }

        var updatedClinic = await _context.Clinics
            .AsNoTracking()
            .Include(c => c.SysClinicType)
            .Include(c => c.SysOwnershipType)
            .Include(c => c.SysSourceSystem)
            .Include(c => c.OperatingHours)
            .Include(c => c.Services)
                .ThenInclude(s => s.SysOperation)
            .Include(c => c.InsurancePlans)
            .Include(c => c.Accreditations)
                .ThenInclude(a => a.SysAccreditation)
            .FirstOrDefaultAsync(c => c.ClinicId == clinicId);

        if (updatedClinic == null)
        {
            return NotFound("Clinic not found after update.");
        }

        return Ok(MapClinic(updatedClinic, await _context.Users.CountAsync(u => u.ClinicId == updatedClinic.ClinicId)));
    }

    private static ClinicSummaryResponse MapClinic(Clinic clinic, int usersCount)
    {
        var bookingMethods = SplitBookingMethods(clinic.BookingMethods);

        return new ClinicSummaryResponse
        {
            ClinicId = clinic.ClinicId,
            Name = clinic.Name,
            Code = clinic.Code,
            LegalName = clinic.LegalName,
            SysClinicTypeId = clinic.SysClinicTypeId,
            ClinicType = clinic.SysClinicType?.Name ?? string.Empty,
            SysOwnershipTypeId = clinic.SysOwnershipTypeId,
            OwnershipType = clinic.SysOwnershipType?.Name ?? string.Empty,
            FoundedOn = clinic.FoundedOn,
            NpiOrganization = clinic.NpiOrganization,
            Ein = clinic.Ein,
            TaxonomyCode = clinic.TaxonomyCode,
            StateLicenseFacility = clinic.StateLicenseFacility,
            CliaNumber = clinic.CliaNumber,
            AddressLine1 = clinic.AddressLine1,
            AddressLine2 = clinic.AddressLine2,
            City = clinic.City,
            State = clinic.State,
            PostalCode = clinic.PostalCode,
            CountryCode = clinic.CountryCode,
            Timezone = clinic.Timezone,
            MainPhone = clinic.MainPhone,
            Fax = clinic.Fax,
            MainEmail = clinic.MainEmail,
            WebsiteUrl = clinic.WebsiteUrl,
            PatientPortalUrl = clinic.PatientPortalUrl,
            BookingMethods = bookingMethods,
            AvgNewPatientWaitDays = clinic.AvgNewPatientWaitDays,
            SameDayAvailable = clinic.SameDayAvailable,
            HipaaNoticeVersion = clinic.HipaaNoticeVersion,
            LastSecurityRiskAssessmentOn = clinic.LastSecurityRiskAssessmentOn,
            SysSourceSystemId = clinic.SysSourceSystemId,
            SourceSystem = clinic.SysSourceSystem?.Name ?? string.Empty,
            CreatedAtUtc = clinic.CreatedAtUtc,
            UpdatedAtUtc = clinic.UpdatedAtUtc,
            OperatingHours = clinic.OperatingHours
                .OrderBy(h => h.DayOfWeek)
                .Select(h => new ClinicOperatingHourContract
                {
                    DayOfWeek = h.DayOfWeek,
                    Open = h.OpenTime.HasValue ? FormatTime(h.OpenTime.Value) : null,
                    Close = h.CloseTime.HasValue ? FormatTime(h.CloseTime.Value) : null,
                    IsClosed = h.IsClosed
                })
                .ToList(),
            Services = clinic.Services
                .Where(s => s.IsActive && s.SysOperation != null && s.SysOperation.IsActive)
                .OrderBy(s => s.SysOperation.Name)
                .Select(s => new ClinicServiceContract
                {
                    SysOperationId = s.SysOperationId,
                    Name = s.SysOperation.Name,
                    IsTelehealthAvailable = s.IsTelehealthAvailable
                })
                .ToList(),
            InsurancePlans = clinic.InsurancePlans
                .Where(p => p.IsActive)
                .OrderBy(p => p.PayerName)
                .ThenBy(p => p.PlanName)
                .Select(p => new ClinicInsurancePlanContract
                {
                    PayerName = p.PayerName,
                    PlanName = p.PlanName,
                    IsInNetwork = p.IsInNetwork
                })
                .ToList(),
            Accreditations = clinic.Accreditations
                .Where(a => a.IsActive && a.SysAccreditation != null && a.SysAccreditation.IsActive)
                .OrderBy(a => a.SysAccreditation.Name)
                .Select(a => new ClinicAccreditationContract
                {
                    SysAccreditationId = a.SysAccreditationId,
                    Name = a.SysAccreditation.Name,
                    EffectiveOn = a.EffectiveOn,
                    ExpiresOn = a.ExpiresOn
                })
                .ToList(),
            IsActive = clinic.IsActive,
            UsersCount = usersCount
        };
    }

    private static bool TryBuildOperatingHours(
        Guid clinicId,
        IReadOnlyCollection<ClinicOperatingHourContract>? requestedHours,
        out List<ClinicOperatingHour> hours,
        out string error)
    {
        hours = [];
        error = string.Empty;

        var source = requestedHours ?? [];
        var duplicateDay = source
            .GroupBy(h => h.DayOfWeek)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .FirstOrDefault(-1);

        if (duplicateDay >= 0)
        {
            error = "Operating hours cannot contain duplicate dayOfWeek entries.";
            return false;
        }

        foreach (var entry in source)
        {
            if (entry.DayOfWeek < 0 || entry.DayOfWeek > 6)
            {
                error = "Operating dayOfWeek must be between 0 (Sunday) and 6 (Saturday).";
                return false;
            }

            var isClosed = entry.IsClosed;
            TimeSpan? openTime = null;
            TimeSpan? closeTime = null;

            if (!isClosed)
            {
                if (!TryParseTime(entry.Open, out var parsedOpen))
                {
                    error = $"Invalid open time for dayOfWeek {entry.DayOfWeek}. Expected HH:mm.";
                    return false;
                }

                if (!TryParseTime(entry.Close, out var parsedClose))
                {
                    error = $"Invalid close time for dayOfWeek {entry.DayOfWeek}. Expected HH:mm.";
                    return false;
                }

                if (parsedOpen >= parsedClose)
                {
                    error = $"Open time must be earlier than close time for dayOfWeek {entry.DayOfWeek}.";
                    return false;
                }

                openTime = parsedOpen;
                closeTime = parsedClose;
            }

            hours.Add(new ClinicOperatingHour
            {
                ClinicOperatingHourId = Guid.NewGuid(),
                ClinicId = clinicId,
                DayOfWeek = entry.DayOfWeek,
                OpenTime = openTime,
                CloseTime = closeTime,
                IsClosed = isClosed
            });
        }

        return true;
    }

    private static bool TryBuildServices(
        Guid clinicId,
        IReadOnlyCollection<ClinicServiceContract>? requestedServices,
        IReadOnlyDictionary<int, SysOperation> operationsById,
        out List<ClinicService> services,
        out string error)
    {
        services = [];
        error = string.Empty;

        var source = requestedServices ?? [];
        var usedOperationIds = new HashSet<int>();

        foreach (var entry in source)
        {
            if (entry.SysOperationId <= 0)
            {
                error = "Each service must include a valid sysOperationId.";
                return false;
            }

            if (!operationsById.ContainsKey(entry.SysOperationId))
            {
                error = $"sysOperationId '{entry.SysOperationId}' is invalid or inactive.";
                return false;
            }

            if (!usedOperationIds.Add(entry.SysOperationId))
            {
                error = $"Duplicate sysOperationId '{entry.SysOperationId}' is not allowed.";
                return false;
            }

            services.Add(new ClinicService
            {
                ClinicServiceId = Guid.NewGuid(),
                ClinicId = clinicId,
                SysOperationId = entry.SysOperationId,
                IsTelehealthAvailable = entry.IsTelehealthAvailable,
                IsActive = true
            });
        }

        return true;
    }

    private static bool TryBuildInsurancePlans(
        Guid clinicId,
        IReadOnlyCollection<ClinicInsurancePlanContract>? requestedPlans,
        out List<ClinicInsurancePlan> plans,
        out string error)
    {
        plans = [];
        error = string.Empty;

        var source = requestedPlans ?? [];
        var usedPlans = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in source)
        {
            var payer = NormalizeText(entry.PayerName);
            var plan = NormalizeText(entry.PlanName);
            if (string.IsNullOrWhiteSpace(payer) || string.IsNullOrWhiteSpace(plan))
            {
                error = "Each insurance plan must include a payerName and planName.";
                return false;
            }

            var key = $"{payer}|{plan}";
            if (!usedPlans.Add(key))
            {
                error = $"Duplicate insurance plan '{payer} - {plan}' is not allowed.";
                return false;
            }

            plans.Add(new ClinicInsurancePlan
            {
                ClinicInsurancePlanId = Guid.NewGuid(),
                ClinicId = clinicId,
                PayerName = payer,
                PlanName = plan,
                IsInNetwork = entry.IsInNetwork,
                IsActive = true
            });
        }

        return true;
    }

    private static bool TryBuildAccreditations(
        Guid clinicId,
        IReadOnlyCollection<ClinicAccreditationContract>? requestedAccreditations,
        IReadOnlyDictionary<int, SysAccreditation> accreditationsById,
        out List<ClinicAccreditation> accreditations,
        out string error)
    {
        accreditations = [];
        error = string.Empty;

        var source = requestedAccreditations ?? [];
        var usedAccreditationIds = new HashSet<int>();

        foreach (var entry in source)
        {
            if (entry.SysAccreditationId <= 0)
            {
                error = "Each accreditation must include a valid sysAccreditationId.";
                return false;
            }

            if (!accreditationsById.TryGetValue(entry.SysAccreditationId, out var accreditationType))
            {
                error = $"sysAccreditationId '{entry.SysAccreditationId}' is invalid or inactive.";
                return false;
            }

            if (!usedAccreditationIds.Add(entry.SysAccreditationId))
            {
                error = $"Duplicate accreditation type '{accreditationType.Name}' is not allowed.";
                return false;
            }

            if (entry.EffectiveOn.HasValue && entry.ExpiresOn.HasValue && entry.EffectiveOn.Value > entry.ExpiresOn.Value)
            {
                error = $"Accreditation '{accreditationType.Name}' has EffectiveOn after ExpiresOn.";
                return false;
            }

            accreditations.Add(new ClinicAccreditation
            {
                ClinicAccreditationId = Guid.NewGuid(),
                ClinicId = clinicId,
                SysAccreditationId = entry.SysAccreditationId,
                EffectiveOn = entry.EffectiveOn,
                ExpiresOn = entry.ExpiresOn,
                IsActive = true
            });
        }

        return true;
    }

    private static bool TryParseTime(string? value, out TimeSpan parsedTime)
    {
        parsedTime = default;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return TimeSpan.TryParseExact(value.Trim(), @"hh\:mm", CultureInfo.InvariantCulture, out parsedTime);
    }

    private static string FormatTime(TimeSpan value)
    {
        return value.ToString(@"hh\:mm", CultureInfo.InvariantCulture);
    }

    private static string NormalizeEmailOptional(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToLowerInvariant();
    }

    private static string NormalizeText(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    private static string NormalizeTextOrFallback(string value, string fallback)
    {
        var normalized = NormalizeText(value);
        return string.IsNullOrWhiteSpace(normalized) ? fallback : normalized;
    }

    private static string NormalizeCodeLikeIdentifier(string code)
    {
        return string.IsNullOrWhiteSpace(code)
            ? string.Empty
            : code.Replace(" ", string.Empty).Trim().ToUpperInvariant();
    }

    private static string NormalizeCountryCode(string countryCode)
    {
        var normalized = NormalizeCodeLikeIdentifier(countryCode);
        if (normalized.Length == 2)
        {
            return normalized;
        }

        return "US";
    }

    private static List<string> NormalizeBookingMethods(IReadOnlyCollection<string>? bookingMethods)
    {
        var source = bookingMethods ?? [];
        var normalized = source
            .Where(method => !string.IsNullOrWhiteSpace(method))
            .Select(method => method.Trim().ToLowerInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return normalized;
    }

    private static List<string> SplitBookingMethods(string bookingMethodsValue)
    {
        if (string.IsNullOrWhiteSpace(bookingMethodsValue))
        {
            return [];
        }

        return bookingMethodsValue
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string NormalizeCode(string code)
    {
        return code.Replace(" ", string.Empty).Trim().ToUpperInvariant();
    }
}
