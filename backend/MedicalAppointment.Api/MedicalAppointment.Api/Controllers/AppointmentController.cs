using MedicalAppointment.Api.Contracts.Appointments;
using MedicalAppointment.Api.Extensions;
using MedicalAppointment.Domain.Constants;
using MedicalAppointment.Domain.Models;
using MedicalAppointment.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AppointmentController : ControllerBase
{
    private const int AppointmentDurationMinutes = 30;
    private readonly AppDbContext _context;

    public AppointmentController(AppDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    [Authorize(Roles = SystemRoles.Patient)]
    public async Task<IActionResult> Create([FromBody] CreateAppointmentRequest request)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        if (!User.TryGetUserId(out var patientId) || !User.TryGetClinicId(out var clinicId))
            return Unauthorized("Invalid token subject.");

        if (request.AppointmentDateTime <= DateTime.UtcNow)
            return BadRequest("Appointment time must be in the future.");

        var patientExists = await _context.Users
            .Include(u => u.SysRole)
            .AnyAsync(u =>
                u.UserId == patientId &&
                u.ClinicId == clinicId &&
                u.SysRole != null &&
                u.SysRole.Name == SystemRoles.Patient &&
                u.SysRole.IsActive);

        if (!patientExists)
            return BadRequest("Patient account is invalid or inactive.");

        var doctorExists = await _context.Users
            .Include(u => u.SysRole)
            .AnyAsync(u =>
                u.UserId == request.DoctorId &&
                u.ClinicId == clinicId &&
                u.SysRole != null &&
                u.SysRole.Name == SystemRoles.Doctor &&
                u.SysRole.IsActive);

        if (!doctorExists)
            return BadRequest("Selected doctor is invalid, inactive, or from another clinic.");

        if (!await IsDoctorBookableAtDateTime(request.DoctorId, clinicId, request.AppointmentDateTime))
            return BadRequest("Selected doctor is unavailable at that time.");

        var doctorConflict = await HasDoctorConflict(request.DoctorId, request.AppointmentDateTime);

        if (doctorConflict)
            return Conflict("The selected doctor already has an overlapping appointment in that 30-minute timeframe.");

        var patientConflict = await HasPatientConflict(patientId, request.AppointmentDateTime);

        if (patientConflict)
            return Conflict("You already have an overlapping appointment in that 30-minute timeframe.");

        var appointment = new Appointment
        {
            AppointmentId = Guid.NewGuid(),
            DoctorId = request.DoctorId,
            PatientId = patientId,
            ClinicId = clinicId,
            AppointmentDateTime = request.AppointmentDateTime,
            Status = AppointmentStatuses.Scheduled,
            PostponeRequestStatus = AppointmentPostponeStatuses.None
        };

        _context.Appointments.Add(appointment);
        AddAppointmentAuditEvent(
            appointment.AppointmentId,
            appointment.ClinicId,
            patientId,
            SystemRoles.Patient,
            AppointmentAuditEventTypes.Created,
            $"Appointment created for {NormalizeToUtc(appointment.AppointmentDateTime):O}.");
        await _context.SaveChangesAsync();

        return Ok(new
        {
            appointment.AppointmentId,
            appointment.Status,
            appointment.DoctorId,
            appointment.PatientId,
            appointment.ClinicId,
            appointment.AppointmentDateTime
        });
    }

    [HttpGet("doctor-availability")]
    [Authorize(Roles = SystemRoles.Doctor + "," + SystemRoles.Admin)]
    public async Task<IActionResult> GetDoctorAvailability([FromQuery] Guid? doctorId = null)
    {
        if (!User.TryGetUserId(out var callerUserId))
        {
            return Unauthorized("Invalid token subject.");
        }

        var callerRole = User.FindFirstValue(ClaimTypes.Role);
        Guid targetDoctorId;

        if (string.Equals(callerRole, SystemRoles.Admin, StringComparison.OrdinalIgnoreCase))
        {
            if (!doctorId.HasValue || doctorId.Value == Guid.Empty)
            {
                return BadRequest("doctorId is required for admin requests.");
            }

            targetDoctorId = doctorId.Value;
        }
        else if (string.Equals(callerRole, SystemRoles.Doctor, StringComparison.OrdinalIgnoreCase))
        {
            if (doctorId.HasValue && doctorId.Value != callerUserId)
            {
                return Forbid();
            }

            targetDoctorId = callerUserId;
        }
        else
        {
            return Forbid();
        }

        var doctor = await _context.Users
            .AsNoTracking()
            .Include(u => u.SysRole)
            .Include(u => u.Clinic)
            .FirstOrDefaultAsync(u =>
                u.UserId == targetDoctorId &&
                u.SysRole != null &&
                u.SysRole.Name == SystemRoles.Doctor &&
                u.SysRole.IsActive &&
                u.Clinic.IsActive);

        if (doctor == null)
        {
            return NotFound("Doctor not found, inactive, or assigned to an inactive clinic.");
        }

        var response = await BuildDoctorAvailabilityResponse(doctor.UserId, doctor.ClinicId, doctor.Clinic.Timezone);
        return Ok(response);
    }

    [HttpPut("doctor-availability")]
    [Authorize(Roles = SystemRoles.Doctor + "," + SystemRoles.Admin)]
    public async Task<IActionResult> UpsertDoctorAvailability([FromBody] UpsertDoctorAvailabilityRequest request, [FromQuery] Guid? doctorId = null)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        if (!User.TryGetUserId(out var callerUserId))
        {
            return Unauthorized("Invalid token subject.");
        }

        var callerRole = User.FindFirstValue(ClaimTypes.Role);
        Guid targetDoctorId;

        if (string.Equals(callerRole, SystemRoles.Admin, StringComparison.OrdinalIgnoreCase))
        {
            if (!doctorId.HasValue || doctorId.Value == Guid.Empty)
            {
                return BadRequest("doctorId is required for admin requests.");
            }

            targetDoctorId = doctorId.Value;
        }
        else if (string.Equals(callerRole, SystemRoles.Doctor, StringComparison.OrdinalIgnoreCase))
        {
            if (doctorId.HasValue && doctorId.Value != callerUserId)
            {
                return Forbid();
            }

            targetDoctorId = callerUserId;
        }
        else
        {
            return Forbid();
        }

        var doctor = await _context.Users
            .AsNoTracking()
            .Include(u => u.SysRole)
            .Include(u => u.Clinic)
            .FirstOrDefaultAsync(u =>
                u.UserId == targetDoctorId &&
                u.SysRole != null &&
                u.SysRole.Name == SystemRoles.Doctor &&
                u.SysRole.IsActive &&
                u.Clinic.IsActive);

        if (doctor == null)
        {
            return NotFound("Doctor not found, inactive, or assigned to an inactive clinic.");
        }

        if (!TryBuildWeeklyWindows(targetDoctorId, request.WeeklyAvailability, out var weeklyWindows, out var windowsError))
        {
            return BadRequest(windowsError);
        }

        if (!TryBuildWeeklyBreaks(targetDoctorId, request.WeeklyBreaks, out var weeklyBreaks, out var breaksError))
        {
            return BadRequest(breaksError);
        }

        if (!TryBuildOverrides(targetDoctorId, request.Overrides, out var overrides, out var overridesError))
        {
            return BadRequest(overridesError);
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();
        await _context.DoctorAvailabilityWindows
            .Where(x => x.DoctorId == targetDoctorId)
            .ExecuteDeleteAsync();
        await _context.DoctorAvailabilityBreaks
            .Where(x => x.DoctorId == targetDoctorId)
            .ExecuteDeleteAsync();
        await _context.DoctorAvailabilityOverrides
            .Where(x => x.DoctorId == targetDoctorId)
            .ExecuteDeleteAsync();

        _context.DoctorAvailabilityWindows.AddRange(weeklyWindows);
        _context.DoctorAvailabilityBreaks.AddRange(weeklyBreaks);
        _context.DoctorAvailabilityOverrides.AddRange(overrides);
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        var response = await BuildDoctorAvailabilityResponse(doctor.UserId, doctor.ClinicId, doctor.Clinic.Timezone);
        return Ok(response);
    }

    [HttpGet("available-slots")]
    [Authorize(Roles = SystemRoles.Patient + "," + SystemRoles.Doctor + "," + SystemRoles.Admin)]
    public async Task<IActionResult> GetAvailableSlots([FromQuery] Guid doctorId, [FromQuery] DateOnly date)
    {
        if (doctorId == Guid.Empty)
        {
            return BadRequest("doctorId is required.");
        }

        if (date == default)
        {
            return BadRequest("date is required (format: yyyy-MM-dd).");
        }

        if (!User.TryGetUserId(out var callerUserId))
        {
            return Unauthorized("Invalid token subject.");
        }

        var callerRole = User.FindFirstValue(ClaimTypes.Role);

        var doctor = await _context.Users
            .AsNoTracking()
            .Include(u => u.SysRole)
            .Include(u => u.Clinic)
            .FirstOrDefaultAsync(u =>
                u.UserId == doctorId &&
                u.SysRole != null &&
                u.SysRole.Name == SystemRoles.Doctor &&
                u.SysRole.IsActive &&
                u.Clinic.IsActive);

        if (doctor == null)
        {
            return NotFound("Doctor not found, inactive, or assigned to an inactive clinic.");
        }

        if (!string.Equals(callerRole, SystemRoles.Admin, StringComparison.OrdinalIgnoreCase))
        {
            if (!User.TryGetClinicId(out var callerClinicId))
            {
                return Unauthorized("Invalid clinic claim in token.");
            }

            if (callerClinicId != doctor.ClinicId)
            {
                return Forbid();
            }
        }

        var clinic = await _context.Clinics
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.ClinicId == doctor.ClinicId && c.IsActive);

        if (clinic == null)
        {
            return BadRequest("Doctor clinic is inactive or unavailable.");
        }

        var clinicTimeZone = ResolveClinicTimeZone(clinic.Timezone);
        var operatingHour = await _context.ClinicOperatingHours
            .AsNoTracking()
            .FirstOrDefaultAsync(h => h.ClinicId == clinic.ClinicId && h.DayOfWeek == (int)date.DayOfWeek);

        if (operatingHour == null || operatingHour.IsClosed || !operatingHour.OpenTime.HasValue || !operatingHour.CloseTime.HasValue)
        {
            return Ok(new
            {
                doctorId = doctor.UserId,
                clinicId = clinic.ClinicId,
                date = date.ToString("yyyy-MM-dd"),
                timezone = clinic.Timezone,
                slotDurationMinutes = AppointmentDurationMinutes,
                slots = Array.Empty<object>()
            });
        }

        var dayStartLocal = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        var clinicOpenLocal = dayStartLocal.Add(operatingHour.OpenTime.Value);
        var clinicCloseLocal = dayStartLocal.Add(operatingHour.CloseTime.Value);

        if (clinicOpenLocal >= clinicCloseLocal)
        {
            return BadRequest("Clinic operating hours are invalid for the selected date.");
        }

        var clinicOpenUtc = TryConvertLocalToUtc(clinicOpenLocal, clinicTimeZone);
        var clinicCloseUtc = TryConvertLocalToUtc(clinicCloseLocal, clinicTimeZone);
        if (!clinicOpenUtc.HasValue || !clinicCloseUtc.HasValue || clinicOpenUtc.Value >= clinicCloseUtc.Value)
        {
            return BadRequest("Clinic timezone configuration prevented slot generation.");
        }

        var weeklyAvailability = await _context.DoctorAvailabilityWindows
            .AsNoTracking()
            .Where(x =>
                x.DoctorId == doctor.UserId &&
                x.IsActive &&
                x.DayOfWeek == (int)date.DayOfWeek)
            .OrderBy(x => x.StartTime)
            .ToListAsync();

        var weeklyBreaks = await _context.DoctorAvailabilityBreaks
            .AsNoTracking()
            .Where(x =>
                x.DoctorId == doctor.UserId &&
                x.IsActive &&
                x.DayOfWeek == (int)date.DayOfWeek)
            .OrderBy(x => x.StartTime)
            .ToListAsync();

        var overrides = await _context.DoctorAvailabilityOverrides
            .AsNoTracking()
            .Where(x =>
                x.DoctorId == doctor.UserId &&
                x.IsActive &&
                x.Date == date)
            .OrderBy(x => x.StartTime)
            .ToListAsync();

        var bookableWindows = BuildDoctorBookableWindowsLocal(
            dayStartLocal,
            clinicOpenLocal,
            clinicCloseLocal,
            weeklyAvailability,
            weeklyBreaks,
            overrides);

        if (bookableWindows.Count == 0)
        {
            return Ok(new
            {
                doctorId = doctor.UserId,
                clinicId = clinic.ClinicId,
                date = date.ToString("yyyy-MM-dd"),
                timezone = clinic.Timezone,
                slotDurationMinutes = AppointmentDurationMinutes,
                slots = Array.Empty<object>()
            });
        }

        var rangeStartUtc = clinicOpenUtc.Value.AddMinutes(-AppointmentDurationMinutes);
        var rangeEndUtc = clinicCloseUtc.Value;

        var doctorBusyStarts = await _context.Appointments
            .AsNoTracking()
            .Where(a =>
                a.ClinicId == clinic.ClinicId &&
                a.DoctorId == doctor.UserId &&
                a.Status == AppointmentStatuses.Scheduled &&
                a.AppointmentDateTime >= rangeStartUtc &&
                a.AppointmentDateTime < rangeEndUtc)
            .Select(a => a.AppointmentDateTime)
            .ToListAsync();

        List<DateTime> patientBusyStarts = [];
        if (string.Equals(callerRole, SystemRoles.Patient, StringComparison.OrdinalIgnoreCase))
        {
            patientBusyStarts = await _context.Appointments
                .AsNoTracking()
                .Where(a =>
                    a.ClinicId == clinic.ClinicId &&
                    a.PatientId == callerUserId &&
                    a.Status == AppointmentStatuses.Scheduled &&
                    a.AppointmentDateTime >= rangeStartUtc &&
                    a.AppointmentDateTime < rangeEndUtc)
                .Select(a => a.AppointmentDateTime)
                .ToListAsync();
        }

        var nowUtc = DateTime.UtcNow;
        var availableSlots = new List<DateTime>();
        foreach (var window in bookableWindows)
        {
            var slotLocal = window.Start;
            while (slotLocal.AddMinutes(AppointmentDurationMinutes) <= window.End)
            {
                var slotUtc = TryConvertLocalToUtc(slotLocal, clinicTimeZone);
                if (slotUtc.HasValue)
                {
                    var slotStartUtc = slotUtc.Value;
                    if (slotStartUtc > nowUtc &&
                        !IsSlotOverlapping(slotStartUtc, doctorBusyStarts) &&
                        !IsSlotOverlapping(slotStartUtc, patientBusyStarts))
                    {
                        availableSlots.Add(slotStartUtc);
                    }
                }

                slotLocal = slotLocal.AddMinutes(AppointmentDurationMinutes);
            }
        }

        return Ok(new
        {
            doctorId = doctor.UserId,
            clinicId = clinic.ClinicId,
            date = date.ToString("yyyy-MM-dd"),
            timezone = clinic.Timezone,
            slotDurationMinutes = AppointmentDurationMinutes,
            slots = availableSlots
                .OrderBy(slot => slot)
                .Select(slotUtc => new
                {
                    slotUtc,
                    localTime = TimeZoneInfo.ConvertTimeFromUtc(slotUtc, clinicTimeZone).ToString("HH:mm")
                })
                .ToList()
        });
    }

    [HttpPost("{appointmentId:guid}/postpone-request")]
    [Authorize(Roles = SystemRoles.Patient)]
    public async Task<IActionResult> RequestPostpone(Guid appointmentId, [FromBody] RequestPostponeAppointmentRequest request)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        if (!User.TryGetUserId(out var patientId) || !User.TryGetClinicId(out var clinicId))
            return Unauthorized("Invalid token subject.");

        var appointment = await _context.Appointments
            .Include(a => a.Doctor)
            .ThenInclude(d => d.Person)
            .Include(a => a.Clinic)
            .FirstOrDefaultAsync(a =>
                a.AppointmentId == appointmentId &&
                a.PatientId == patientId &&
                a.ClinicId == clinicId);

        if (appointment == null)
            return NotFound("Appointment not found.");

        if (appointment.Status != AppointmentStatuses.Scheduled)
            return BadRequest("Only scheduled appointments can be postponed.");

        if (appointment.PostponeRequestStatus == AppointmentPostponeStatuses.CounterProposed)
            return BadRequest("Respond to the doctor's counter-proposal before creating a new postpone request.");

        if (appointment.AppointmentDateTime <= DateTime.UtcNow)
            return BadRequest("Only future appointments can be postponed.");

        if (request.ProposedDateTime <= DateTime.UtcNow)
            return BadRequest("The proposed appointment time must be in the future.");

        if (request.ProposedDateTime == appointment.AppointmentDateTime)
            return BadRequest("Proposed date must be different from current appointment date.");

        if (!await IsDoctorBookableAtDateTime(appointment.DoctorId, appointment.ClinicId, request.ProposedDateTime))
            return BadRequest("Selected doctor is unavailable at the proposed time.");

        if (await HasDoctorConflict(appointment.DoctorId, request.ProposedDateTime, appointmentId))
            return Conflict("Doctor already has an overlapping appointment in that proposed 30-minute timeframe.");

        if (await HasPatientConflict(patientId, request.ProposedDateTime, appointmentId))
            return Conflict("You already have another overlapping appointment in that proposed 30-minute timeframe.");

        appointment.PostponeRequestStatus = AppointmentPostponeStatuses.Pending;
        appointment.ProposedDateTime = request.ProposedDateTime;
        appointment.PostponeReason = request.Reason.Trim();
        appointment.PostponeRequestedAtUtc = DateTime.UtcNow;
        appointment.DoctorResponseNote = null;
        appointment.DoctorRespondedAtUtc = null;
        appointment.PatientRespondedAtUtc = null;
        var proposedUtc = NormalizeToUtc(request.ProposedDateTime);

        AddAppointmentAuditEvent(
            appointment.AppointmentId,
            appointment.ClinicId,
            patientId,
            SystemRoles.Patient,
            AppointmentAuditEventTypes.PostponeRequested,
            $"Patient requested postponement to {proposedUtc:O}. Reason: {appointment.PostponeReason ?? string.Empty}");

        QueueNotification(
            appointment.DoctorId,
            appointment.AppointmentId,
            patientId,
            NotificationTypes.PostponeRequested,
            "Postpone request received",
            $"A patient requested to move this appointment to {proposedUtc:yyyy-MM-dd HH:mm} UTC.");

        await _context.SaveChangesAsync();

        return Ok(new
        {
            appointment.AppointmentId,
            appointment.Status,
            appointment.AppointmentDateTime,
            appointment.PostponeRequestStatus,
            appointment.ProposedDateTime,
            appointment.PostponeReason,
            appointment.PostponeRequestedAtUtc,
            appointment.DoctorResponseNote,
            appointment.DoctorRespondedAtUtc,
            appointment.PatientRespondedAtUtc,
            appointment.CancelledAtUtc,
            appointment.CancelledByUserId,
            appointment.CancellationReason,
            DoctorName = $"{appointment.Doctor.Person.FirstName} {appointment.Doctor.Person.LastName}".Trim(),
            ClinicName = appointment.Clinic.Name
        });
    }

    [HttpPost("{appointmentId:guid}/postpone-response")]
    [Authorize(Roles = SystemRoles.Doctor)]
    public async Task<IActionResult> RespondToPostponeRequest(Guid appointmentId, [FromBody] RespondPostponeAppointmentRequest request)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        if (!User.TryGetUserId(out var doctorId) || !User.TryGetClinicId(out var clinicId))
            return Unauthorized("Invalid token subject.");

        var appointment = await _context.Appointments
            .Include(a => a.Patient)
            .ThenInclude(p => p.Person)
            .Include(a => a.Clinic)
            .FirstOrDefaultAsync(a =>
                a.AppointmentId == appointmentId &&
                a.DoctorId == doctorId &&
                a.ClinicId == clinicId);

        if (appointment == null)
            return NotFound("Appointment not found.");

        if (appointment.Status != AppointmentStatuses.Scheduled)
            return BadRequest("Only scheduled appointments can be updated.");

        if (appointment.PostponeRequestStatus != AppointmentPostponeStatuses.Pending)
            return BadRequest("Only pending postpone requests can be reviewed.");

        if (!appointment.ProposedDateTime.HasValue)
            return BadRequest("Pending request has no proposed date.");

        var now = DateTime.UtcNow;
        if (appointment.AppointmentDateTime <= now)
            return BadRequest("Only future appointments can be updated.");

        var decision = NormalizeText(request.Decision);
        var note = CleanOptionalText(request.Note);
        string auditEventType;
        string auditDetails;
        string notificationType;
        string notificationTitle;
        string notificationMessage;

        if (decision is "approve" or "approved")
        {
            var approvedDateTime = appointment.ProposedDateTime.Value;
            if (approvedDateTime <= now)
                return BadRequest("The proposed appointment time is no longer in the future.");

            if (!await IsDoctorBookableAtDateTime(appointment.DoctorId, appointment.ClinicId, approvedDateTime))
                return BadRequest("Selected doctor is unavailable at the proposed time.");

            if (await HasDoctorConflict(appointment.DoctorId, approvedDateTime, appointmentId))
                return Conflict("Doctor already has an overlapping appointment in that proposed 30-minute timeframe.");

            if (await HasPatientConflict(appointment.PatientId, approvedDateTime, appointmentId))
                return Conflict("Patient already has another overlapping appointment in that proposed 30-minute timeframe.");

            appointment.AppointmentDateTime = approvedDateTime;
            appointment.PostponeRequestStatus = AppointmentPostponeStatuses.Approved;
            var approvedUtc = NormalizeToUtc(approvedDateTime);
            auditEventType = AppointmentAuditEventTypes.PostponeApprovedByDoctor;
            auditDetails = $"Doctor approved postponement. Appointment moved to {approvedUtc:O}.";
            notificationType = NotificationTypes.PostponeApproved;
            notificationTitle = "Postpone request approved";
            notificationMessage = $"Your doctor approved the postpone request. New time: {approvedUtc:yyyy-MM-dd HH:mm} UTC.";
        }
        else if (decision is "reject" or "rejected")
        {
            if (string.IsNullOrWhiteSpace(note))
                return BadRequest("Reject reason is required.");

            if (note.Length < 5)
                return BadRequest("Reject reason must be at least 5 characters.");

            appointment.PostponeRequestStatus = AppointmentPostponeStatuses.Rejected;
            auditEventType = AppointmentAuditEventTypes.PostponeRejectedByDoctor;
            auditDetails = $"Doctor rejected postponement request. Note: {note}";
            notificationType = NotificationTypes.PostponeRejected;
            notificationTitle = "Postpone request rejected";
            notificationMessage = "Your doctor rejected the postpone request.";
        }
        else if (decision is "counter" or "counterpropose" or "counter-propose" or "counterproposed")
        {
            if (!request.CounterProposedDateTime.HasValue)
                return BadRequest("Counter-proposed appointment time is required.");

            var counterDateTime = request.CounterProposedDateTime.Value;
            if (counterDateTime <= now)
                return BadRequest("Counter-proposed appointment time must be in the future.");

            if (counterDateTime == appointment.AppointmentDateTime)
                return BadRequest("Counter-proposed date must be different from current appointment date.");

            if (!await IsDoctorBookableAtDateTime(appointment.DoctorId, appointment.ClinicId, counterDateTime))
                return BadRequest("Selected doctor is unavailable at the counter-proposed time.");

            if (await HasDoctorConflict(appointment.DoctorId, counterDateTime, appointmentId))
                return Conflict("Doctor already has an overlapping appointment in that counter-proposed 30-minute timeframe.");

            if (await HasPatientConflict(appointment.PatientId, counterDateTime, appointmentId))
                return Conflict("Patient already has another overlapping appointment in that counter-proposed 30-minute timeframe.");

            appointment.ProposedDateTime = counterDateTime;
            appointment.PostponeRequestStatus = AppointmentPostponeStatuses.CounterProposed;
            var counterUtc = NormalizeToUtc(counterDateTime);
            auditEventType = AppointmentAuditEventTypes.PostponeCounterProposedByDoctor;
            auditDetails = $"Doctor counter-proposed appointment time {counterUtc:O}. Note: {note ?? string.Empty}";
            notificationType = NotificationTypes.PostponeCounterProposed;
            notificationTitle = "Doctor sent a counter proposal";
            notificationMessage = $"Your doctor proposed a new time: {counterUtc:yyyy-MM-dd HH:mm} UTC.";
        }
        else
        {
            return BadRequest("Decision must be Approve, Reject, or CounterPropose.");
        }

        appointment.DoctorResponseNote = note;
        appointment.DoctorRespondedAtUtc = now;
        appointment.PatientRespondedAtUtc = null;
        AddAppointmentAuditEvent(
            appointment.AppointmentId,
            appointment.ClinicId,
            doctorId,
            SystemRoles.Doctor,
            auditEventType,
            auditDetails);
        QueueNotification(
            appointment.PatientId,
            appointment.AppointmentId,
            doctorId,
            notificationType,
            notificationTitle,
            notificationMessage);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            appointment.AppointmentId,
            appointment.Status,
            appointment.AppointmentDateTime,
            appointment.PostponeRequestStatus,
            appointment.ProposedDateTime,
            appointment.PostponeReason,
            appointment.PostponeRequestedAtUtc,
            appointment.DoctorResponseNote,
            appointment.DoctorRespondedAtUtc,
            appointment.PatientRespondedAtUtc,
            appointment.CancelledAtUtc,
            appointment.CancelledByUserId,
            appointment.CancellationReason,
            PatientName = $"{appointment.Patient.Person.FirstName} {appointment.Patient.Person.LastName}".Trim(),
            ClinicName = appointment.Clinic.Name
        });
    }

    [HttpPost("{appointmentId:guid}/postpone-counter-response")]
    [Authorize(Roles = SystemRoles.Patient)]
    public async Task<IActionResult> RespondToCounterPostpone(Guid appointmentId, [FromBody] RespondToCounterPostponeRequest request)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        if (!User.TryGetUserId(out var patientId) || !User.TryGetClinicId(out var clinicId))
            return Unauthorized("Invalid token subject.");

        var appointment = await _context.Appointments
            .Include(a => a.Doctor)
            .ThenInclude(d => d.Person)
            .Include(a => a.Clinic)
            .FirstOrDefaultAsync(a =>
                a.AppointmentId == appointmentId &&
                a.PatientId == patientId &&
                a.ClinicId == clinicId);

        if (appointment == null)
            return NotFound("Appointment not found.");

        if (appointment.Status != AppointmentStatuses.Scheduled)
            return BadRequest("Only scheduled appointments can be updated.");

        if (appointment.PostponeRequestStatus != AppointmentPostponeStatuses.CounterProposed)
            return BadRequest("No doctor counter-proposal is awaiting your response.");

        if (!appointment.ProposedDateTime.HasValue)
            return BadRequest("Counter-proposal has no proposed date.");

        var now = DateTime.UtcNow;
        if (appointment.AppointmentDateTime <= now)
            return BadRequest("Only future appointments can be updated.");

        var decision = NormalizeText(request.Decision);
        string auditEventType;
        string auditDetails;
        string notificationType;
        string notificationTitle;
        string notificationMessage;
        if (decision is "accept" or "accepted")
        {
            var acceptedDateTime = appointment.ProposedDateTime.Value;
            if (acceptedDateTime <= now)
                return BadRequest("Counter-proposed appointment time is no longer in the future.");

            if (!await IsDoctorBookableAtDateTime(appointment.DoctorId, appointment.ClinicId, acceptedDateTime))
                return BadRequest("Selected doctor is unavailable at the counter-proposed time.");

            if (await HasDoctorConflict(appointment.DoctorId, acceptedDateTime, appointmentId))
                return Conflict("Doctor already has an overlapping appointment in that counter-proposed 30-minute timeframe.");

            if (await HasPatientConflict(appointment.PatientId, acceptedDateTime, appointmentId))
                return Conflict("You already have another overlapping appointment in that counter-proposed 30-minute timeframe.");

            appointment.AppointmentDateTime = acceptedDateTime;
            appointment.PostponeRequestStatus = AppointmentPostponeStatuses.Approved;
            var acceptedUtc = NormalizeToUtc(acceptedDateTime);
            auditEventType = AppointmentAuditEventTypes.PostponeCounterAcceptedByPatient;
            auditDetails = $"Patient accepted counter-proposed appointment time {acceptedUtc:O}.";
            notificationType = NotificationTypes.PostponeCounterAccepted;
            notificationTitle = "Counter proposal accepted";
            notificationMessage = $"The patient accepted your counter proposal. New time: {acceptedUtc:yyyy-MM-dd HH:mm} UTC.";
        }
        else if (decision is "reject" or "rejected")
        {
            appointment.PostponeRequestStatus = AppointmentPostponeStatuses.Rejected;
            auditEventType = AppointmentAuditEventTypes.PostponeCounterRejectedByPatient;
            auditDetails = "Patient rejected the doctor's counter proposal.";
            notificationType = NotificationTypes.PostponeCounterRejected;
            notificationTitle = "Counter proposal rejected";
            notificationMessage = "The patient rejected your counter proposal.";
        }
        else
        {
            return BadRequest("Decision must be Accept or Reject.");
        }

        appointment.PatientRespondedAtUtc = now;
        AddAppointmentAuditEvent(
            appointment.AppointmentId,
            appointment.ClinicId,
            patientId,
            SystemRoles.Patient,
            auditEventType,
            auditDetails);
        QueueNotification(
            appointment.DoctorId,
            appointment.AppointmentId,
            patientId,
            notificationType,
            notificationTitle,
            notificationMessage);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            appointment.AppointmentId,
            appointment.Status,
            appointment.AppointmentDateTime,
            appointment.PostponeRequestStatus,
            appointment.ProposedDateTime,
            appointment.PostponeReason,
            appointment.PostponeRequestedAtUtc,
            appointment.DoctorResponseNote,
            appointment.DoctorRespondedAtUtc,
            appointment.PatientRespondedAtUtc,
            appointment.CancelledAtUtc,
            appointment.CancelledByUserId,
            appointment.CancellationReason,
            DoctorName = $"{appointment.Doctor.Person.FirstName} {appointment.Doctor.Person.LastName}".Trim(),
            ClinicName = appointment.Clinic.Name
        });
    }

    [HttpPost("{appointmentId:guid}/cancel")]
    [Authorize(Roles = SystemRoles.Patient + "," + SystemRoles.Doctor + "," + SystemRoles.Admin)]
    public async Task<IActionResult> CancelAppointment(Guid appointmentId, [FromBody] CancelAppointmentRequest request)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        if (!User.TryGetUserId(out var actorUserId))
        {
            return Unauthorized("Invalid token subject.");
        }

        var actorRole = User.FindFirstValue(ClaimTypes.Role);
        var query = _context.Appointments
            .Include(a => a.Patient).ThenInclude(p => p.Person)
            .Include(a => a.Doctor).ThenInclude(d => d.Person)
            .Include(a => a.Clinic)
            .AsQueryable();

        if (string.Equals(actorRole, SystemRoles.Patient, StringComparison.OrdinalIgnoreCase))
        {
            if (!User.TryGetClinicId(out var clinicId))
            {
                return Unauthorized("Invalid clinic claim in token.");
            }

            query = query.Where(a => a.PatientId == actorUserId && a.ClinicId == clinicId);
        }
        else if (string.Equals(actorRole, SystemRoles.Doctor, StringComparison.OrdinalIgnoreCase))
        {
            if (!User.TryGetClinicId(out var clinicId))
            {
                return Unauthorized("Invalid clinic claim in token.");
            }

            query = query.Where(a => a.DoctorId == actorUserId && a.ClinicId == clinicId);
        }
        else if (!string.Equals(actorRole, SystemRoles.Admin, StringComparison.OrdinalIgnoreCase))
        {
            return Forbid();
        }

        var appointment = await query.FirstOrDefaultAsync(a => a.AppointmentId == appointmentId);
        if (appointment == null)
        {
            return NotFound("Appointment not found.");
        }

        if (appointment.Status == AppointmentStatuses.Cancelled)
        {
            return BadRequest("Appointment is already cancelled.");
        }

        if (appointment.Status is AppointmentStatuses.Completed or AppointmentStatuses.NoShow)
        {
            return BadRequest("Completed appointments cannot be cancelled.");
        }

        if (appointment.AppointmentDateTime <= DateTime.UtcNow)
        {
            return BadRequest("Only future appointments can be cancelled.");
        }

        appointment.Status = AppointmentStatuses.Cancelled;
        appointment.CancelledAtUtc = DateTime.UtcNow;
        appointment.CancelledByUserId = actorUserId;
        var cancellationReason = request.Reason.Trim();
        appointment.CancellationReason = cancellationReason;
        appointment.PostponeRequestStatus = AppointmentPostponeStatuses.None;
        appointment.ProposedDateTime = null;
        appointment.PostponeReason = null;
        appointment.PostponeRequestedAtUtc = null;
        appointment.DoctorResponseNote = null;
        appointment.DoctorRespondedAtUtc = null;
        appointment.PatientRespondedAtUtc = null;

        var actorRoleLabel = string.IsNullOrWhiteSpace(actorRole) ? "Unknown" : actorRole.Trim();
        AddAppointmentAuditEvent(
            appointment.AppointmentId,
            appointment.ClinicId,
            actorUserId,
            actorRole,
            AppointmentAuditEventTypes.Cancelled,
            $"Appointment cancelled by {actorRoleLabel}. Reason: {cancellationReason}");

        var appointmentTimeUtc = NormalizeToUtc(appointment.AppointmentDateTime);
        var cancellationMessage = $"Appointment scheduled for {appointmentTimeUtc:yyyy-MM-dd HH:mm} UTC was cancelled.";

        if (string.Equals(actorRole, SystemRoles.Patient, StringComparison.OrdinalIgnoreCase))
        {
            QueueNotification(
                appointment.DoctorId,
                appointment.AppointmentId,
                actorUserId,
                NotificationTypes.AppointmentCancelled,
                "Appointment cancelled",
                cancellationMessage);
        }
        else if (string.Equals(actorRole, SystemRoles.Doctor, StringComparison.OrdinalIgnoreCase))
        {
            QueueNotification(
                appointment.PatientId,
                appointment.AppointmentId,
                actorUserId,
                NotificationTypes.AppointmentCancelled,
                "Appointment cancelled",
                cancellationMessage);
        }
        else if (string.Equals(actorRole, SystemRoles.Admin, StringComparison.OrdinalIgnoreCase))
        {
            if (appointment.PatientId != actorUserId)
            {
                QueueNotification(
                    appointment.PatientId,
                    appointment.AppointmentId,
                    actorUserId,
                    NotificationTypes.AppointmentCancelled,
                    "Appointment cancelled",
                    cancellationMessage);
            }

            if (appointment.DoctorId != actorUserId)
            {
                QueueNotification(
                    appointment.DoctorId,
                    appointment.AppointmentId,
                    actorUserId,
                    NotificationTypes.AppointmentCancelled,
                    "Appointment cancelled",
                    cancellationMessage);
            }
        }

        await _context.SaveChangesAsync();

        return Ok(new
        {
            appointment.AppointmentId,
            appointment.Status,
            appointment.AppointmentDateTime,
            appointment.CancelledAtUtc,
            appointment.CancelledByUserId,
            appointment.CancellationReason,
            PatientName = $"{appointment.Patient.Person.FirstName} {appointment.Patient.Person.LastName}".Trim(),
            DoctorName = $"{appointment.Doctor.Person.FirstName} {appointment.Doctor.Person.LastName}".Trim(),
            ClinicName = appointment.Clinic.Name
        });
    }

    [HttpPost("{appointmentId:guid}/attendance")]
    [Authorize(Roles = SystemRoles.Doctor + "," + SystemRoles.Admin)]
    public async Task<IActionResult> UpdateAttendance(Guid appointmentId, [FromBody] UpdateAppointmentAttendanceRequest request)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        if (!User.TryGetUserId(out var actorUserId))
        {
            return Unauthorized("Invalid token subject.");
        }

        var actorRole = User.FindFirstValue(ClaimTypes.Role);
        var query = _context.Appointments
            .Include(a => a.Patient).ThenInclude(p => p.Person)
            .Include(a => a.Doctor).ThenInclude(d => d.Person)
            .Include(a => a.Clinic)
            .AsQueryable();

        if (string.Equals(actorRole, SystemRoles.Doctor, StringComparison.OrdinalIgnoreCase))
        {
            if (!User.TryGetClinicId(out var clinicId))
            {
                return Unauthorized("Invalid clinic claim in token.");
            }

            query = query.Where(a => a.DoctorId == actorUserId && a.ClinicId == clinicId);
        }
        else if (!string.Equals(actorRole, SystemRoles.Admin, StringComparison.OrdinalIgnoreCase))
        {
            return Forbid();
        }

        var appointment = await query.FirstOrDefaultAsync(a => a.AppointmentId == appointmentId);
        if (appointment == null)
        {
            return NotFound("Appointment not found.");
        }

        if (appointment.Status == AppointmentStatuses.Cancelled)
        {
            return BadRequest("Cancelled appointments cannot have attendance recorded.");
        }

        if (appointment.Status is AppointmentStatuses.Completed or AppointmentStatuses.NoShow)
        {
            return BadRequest("Attendance has already been recorded for this appointment.");
        }

        if (appointment.AppointmentDateTime > DateTime.UtcNow.AddMinutes(5))
        {
            return BadRequest("Attendance can only be recorded after the appointment starts.");
        }

        var status = NormalizeText(request.Status);
        if (status is "completed" or "complete")
        {
            appointment.Status = AppointmentStatuses.Completed;
        }
        else if (status is "noshow" or "no-show" or "no_show")
        {
            appointment.Status = AppointmentStatuses.NoShow;
        }
        else
        {
            return BadRequest("Status must be Completed or NoShow.");
        }

        var attendanceAuditEventType = appointment.Status == AppointmentStatuses.Completed
            ? AppointmentAuditEventTypes.AttendanceMarkedCompleted
            : AppointmentAuditEventTypes.AttendanceMarkedNoShow;

        AddAppointmentAuditEvent(
            appointment.AppointmentId,
            appointment.ClinicId,
            actorUserId,
            actorRole,
            attendanceAuditEventType,
            $"Attendance marked as {appointment.Status}.");

        await _context.SaveChangesAsync();

        return Ok(new
        {
            appointment.AppointmentId,
            appointment.Status,
            appointment.AppointmentDateTime,
            PatientName = $"{appointment.Patient.Person.FirstName} {appointment.Patient.Person.LastName}".Trim(),
            DoctorName = $"{appointment.Doctor.Person.FirstName} {appointment.Doctor.Person.LastName}".Trim(),
            ClinicName = appointment.Clinic.Name
        });
    }

    [HttpGet("doctor")]
    [Authorize(Roles = SystemRoles.Doctor)]
    public async Task<IActionResult> GetDoctorAppointments()
    {
        if (!User.TryGetUserId(out var doctorId) || !User.TryGetClinicId(out var clinicId))
            return Unauthorized("Invalid token subject.");

        var appointments = await _context.Appointments
            .AsNoTracking()
            .Where(a => a.DoctorId == doctorId)
            .Where(a => a.ClinicId == clinicId)
            .Include(a => a.Patient)
            .ThenInclude(u => u.Person)
            .Select(a => new
            {
                a.AppointmentId,
                a.DoctorId,
                a.Status,
                a.AppointmentDateTime,
                PatientName = a.Patient.Person.FirstName + " " + a.Patient.Person.LastName,
                a.PostponeRequestStatus,
                a.ProposedDateTime,
                a.PostponeReason,
                a.PostponeRequestedAtUtc,
                a.DoctorResponseNote,
                a.DoctorRespondedAtUtc,
                a.PatientRespondedAtUtc,
                a.CancelledAtUtc,
                a.CancelledByUserId,
                a.CancellationReason
            })
            .OrderBy(a => a.AppointmentDateTime)
            .ToListAsync();

        return Ok(appointments);
    }

    [HttpGet("all")]
    [Authorize(Roles = SystemRoles.Admin)]
    public async Task<IActionResult> GetAllAppointments(
        [FromQuery] Guid? clinicId = null,
        [FromQuery] DateOnly? dateFrom = null,
        [FromQuery] DateOnly? dateTo = null,
        [FromQuery] string? status = null)
    {
        var query = _context.Appointments
            .AsNoTracking()
            .Include(a => a.Patient).ThenInclude(p => p.Person)
            .Include(a => a.Doctor).ThenInclude(d => d.Person)
            .Include(a => a.Clinic)
            .AsQueryable();

        if (!TryApplyAdminFilters(query, clinicId, dateFrom, dateTo, status, out var filteredQuery, out var filterError))
        {
            return BadRequest(filterError);
        }

        var appointments = await filteredQuery
            .Select(a => new
            {
                a.AppointmentId,
                a.Status,
                a.AppointmentDateTime,
                PatientName = a.Patient.Person.FirstName + " " + a.Patient.Person.LastName,
                DoctorName = a.Doctor.Person.FirstName + " " + a.Doctor.Person.LastName,
                a.ClinicId,
                ClinicName = a.Clinic.Name,
                a.PostponeRequestStatus,
                a.ProposedDateTime,
                a.PostponeReason,
                a.PostponeRequestedAtUtc,
                a.DoctorResponseNote,
                a.DoctorRespondedAtUtc,
                a.PatientRespondedAtUtc,
                a.CancelledAtUtc,
                a.CancelledByUserId,
                a.CancellationReason
            })
            .OrderBy(a => a.AppointmentDateTime)
            .ToListAsync();

        return Ok(appointments);
    }

    [HttpGet("all/export")]
    [Authorize(Roles = SystemRoles.Admin)]
    public async Task<IActionResult> ExportAllAppointmentsCsv(
        [FromQuery] Guid? clinicId = null,
        [FromQuery] DateOnly? dateFrom = null,
        [FromQuery] DateOnly? dateTo = null,
        [FromQuery] string? status = null)
    {
        var query = _context.Appointments
            .AsNoTracking()
            .Include(a => a.Patient).ThenInclude(p => p.Person)
            .Include(a => a.Doctor).ThenInclude(d => d.Person)
            .Include(a => a.Clinic)
            .AsQueryable();

        if (!TryApplyAdminFilters(query, clinicId, dateFrom, dateTo, status, out var filteredQuery, out var filterError))
        {
            return BadRequest(filterError);
        }

        var appointments = await filteredQuery
            .Select(a => new
            {
                a.AppointmentId,
                a.Status,
                a.AppointmentDateTime,
                PatientName = a.Patient.Person.FirstName + " " + a.Patient.Person.LastName,
                DoctorName = a.Doctor.Person.FirstName + " " + a.Doctor.Person.LastName,
                a.ClinicId,
                ClinicName = a.Clinic.Name,
                a.PostponeRequestStatus,
                a.ProposedDateTime,
                a.PostponeReason,
                a.PostponeRequestedAtUtc,
                a.DoctorResponseNote,
                a.DoctorRespondedAtUtc,
                a.PatientRespondedAtUtc,
                a.CancelledAtUtc,
                a.CancelledByUserId,
                a.CancellationReason
            })
            .OrderBy(a => a.AppointmentDateTime)
            .ToListAsync();

        var csv = new StringBuilder();
        csv.AppendLine("AppointmentId,Status,AppointmentDateTimeUtc,PatientName,DoctorName,ClinicId,ClinicName,PostponeRequestStatus,ProposedDateTimeUtc,PostponeReason,PostponeRequestedAtUtc,DoctorResponseNote,DoctorRespondedAtUtc,PatientRespondedAtUtc,CancelledAtUtc,CancelledByUserId,CancellationReason");

        foreach (var appointment in appointments)
        {
            csv.AppendLine(string.Join(",",
                EscapeCsv(appointment.AppointmentId.ToString()),
                EscapeCsv(appointment.Status),
                EscapeCsv(NormalizeToUtc(appointment.AppointmentDateTime).ToString("O")),
                EscapeCsv(appointment.PatientName),
                EscapeCsv(appointment.DoctorName),
                EscapeCsv(appointment.ClinicId.ToString()),
                EscapeCsv(appointment.ClinicName),
                EscapeCsv(appointment.PostponeRequestStatus),
                EscapeCsv(appointment.ProposedDateTime.HasValue ? NormalizeToUtc(appointment.ProposedDateTime.Value).ToString("O") : string.Empty),
                EscapeCsv(appointment.PostponeReason),
                EscapeCsv(appointment.PostponeRequestedAtUtc.HasValue ? NormalizeToUtc(appointment.PostponeRequestedAtUtc.Value).ToString("O") : string.Empty),
                EscapeCsv(appointment.DoctorResponseNote),
                EscapeCsv(appointment.DoctorRespondedAtUtc.HasValue ? NormalizeToUtc(appointment.DoctorRespondedAtUtc.Value).ToString("O") : string.Empty),
                EscapeCsv(appointment.PatientRespondedAtUtc.HasValue ? NormalizeToUtc(appointment.PatientRespondedAtUtc.Value).ToString("O") : string.Empty),
                EscapeCsv(appointment.CancelledAtUtc.HasValue ? NormalizeToUtc(appointment.CancelledAtUtc.Value).ToString("O") : string.Empty),
                EscapeCsv(appointment.CancelledByUserId?.ToString()),
                EscapeCsv(appointment.CancellationReason)));
        }

        var scope = clinicId.HasValue ? clinicId.Value.ToString("N")[..8] : "all-clinics";
        var fileName = $"appointments-{scope}-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv";
        var content = Encoding.UTF8.GetBytes(csv.ToString());
        return File(content, "text/csv; charset=utf-8", fileName);
    }

    [HttpGet("{appointmentId:guid}/audit")]
    [Authorize(Roles = SystemRoles.Patient + "," + SystemRoles.Doctor + "," + SystemRoles.Admin)]
    public async Task<IActionResult> GetAppointmentAudit(Guid appointmentId)
    {
        if (!User.TryGetUserId(out var actorUserId))
        {
            return Unauthorized("Invalid token subject.");
        }

        var actorRole = User.FindFirstValue(ClaimTypes.Role);
        var appointmentScope = _context.Appointments.AsNoTracking().AsQueryable();

        if (string.Equals(actorRole, SystemRoles.Patient, StringComparison.OrdinalIgnoreCase))
        {
            if (!User.TryGetClinicId(out var clinicId))
            {
                return Unauthorized("Invalid clinic claim in token.");
            }

            appointmentScope = appointmentScope.Where(a =>
                a.PatientId == actorUserId &&
                a.ClinicId == clinicId);
        }
        else if (string.Equals(actorRole, SystemRoles.Doctor, StringComparison.OrdinalIgnoreCase))
        {
            if (!User.TryGetClinicId(out var clinicId))
            {
                return Unauthorized("Invalid clinic claim in token.");
            }

            appointmentScope = appointmentScope.Where(a =>
                a.DoctorId == actorUserId &&
                a.ClinicId == clinicId);
        }
        else if (!string.Equals(actorRole, SystemRoles.Admin, StringComparison.OrdinalIgnoreCase))
        {
            return Forbid();
        }

        var appointmentExists = await appointmentScope.AnyAsync(a => a.AppointmentId == appointmentId);
        if (!appointmentExists)
        {
            return NotFound("Appointment not found.");
        }

        var auditEvents = await _context.AppointmentAuditEvents
            .AsNoTracking()
            .Where(e => e.AppointmentId == appointmentId)
            .OrderByDescending(e => e.OccurredAtUtc)
            .Select(e => new
            {
                e.AppointmentAuditEventId,
                e.AppointmentId,
                e.EventType,
                e.Details,
                e.ActorUserId,
                e.ActorRole,
                ActorName = e.ActorUser != null
                    ? (e.ActorUser.Person.FirstName + " " + e.ActorUser.Person.LastName)
                    : null,
                e.OccurredAtUtc
            })
            .ToListAsync();

        return Ok(auditEvents);
    }

    [HttpGet("patient")]
    [Authorize(Roles = SystemRoles.Patient)]
    public async Task<IActionResult> GetPatientAppointments()
    {
        if (!User.TryGetUserId(out var patientId) || !User.TryGetClinicId(out var clinicId))
            return Unauthorized("Invalid token subject.");

        var appointments = await _context.Appointments
            .AsNoTracking()
            .Include(a => a.Doctor).ThenInclude(d => d.Person)
            .Include(a => a.Clinic)
            .Where(a => a.PatientId == patientId)
            .Where(a => a.ClinicId == clinicId)
            .Select(a => new
            {
                a.AppointmentId,
                a.DoctorId,
                a.Status,
                a.AppointmentDateTime,
                DoctorName = a.Doctor.Person.FirstName + " " + a.Doctor.Person.LastName,
                ClinicName = a.Clinic.Name,
                a.PostponeRequestStatus,
                a.ProposedDateTime,
                a.PostponeReason,
                a.PostponeRequestedAtUtc,
                a.DoctorResponseNote,
                a.DoctorRespondedAtUtc,
                a.PatientRespondedAtUtc,
                a.CancelledAtUtc,
                a.CancelledByUserId,
                a.CancellationReason
            })
            .OrderBy(a => a.AppointmentDateTime)
            .ToListAsync();

        return Ok(appointments);
    }

    private static bool TryApplyAdminFilters(
        IQueryable<Appointment> source,
        Guid? clinicId,
        DateOnly? dateFrom,
        DateOnly? dateTo,
        string? status,
        out IQueryable<Appointment> filtered,
        out string error)
    {
        filtered = source;
        error = string.Empty;

        if (clinicId.HasValue && clinicId.Value != Guid.Empty)
        {
            filtered = filtered.Where(a => a.ClinicId == clinicId.Value);
        }

        if (dateFrom.HasValue && dateTo.HasValue && dateFrom.Value > dateTo.Value)
        {
            error = "dateFrom must be earlier than or equal to dateTo.";
            return false;
        }

        if (dateFrom.HasValue)
        {
            var fromUtc = DateTime.SpecifyKind(dateFrom.Value.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
            filtered = filtered.Where(a => a.AppointmentDateTime >= fromUtc);
        }

        if (dateTo.HasValue)
        {
            var toExclusiveUtc = DateTime.SpecifyKind(dateTo.Value.AddDays(1).ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
            filtered = filtered.Where(a => a.AppointmentDateTime < toExclusiveUtc);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!TryNormalizeLifecycleStatus(status, out var normalizedStatus))
            {
                error = "status must be Scheduled, Cancelled, Completed, or NoShow.";
                return false;
            }

            filtered = filtered.Where(a => a.Status == normalizedStatus);
        }

        return true;
    }

    private static bool TryNormalizeLifecycleStatus(string rawStatus, out string normalizedStatus)
    {
        normalizedStatus = string.Empty;
        var status = NormalizeText(rawStatus);

        if (status is "scheduled")
        {
            normalizedStatus = AppointmentStatuses.Scheduled;
            return true;
        }

        if (status is "cancelled" or "canceled")
        {
            normalizedStatus = AppointmentStatuses.Cancelled;
            return true;
        }

        if (status is "completed" or "complete")
        {
            normalizedStatus = AppointmentStatuses.Completed;
            return true;
        }

        if (status is "noshow" or "no-show" or "no_show")
        {
            normalizedStatus = AppointmentStatuses.NoShow;
            return true;
        }

        return false;
    }

    private void AddAppointmentAuditEvent(
        Guid appointmentId,
        Guid clinicId,
        Guid? actorUserId,
        string? actorRole,
        string eventType,
        string details)
    {
        _context.AppointmentAuditEvents.Add(new AppointmentAuditEvent
        {
            AppointmentAuditEventId = Guid.NewGuid(),
            AppointmentId = appointmentId,
            ClinicId = clinicId,
            ActorUserId = actorUserId,
            ActorRole = (actorRole ?? string.Empty).Trim(),
            EventType = eventType,
            Details = details.Trim(),
            OccurredAtUtc = DateTime.UtcNow
        });
    }

    private void QueueNotification(
        Guid recipientUserId,
        Guid? appointmentId,
        Guid? actorUserId,
        string type,
        string title,
        string message)
    {
        _context.UserNotifications.Add(new UserNotification
        {
            UserNotificationId = Guid.NewGuid(),
            UserId = recipientUserId,
            AppointmentId = appointmentId,
            ActorUserId = actorUserId,
            Type = type,
            Title = title,
            Message = message,
            IsRead = false,
            CreatedAtUtc = DateTime.UtcNow,
            ReadAtUtc = null
        });
    }

    private async Task<bool> HasDoctorConflict(Guid doctorId, DateTime proposedDateTime, Guid? excludedAppointmentId = null)
    {
        var proposedEnd = proposedDateTime.AddMinutes(AppointmentDurationMinutes);
        return await _context.Appointments.AnyAsync(a =>
            a.DoctorId == doctorId &&
            a.Status == AppointmentStatuses.Scheduled &&
            (!excludedAppointmentId.HasValue || a.AppointmentId != excludedAppointmentId.Value) &&
            a.AppointmentDateTime < proposedEnd &&
            proposedDateTime < a.AppointmentDateTime.AddMinutes(AppointmentDurationMinutes));
    }

    private async Task<bool> HasPatientConflict(Guid patientId, DateTime proposedDateTime, Guid? excludedAppointmentId = null)
    {
        var proposedEnd = proposedDateTime.AddMinutes(AppointmentDurationMinutes);
        return await _context.Appointments.AnyAsync(a =>
            a.PatientId == patientId &&
            a.Status == AppointmentStatuses.Scheduled &&
            (!excludedAppointmentId.HasValue || a.AppointmentId != excludedAppointmentId.Value) &&
            a.AppointmentDateTime < proposedEnd &&
            proposedDateTime < a.AppointmentDateTime.AddMinutes(AppointmentDurationMinutes));
    }

    private async Task<bool> IsDoctorBookableAtDateTime(Guid doctorId, Guid clinicId, DateTime requestedStartUtc)
    {
        var clinic = await _context.Clinics
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.ClinicId == clinicId && c.IsActive);
        if (clinic == null)
        {
            return false;
        }

        var clinicTimeZone = ResolveClinicTimeZone(clinic.Timezone);
        var normalizedStartUtc = NormalizeToUtc(requestedStartUtc);
        var requestedStartLocal = TimeZoneInfo.ConvertTimeFromUtc(normalizedStartUtc, clinicTimeZone);
        var date = DateOnly.FromDateTime(requestedStartLocal);
        var dayStartLocal = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);

        var operatingHour = await _context.ClinicOperatingHours
            .AsNoTracking()
            .FirstOrDefaultAsync(h => h.ClinicId == clinicId && h.DayOfWeek == (int)date.DayOfWeek);

        if (operatingHour == null || operatingHour.IsClosed || !operatingHour.OpenTime.HasValue || !operatingHour.CloseTime.HasValue)
        {
            return false;
        }

        var clinicOpenLocal = dayStartLocal.Add(operatingHour.OpenTime.Value);
        var clinicCloseLocal = dayStartLocal.Add(operatingHour.CloseTime.Value);
        if (clinicOpenLocal >= clinicCloseLocal)
        {
            return false;
        }

        var weeklyAvailability = await _context.DoctorAvailabilityWindows
            .AsNoTracking()
            .Where(x =>
                x.DoctorId == doctorId &&
                x.IsActive &&
                x.DayOfWeek == (int)date.DayOfWeek)
            .OrderBy(x => x.StartTime)
            .ToListAsync();

        var weeklyBreaks = await _context.DoctorAvailabilityBreaks
            .AsNoTracking()
            .Where(x =>
                x.DoctorId == doctorId &&
                x.IsActive &&
                x.DayOfWeek == (int)date.DayOfWeek)
            .OrderBy(x => x.StartTime)
            .ToListAsync();

        var overrides = await _context.DoctorAvailabilityOverrides
            .AsNoTracking()
            .Where(x =>
                x.DoctorId == doctorId &&
                x.IsActive &&
                x.Date == date)
            .OrderBy(x => x.StartTime)
            .ToListAsync();

        var windows = BuildDoctorBookableWindowsLocal(
            dayStartLocal,
            clinicOpenLocal,
            clinicCloseLocal,
            weeklyAvailability,
            weeklyBreaks,
            overrides);

        if (windows.Count == 0)
        {
            return false;
        }

        var requestedStartLocalUnspecified = DateTime.SpecifyKind(requestedStartLocal, DateTimeKind.Unspecified);
        var requestedEndLocal = requestedStartLocalUnspecified.AddMinutes(AppointmentDurationMinutes);
        return windows.Any(window =>
            window.Start <= requestedStartLocalUnspecified &&
            requestedEndLocal <= window.End);
    }

    private async Task<object> BuildDoctorAvailabilityResponse(Guid doctorId, Guid clinicId, string timezone)
    {
        var weeklyAvailability = await _context.DoctorAvailabilityWindows
            .AsNoTracking()
            .Where(x => x.DoctorId == doctorId && x.IsActive)
            .OrderBy(x => x.DayOfWeek)
            .ThenBy(x => x.StartTime)
            .ToListAsync();

        var weeklyBreaks = await _context.DoctorAvailabilityBreaks
            .AsNoTracking()
            .Where(x => x.DoctorId == doctorId && x.IsActive)
            .OrderBy(x => x.DayOfWeek)
            .ThenBy(x => x.StartTime)
            .ToListAsync();

        var overrides = await _context.DoctorAvailabilityOverrides
            .AsNoTracking()
            .Where(x => x.DoctorId == doctorId && x.IsActive)
            .OrderBy(x => x.Date)
            .ThenBy(x => x.IsAvailable ? 1 : 0)
            .ThenBy(x => x.StartTime)
            .ToListAsync();

        return new
        {
            doctorId,
            clinicId,
            timezone,
            weeklyAvailability = weeklyAvailability.Select(x => new
            {
                x.DayOfWeek,
                Start = FormatTime(x.StartTime),
                End = FormatTime(x.EndTime)
            }),
            weeklyBreaks = weeklyBreaks.Select(x => new
            {
                x.DayOfWeek,
                Start = FormatTime(x.StartTime),
                End = FormatTime(x.EndTime)
            }),
            overrides = overrides.Select(x => new
            {
                x.Date,
                Start = x.StartTime.HasValue ? FormatTime(x.StartTime.Value) : null,
                End = x.EndTime.HasValue ? FormatTime(x.EndTime.Value) : null,
                x.IsAvailable,
                x.Reason
            })
        };
    }

    private static bool TryBuildWeeklyWindows(
        Guid doctorId,
        IReadOnlyCollection<DoctorWeeklyTimeRangeRequest> requestedRanges,
        out List<DoctorAvailabilityWindow> windows,
        out string error)
    {
        windows = [];
        error = string.Empty;

        var uniqueKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var range in requestedRanges)
        {
            if (range.DayOfWeek < 0 || range.DayOfWeek > 6)
            {
                error = "Weekly availability dayOfWeek must be between 0 and 6.";
                return false;
            }

            if (!TryParseTimeOfDay(range.Start, out var start))
            {
                error = $"Weekly availability start time '{range.Start}' is invalid. Use HH:mm.";
                return false;
            }

            if (!TryParseTimeOfDay(range.End, out var end))
            {
                error = $"Weekly availability end time '{range.End}' is invalid. Use HH:mm.";
                return false;
            }

            if (start >= end)
            {
                error = "Weekly availability start time must be earlier than end time.";
                return false;
            }

            var key = $"{range.DayOfWeek}|{start:c}|{end:c}";
            if (!uniqueKeys.Add(key))
            {
                error = "Weekly availability contains duplicate intervals.";
                return false;
            }

            windows.Add(new DoctorAvailabilityWindow
            {
                DoctorAvailabilityWindowId = Guid.NewGuid(),
                DoctorId = doctorId,
                DayOfWeek = range.DayOfWeek,
                StartTime = start,
                EndTime = end,
                IsActive = true
            });
        }

        return true;
    }

    private static bool TryBuildWeeklyBreaks(
        Guid doctorId,
        IReadOnlyCollection<DoctorWeeklyTimeRangeRequest> requestedRanges,
        out List<DoctorAvailabilityBreak> breaks,
        out string error)
    {
        breaks = [];
        error = string.Empty;

        var uniqueKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var range in requestedRanges)
        {
            if (range.DayOfWeek < 0 || range.DayOfWeek > 6)
            {
                error = "Weekly break dayOfWeek must be between 0 and 6.";
                return false;
            }

            if (!TryParseTimeOfDay(range.Start, out var start))
            {
                error = $"Weekly break start time '{range.Start}' is invalid. Use HH:mm.";
                return false;
            }

            if (!TryParseTimeOfDay(range.End, out var end))
            {
                error = $"Weekly break end time '{range.End}' is invalid. Use HH:mm.";
                return false;
            }

            if (start >= end)
            {
                error = "Weekly break start time must be earlier than end time.";
                return false;
            }

            var key = $"{range.DayOfWeek}|{start:c}|{end:c}";
            if (!uniqueKeys.Add(key))
            {
                error = "Weekly breaks contain duplicate intervals.";
                return false;
            }

            breaks.Add(new DoctorAvailabilityBreak
            {
                DoctorAvailabilityBreakId = Guid.NewGuid(),
                DoctorId = doctorId,
                DayOfWeek = range.DayOfWeek,
                StartTime = start,
                EndTime = end,
                IsActive = true
            });
        }

        return true;
    }

    private static bool TryBuildOverrides(
        Guid doctorId,
        IReadOnlyCollection<DoctorDateOverrideRequest> requestedOverrides,
        out List<DoctorAvailabilityOverride> overrides,
        out string error)
    {
        overrides = [];
        error = string.Empty;

        var uniqueKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in requestedOverrides)
        {
            if (entry.Date == default)
            {
                error = "Override date is required.";
                return false;
            }

            var hasStart = !string.IsNullOrWhiteSpace(entry.Start);
            var hasEnd = !string.IsNullOrWhiteSpace(entry.End);
            if (hasStart != hasEnd)
            {
                error = "Override start and end must both be provided, or both omitted.";
                return false;
            }

            TimeSpan? start = null;
            TimeSpan? end = null;
            if (hasStart && hasEnd)
            {
                if (!TryParseTimeOfDay(entry.Start, out var parsedStart))
                {
                    error = $"Override start time '{entry.Start}' is invalid. Use HH:mm.";
                    return false;
                }

                if (!TryParseTimeOfDay(entry.End, out var parsedEnd))
                {
                    error = $"Override end time '{entry.End}' is invalid. Use HH:mm.";
                    return false;
                }

                if (parsedStart >= parsedEnd)
                {
                    error = "Override start time must be earlier than end time.";
                    return false;
                }

                start = parsedStart;
                end = parsedEnd;
            }
            else if (entry.IsAvailable)
            {
                error = "Available overrides require start and end time.";
                return false;
            }

            var key = $"{entry.Date:yyyy-MM-dd}|{entry.IsAvailable}|{start?.ToString("c")}|{end?.ToString("c")}";
            if (!uniqueKeys.Add(key))
            {
                error = "Overrides contain duplicate entries.";
                return false;
            }

            overrides.Add(new DoctorAvailabilityOverride
            {
                DoctorAvailabilityOverrideId = Guid.NewGuid(),
                DoctorId = doctorId,
                Date = entry.Date,
                StartTime = start,
                EndTime = end,
                IsAvailable = entry.IsAvailable,
                Reason = (entry.Reason ?? string.Empty).Trim(),
                IsActive = true
            });
        }

        return true;
    }

    private static List<LocalWindow> BuildDoctorBookableWindowsLocal(
        DateTime dayStartLocal,
        DateTime clinicOpenLocal,
        DateTime clinicCloseLocal,
        IReadOnlyCollection<DoctorAvailabilityWindow> weeklyAvailability,
        IReadOnlyCollection<DoctorAvailabilityBreak> weeklyBreaks,
        IReadOnlyCollection<DoctorAvailabilityOverride> overrides)
    {
        var windows = weeklyAvailability.Count > 0
            ? weeklyAvailability.Select(x => new LocalWindow(dayStartLocal.Add(x.StartTime), dayStartLocal.Add(x.EndTime))).ToList()
            : new List<LocalWindow> { new(clinicOpenLocal, clinicCloseLocal) };

        windows = IntersectWithBounds(windows, clinicOpenLocal, clinicCloseLocal);
        windows = MergeWindows(windows);

        if (weeklyBreaks.Count > 0)
        {
            var breakWindows = weeklyBreaks
                .Select(x => new LocalWindow(dayStartLocal.Add(x.StartTime), dayStartLocal.Add(x.EndTime)))
                .ToList();
            windows = SubtractWindows(windows, breakWindows);
        }

        foreach (var unavailableOverride in overrides.Where(x => !x.IsAvailable))
        {
            if (unavailableOverride.StartTime.HasValue && unavailableOverride.EndTime.HasValue)
            {
                var block = new LocalWindow(
                    dayStartLocal.Add(unavailableOverride.StartTime.Value),
                    dayStartLocal.Add(unavailableOverride.EndTime.Value));
                windows = SubtractWindows(windows, [block]);
            }
            else
            {
                windows.Clear();
            }
        }

        foreach (var availableOverride in overrides.Where(x =>
                     x.IsAvailable &&
                     x.StartTime.HasValue &&
                     x.EndTime.HasValue))
        {
            windows.Add(new LocalWindow(
                dayStartLocal.Add(availableOverride.StartTime!.Value),
                dayStartLocal.Add(availableOverride.EndTime!.Value)));
        }

        windows = IntersectWithBounds(windows, clinicOpenLocal, clinicCloseLocal);
        windows = MergeWindows(windows);
        return windows;
    }

    private static List<LocalWindow> IntersectWithBounds(IEnumerable<LocalWindow> windows, DateTime minStart, DateTime maxEnd)
    {
        return windows
            .Select(window => new LocalWindow(
                window.Start < minStart ? minStart : window.Start,
                window.End > maxEnd ? maxEnd : window.End))
            .Where(window => window.Start < window.End)
            .ToList();
    }

    private static List<LocalWindow> SubtractWindows(IEnumerable<LocalWindow> sourceWindows, IEnumerable<LocalWindow> subtractors)
    {
        var current = MergeWindows(sourceWindows);
        var blocks = MergeWindows(subtractors);

        foreach (var block in blocks)
        {
            var next = new List<LocalWindow>();
            foreach (var window in current)
            {
                if (block.End <= window.Start || block.Start >= window.End)
                {
                    next.Add(window);
                    continue;
                }

                if (block.Start > window.Start)
                {
                    next.Add(new LocalWindow(window.Start, block.Start));
                }

                if (block.End < window.End)
                {
                    next.Add(new LocalWindow(block.End, window.End));
                }
            }

            current = next;
            if (current.Count == 0)
            {
                break;
            }
        }

        return MergeWindows(current);
    }

    private static List<LocalWindow> MergeWindows(IEnumerable<LocalWindow> windows)
    {
        var sorted = windows
            .Where(window => window.Start < window.End)
            .OrderBy(window => window.Start)
            .ThenBy(window => window.End)
            .ToList();

        if (sorted.Count <= 1)
        {
            return sorted;
        }

        var merged = new List<LocalWindow> { new(sorted[0].Start, sorted[0].End) };
        for (var i = 1; i < sorted.Count; i++)
        {
            var current = sorted[i];
            var last = merged[^1];
            if (current.Start <= last.End)
            {
                if (current.End > last.End)
                {
                    last.End = current.End;
                }

                continue;
            }

            merged.Add(new LocalWindow(current.Start, current.End));
        }

        return merged;
    }

    private static string FormatTime(TimeSpan value)
    {
        return value.ToString(@"hh\:mm");
    }

    private static bool TryParseTimeOfDay(string? value, out TimeSpan time)
    {
        var isValid =
            TimeSpan.TryParseExact(value?.Trim(), @"hh\:mm", null, out time) ||
            TimeSpan.TryParseExact(value?.Trim(), @"h\:mm", null, out time);

        if (!isValid)
        {
            time = default;
            return false;
        }

        return time >= TimeSpan.Zero && time < TimeSpan.FromDays(1);
    }

    private static string NormalizeText(string value)
    {
        return value.Trim().ToLowerInvariant();
    }

    private static DateTime NormalizeToUtc(DateTime value)
    {
        if (value.Kind == DateTimeKind.Utc)
        {
            return value;
        }

        if (value.Kind == DateTimeKind.Local)
        {
            return value.ToUniversalTime();
        }

        return DateTime.SpecifyKind(value, DateTimeKind.Utc);
    }

    private static TimeZoneInfo ResolveClinicTimeZone(string? timezoneId)
    {
        var normalized = string.IsNullOrWhiteSpace(timezoneId) ? "UTC" : timezoneId.Trim();

        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(normalized);
        }
        catch (TimeZoneNotFoundException)
        {
            if (TimeZoneInfo.TryConvertIanaIdToWindowsId(normalized, out var windowsId))
            {
                return TimeZoneInfo.FindSystemTimeZoneById(windowsId);
            }

            if (TimeZoneInfo.TryConvertWindowsIdToIanaId(normalized, out var ianaId))
            {
                return TimeZoneInfo.FindSystemTimeZoneById(ianaId);
            }
        }
        catch (InvalidTimeZoneException)
        {
            // Fallback to UTC below.
        }

        return TimeZoneInfo.Utc;
    }

    private static DateTime? TryConvertLocalToUtc(DateTime localTime, TimeZoneInfo timezone)
    {
        try
        {
            var unspecified = DateTime.SpecifyKind(localTime, DateTimeKind.Unspecified);
            return TimeZoneInfo.ConvertTimeToUtc(unspecified, timezone);
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    private static bool IsSlotOverlapping(DateTime candidateStartUtc, IReadOnlyCollection<DateTime> existingStartsUtc)
    {
        var candidateEndUtc = candidateStartUtc.AddMinutes(AppointmentDurationMinutes);

        foreach (var existingStart in existingStartsUtc)
        {
            var normalizedExistingStart = existingStart.Kind == DateTimeKind.Utc
                ? existingStart
                : DateTime.SpecifyKind(existingStart, DateTimeKind.Utc);
            var existingEndUtc = normalizedExistingStart.AddMinutes(AppointmentDurationMinutes);
            if (normalizedExistingStart < candidateEndUtc && candidateStartUtc < existingEndUtc)
            {
                return true;
            }
        }

        return false;
    }

    private static string? CleanOptionalText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }

    private static string EscapeCsv(string? value)
    {
        var text = value ?? string.Empty;

        // Mitigate CSV formula injection in spreadsheet clients.
        if (text.Length > 0)
        {
            var firstNonWhitespaceIndex = 0;
            while (firstNonWhitespaceIndex < text.Length && char.IsWhiteSpace(text[firstNonWhitespaceIndex]))
            {
                firstNonWhitespaceIndex++;
            }

            if (firstNonWhitespaceIndex < text.Length)
            {
                var firstNonWhitespaceChar = text[firstNonWhitespaceIndex];
                if (firstNonWhitespaceChar is '=' or '+' or '-' or '@')
                {
                    text = "'" + text;
                }
            }
        }

        var escaped = text.Replace("\"", "\"\"");
        if (escaped.IndexOfAny(new[] { ',', '"', '\r', '\n' }) >= 0)
        {
            return $"\"{escaped}\"";
        }

        return escaped;
    }

    private sealed class LocalWindow
    {
        public LocalWindow(DateTime start, DateTime end)
        {
            Start = start;
            End = end;
        }

        public DateTime Start { get; }
        public DateTime End { get; set; }
    }
}
