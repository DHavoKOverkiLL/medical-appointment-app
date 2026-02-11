using MedicalAppointment.Api.Contracts.Dashboard;
using MedicalAppointment.Api.Extensions;
using MedicalAppointment.Domain.Constants;
using MedicalAppointment.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MedicalAppointment.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly AppDbContext _context;

    public DashboardController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("admin")]
    [Authorize(Roles = SystemRoles.Admin)]
    public async Task<IActionResult> GetAdminDashboard()
    {
        var now = DateTime.UtcNow;
        var startOfTodayUtc = now.Date;
        var startOfTomorrowUtc = startOfTodayUtc.AddDays(1);

        var response = new AdminDashboardResponse
        {
            Clinics = await _context.Clinics.CountAsync(),
            Users = await _context.Users.CountAsync(),
            Doctors = await _context.Users.CountAsync(u => u.SysRole != null && u.SysRole.Name == SystemRoles.Doctor && u.SysRole.IsActive),
            Patients = await _context.Users.CountAsync(u => u.SysRole != null && u.SysRole.Name == SystemRoles.Patient && u.SysRole.IsActive),
            Admins = await _context.Users.CountAsync(u => u.SysRole != null && u.SysRole.Name == SystemRoles.Admin && u.SysRole.IsActive),
            AppointmentsToday = await _context.Appointments.CountAsync(a =>
                a.Status == AppointmentStatuses.Scheduled &&
                a.AppointmentDateTime >= startOfTodayUtc &&
                a.AppointmentDateTime < startOfTomorrowUtc),
            UpcomingAppointments = await _context.Appointments.CountAsync(a =>
                a.Status == AppointmentStatuses.Scheduled &&
                a.AppointmentDateTime >= now),
            ClinicLoad = await _context.Clinics
                .AsNoTracking()
                .OrderBy(c => c.Name)
                .Select(c => new ClinicLoadResponse
                {
                    ClinicId = c.ClinicId,
                    ClinicName = c.Name,
                    Users = _context.Users.Count(u => u.ClinicId == c.ClinicId),
                    Appointments = _context.Appointments.Count(a =>
                        a.ClinicId == c.ClinicId &&
                        a.Status == AppointmentStatuses.Scheduled &&
                        a.AppointmentDateTime >= now)
                })
                .ToListAsync()
        };

        return Ok(response);
    }

    [HttpGet("doctor")]
    [Authorize(Roles = SystemRoles.Doctor)]
    public async Task<IActionResult> GetDoctorDashboard()
    {
        if (!User.TryGetUserId(out var doctorId) || !User.TryGetClinicId(out var clinicId))
        {
            return Unauthorized("Invalid token.");
        }

        var now = DateTime.UtcNow;
        var startOfTodayUtc = now.Date;
        var startOfTomorrowUtc = startOfTodayUtc.AddDays(1);

        var upcomingAppointments = _context.Appointments
            .AsNoTracking()
            .Where(a =>
                a.DoctorId == doctorId &&
                a.ClinicId == clinicId &&
                a.Status == AppointmentStatuses.Scheduled &&
                a.AppointmentDateTime >= now);

        var response = new DoctorDashboardResponse
        {
            ClinicName = await _context.Clinics
                .Where(c => c.ClinicId == clinicId)
                .Select(c => c.Name)
                .FirstOrDefaultAsync() ?? string.Empty,
            AppointmentsToday = await _context.Appointments.CountAsync(a =>
                a.DoctorId == doctorId &&
                a.ClinicId == clinicId &&
                a.Status == AppointmentStatuses.Scheduled &&
                a.AppointmentDateTime >= startOfTodayUtc &&
                a.AppointmentDateTime < startOfTomorrowUtc),
            UpcomingAppointments = await upcomingAppointments.CountAsync(),
            UniquePatientsUpcoming = await upcomingAppointments
                .Select(a => a.PatientId)
                .Distinct()
                .CountAsync(),
            NextAppointmentUtc = await upcomingAppointments
                .OrderBy(a => a.AppointmentDateTime)
                .Select(a => (DateTime?)a.AppointmentDateTime)
                .FirstOrDefaultAsync()
        };

        return Ok(response);
    }

    [HttpGet("patient")]
    [Authorize(Roles = SystemRoles.Patient)]
    public async Task<IActionResult> GetPatientDashboard()
    {
        if (!User.TryGetUserId(out var patientId) || !User.TryGetClinicId(out var clinicId))
        {
            return Unauthorized("Invalid token.");
        }

        var now = DateTime.UtcNow;

        var upcomingAppointments = _context.Appointments
            .AsNoTracking()
            .Where(a =>
                a.PatientId == patientId &&
                a.ClinicId == clinicId &&
                a.Status == AppointmentStatuses.Scheduled &&
                a.AppointmentDateTime >= now);

        var response = new PatientDashboardResponse
        {
            ClinicName = await _context.Clinics
                .Where(c => c.ClinicId == clinicId)
                .Select(c => c.Name)
                .FirstOrDefaultAsync() ?? string.Empty,
            UpcomingAppointments = await upcomingAppointments.CountAsync(),
            NextAppointmentUtc = await upcomingAppointments
                .OrderBy(a => a.AppointmentDateTime)
                .Select(a => (DateTime?)a.AppointmentDateTime)
                .FirstOrDefaultAsync(),
            DoctorsInClinic = await _context.Users.CountAsync(u =>
                u.ClinicId == clinicId &&
                u.SysRole != null &&
                u.SysRole.Name == SystemRoles.Doctor &&
                u.SysRole.IsActive)
        };

        return Ok(response);
    }
}
