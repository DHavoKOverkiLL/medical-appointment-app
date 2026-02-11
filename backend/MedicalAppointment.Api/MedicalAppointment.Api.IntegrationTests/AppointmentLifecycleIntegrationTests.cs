using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MedicalAppointment.Api.Contracts.Appointments;
using MedicalAppointment.Api.IntegrationTests.Infrastructure;
using MedicalAppointment.Domain.Constants;
using MedicalAppointment.Domain.Models;
using MedicalAppointment.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MedicalAppointment.Api.IntegrationTests;

public sealed class AppointmentLifecycleIntegrationTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;

    public AppointmentLifecycleIntegrationTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Lifecycle_CreateThenPostponeApproveThenCancel_UpdatesStateAuditAndNotifications()
    {
        var actors = await SeedActorsAsync(includeAdmin: false);
        var initialDateUtc = FutureUtc(daysAhead: 3, hour: 10);
        var proposedDateUtc = FutureUtc(daysAhead: 3, hour: 11);

        using var patientClient = _factory.CreateAuthenticatedClient(actors.PatientId, actors.ClinicId, SystemRoles.Patient);
        using var doctorClient = _factory.CreateAuthenticatedClient(actors.DoctorId, actors.ClinicId, SystemRoles.Doctor);

        var createResponse = await patientClient.PostAsJsonAsync("/api/Appointment", new CreateAppointmentRequest
        {
            DoctorId = actors.DoctorId,
            AppointmentDateTime = initialDateUtc
        });
        Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);
        var appointmentId = await ReadGuidFromResponseAsync(createResponse, "appointmentId");

        var postponeResponse = await patientClient.PostAsJsonAsync(
            $"/api/Appointment/{appointmentId}/postpone-request",
            new RequestPostponeAppointmentRequest
            {
                ProposedDateTime = proposedDateUtc,
                Reason = "Need to move due to work travel."
            });
        Assert.Equal(HttpStatusCode.OK, postponeResponse.StatusCode);

        var approveResponse = await doctorClient.PostAsJsonAsync(
            $"/api/Appointment/{appointmentId}/postpone-response",
            new RespondPostponeAppointmentRequest
            {
                Decision = "Approve"
            });
        Assert.Equal(HttpStatusCode.OK, approveResponse.StatusCode);

        var cancelResponse = await patientClient.PostAsJsonAsync(
            $"/api/Appointment/{appointmentId}/cancel",
            new CancelAppointmentRequest
            {
                Reason = "Cannot attend on the updated date."
            });
        Assert.Equal(HttpStatusCode.OK, cancelResponse.StatusCode);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var appointment = await db.Appointments.SingleAsync(a => a.AppointmentId == appointmentId);
        Assert.Equal(AppointmentStatuses.Cancelled, appointment.Status);
        Assert.Equal(AppointmentPostponeStatuses.None, appointment.PostponeRequestStatus);
        Assert.Null(appointment.ProposedDateTime);
        Assert.Null(appointment.PostponeReason);
        Assert.NotNull(appointment.CancelledAtUtc);
        Assert.Equal(actors.PatientId, appointment.CancelledByUserId);
        Assert.Equal("Cannot attend on the updated date.", appointment.CancellationReason);

        var auditEventTypes = await db.AppointmentAuditEvents
            .Where(x => x.AppointmentId == appointmentId)
            .Select(x => x.EventType)
            .ToListAsync();
        Assert.Contains(AppointmentAuditEventTypes.Created, auditEventTypes);
        Assert.Contains(AppointmentAuditEventTypes.PostponeRequested, auditEventTypes);
        Assert.Contains(AppointmentAuditEventTypes.PostponeApprovedByDoctor, auditEventTypes);
        Assert.Contains(AppointmentAuditEventTypes.Cancelled, auditEventTypes);

        var notifications = await db.UserNotifications
            .Where(n => n.AppointmentId == appointmentId)
            .ToListAsync();
        Assert.Contains(notifications, n => n.UserId == actors.DoctorId && n.Type == NotificationTypes.PostponeRequested);
        Assert.Contains(notifications, n => n.UserId == actors.PatientId && n.Type == NotificationTypes.PostponeApproved);
        Assert.Contains(notifications, n => n.UserId == actors.DoctorId && n.Type == NotificationTypes.AppointmentCancelled);
    }

    [Fact]
    public async Task Lifecycle_DoctorCounterProposalThenPatientAccept_UpdatesStateAuditAndNotifications()
    {
        var actors = await SeedActorsAsync(includeAdmin: false);
        var initialDateUtc = FutureUtc(daysAhead: 4, hour: 9);
        var patientProposedDateUtc = FutureUtc(daysAhead: 4, hour: 10);
        var doctorCounterDateUtc = FutureUtc(daysAhead: 4, hour: 11);

        using var patientClient = _factory.CreateAuthenticatedClient(actors.PatientId, actors.ClinicId, SystemRoles.Patient);
        using var doctorClient = _factory.CreateAuthenticatedClient(actors.DoctorId, actors.ClinicId, SystemRoles.Doctor);

        var createResponse = await patientClient.PostAsJsonAsync("/api/Appointment", new CreateAppointmentRequest
        {
            DoctorId = actors.DoctorId,
            AppointmentDateTime = initialDateUtc
        });
        Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);
        var appointmentId = await ReadGuidFromResponseAsync(createResponse, "appointmentId");

        var requestPostponeResponse = await patientClient.PostAsJsonAsync(
            $"/api/Appointment/{appointmentId}/postpone-request",
            new RequestPostponeAppointmentRequest
            {
                ProposedDateTime = patientProposedDateUtc,
                Reason = "Please move to a different hour."
            });
        Assert.Equal(HttpStatusCode.OK, requestPostponeResponse.StatusCode);

        var counterResponse = await doctorClient.PostAsJsonAsync(
            $"/api/Appointment/{appointmentId}/postpone-response",
            new RespondPostponeAppointmentRequest
            {
                Decision = "CounterPropose",
                CounterProposedDateTime = doctorCounterDateUtc,
                Note = "Offering another available slot."
            });
        Assert.Equal(HttpStatusCode.OK, counterResponse.StatusCode);

        var patientAcceptResponse = await patientClient.PostAsJsonAsync(
            $"/api/Appointment/{appointmentId}/postpone-counter-response",
            new RespondToCounterPostponeRequest
            {
                Decision = "Accept"
            });
        Assert.Equal(HttpStatusCode.OK, patientAcceptResponse.StatusCode);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var appointment = await db.Appointments.SingleAsync(a => a.AppointmentId == appointmentId);

        Assert.Equal(AppointmentStatuses.Scheduled, appointment.Status);
        Assert.Equal(AppointmentPostponeStatuses.Approved, appointment.PostponeRequestStatus);
        Assert.Equal(doctorCounterDateUtc, appointment.AppointmentDateTime);
        Assert.Equal(doctorCounterDateUtc, appointment.ProposedDateTime);
        Assert.Equal("Offering another available slot.", appointment.DoctorResponseNote);
        Assert.NotNull(appointment.DoctorRespondedAtUtc);
        Assert.NotNull(appointment.PatientRespondedAtUtc);

        var auditEventTypes = await db.AppointmentAuditEvents
            .Where(x => x.AppointmentId == appointmentId)
            .Select(x => x.EventType)
            .ToListAsync();
        Assert.Contains(AppointmentAuditEventTypes.PostponeCounterProposedByDoctor, auditEventTypes);
        Assert.Contains(AppointmentAuditEventTypes.PostponeCounterAcceptedByPatient, auditEventTypes);

        var notifications = await db.UserNotifications
            .Where(n => n.AppointmentId == appointmentId)
            .ToListAsync();
        Assert.Contains(notifications, n => n.UserId == actors.PatientId && n.Type == NotificationTypes.PostponeCounterProposed);
        Assert.Contains(notifications, n => n.UserId == actors.DoctorId && n.Type == NotificationTypes.PostponeCounterAccepted);
    }

    [Fact]
    public async Task Cancel_ByAdmin_NotifiesBothPatientAndDoctor()
    {
        var actors = await SeedActorsAsync(includeAdmin: true);
        var appointmentId = await CreateScheduledAppointmentAsync(actors, FutureUtc(daysAhead: 5, hour: 14));

        using var adminClient = _factory.CreateAuthenticatedClient(actors.AdminId!.Value, actors.ClinicId, SystemRoles.Admin);
        var response = await adminClient.PostAsJsonAsync(
            $"/api/Appointment/{appointmentId}/cancel",
            new CancelAppointmentRequest
            {
                Reason = "Clinic maintenance window."
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var appointment = await db.Appointments.SingleAsync(a => a.AppointmentId == appointmentId);
        Assert.Equal(AppointmentStatuses.Cancelled, appointment.Status);
        Assert.Equal(actors.AdminId, appointment.CancelledByUserId);

        var cancellations = await db.UserNotifications
            .Where(n => n.AppointmentId == appointmentId && n.Type == NotificationTypes.AppointmentCancelled)
            .ToListAsync();
        Assert.Contains(cancellations, n => n.UserId == actors.PatientId);
        Assert.Contains(cancellations, n => n.UserId == actors.DoctorId);
    }

    [Fact]
    public async Task Attendance_ByDoctor_MarksCompleted_AndSecondUpdateIsRejected()
    {
        var actors = await SeedActorsAsync(includeAdmin: false);
        var appointmentId = await CreateScheduledAppointmentAsync(actors, DateTime.UtcNow.AddMinutes(-10));

        using var doctorClient = _factory.CreateAuthenticatedClient(actors.DoctorId, actors.ClinicId, SystemRoles.Doctor);

        var firstResponse = await doctorClient.PostAsJsonAsync(
            $"/api/Appointment/{appointmentId}/attendance",
            new UpdateAppointmentAttendanceRequest
            {
                Status = "Completed"
            });
        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);

        var secondResponse = await doctorClient.PostAsJsonAsync(
            $"/api/Appointment/{appointmentId}/attendance",
            new UpdateAppointmentAttendanceRequest
            {
                Status = "NoShow"
            });
        Assert.Equal(HttpStatusCode.BadRequest, secondResponse.StatusCode);
        var secondBody = await secondResponse.Content.ReadAsStringAsync();
        Assert.Contains("Attendance has already been recorded", secondBody);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var appointment = await db.Appointments.SingleAsync(a => a.AppointmentId == appointmentId);
        Assert.Equal(AppointmentStatuses.Completed, appointment.Status);

        var auditEventTypes = await db.AppointmentAuditEvents
            .Where(x => x.AppointmentId == appointmentId)
            .Select(x => x.EventType)
            .ToListAsync();
        Assert.Contains(AppointmentAuditEventTypes.AttendanceMarkedCompleted, auditEventTypes);
    }

    private async Task<TestActors> SeedActorsAsync(bool includeAdmin)
    {
        var clinicId = Guid.NewGuid();
        var patientRoleId = Guid.NewGuid();
        var doctorRoleId = Guid.NewGuid();
        var adminRoleId = Guid.NewGuid();

        var patientPersonId = Guid.NewGuid();
        var doctorPersonId = Guid.NewGuid();
        var adminPersonId = Guid.NewGuid();

        var patientId = Guid.NewGuid();
        var doctorId = Guid.NewGuid();
        var adminId = includeAdmin ? Guid.NewGuid() : (Guid?)null;

        await _factory.ResetDatabaseAsync(async db =>
        {
            AddRequiredSystemLookups(db);
            db.SysRoles.AddRange(
                new SysRole
                {
                    SysRoleId = patientRoleId,
                    Name = SystemRoles.Patient,
                    Description = "Patient",
                    IsActive = true
                },
                new SysRole
                {
                    SysRoleId = doctorRoleId,
                    Name = SystemRoles.Doctor,
                    Description = "Doctor",
                    IsActive = true
                });

            if (includeAdmin)
            {
                db.SysRoles.Add(new SysRole
                {
                    SysRoleId = adminRoleId,
                    Name = SystemRoles.Admin,
                    Description = "Admin",
                    IsActive = true
                });
            }

            db.Clinics.Add(new Clinic
            {
                ClinicId = clinicId,
                Name = "Lifecycle Clinic",
                Code = $"LC-{clinicId.ToString("N")[..6]}",
                LegalName = "Lifecycle Clinic LLC",
                SysClinicTypeId = Clinic.DefaultSysClinicTypeId,
                SysOwnershipTypeId = Clinic.DefaultSysOwnershipTypeId,
                SysSourceSystemId = Clinic.DefaultSysSourceSystemId,
                Timezone = "UTC",
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            });

            for (var day = 0; day <= 6; day++)
            {
                db.ClinicOperatingHours.Add(new ClinicOperatingHour
                {
                    ClinicOperatingHourId = Guid.NewGuid(),
                    ClinicId = clinicId,
                    DayOfWeek = day,
                    OpenTime = TimeSpan.Zero,
                    CloseTime = new TimeSpan(23, 59, 0),
                    IsClosed = false
                });
            }

            db.Persons.AddRange(
                new Person
                {
                    PersonId = patientPersonId,
                    FirstName = "Patient",
                    LastName = "Lifecycle",
                    NormalizedName = "PATIENTLIFECYCLE",
                    PersonalIdentifier = "PAT-LIFE-001",
                    Address = "Patient Address",
                    BirthDate = new DateTime(1992, 1, 1)
                },
                new Person
                {
                    PersonId = doctorPersonId,
                    FirstName = "Doctor",
                    LastName = "Lifecycle",
                    NormalizedName = "DOCTORLIFECYCLE",
                    PersonalIdentifier = "DOC-LIFE-001",
                    Address = "Doctor Address",
                    BirthDate = new DateTime(1985, 1, 1)
                });

            if (includeAdmin)
            {
                db.Persons.Add(new Person
                {
                    PersonId = adminPersonId,
                    FirstName = "Admin",
                    LastName = "Lifecycle",
                    NormalizedName = "ADMINLIFECYCLE",
                    PersonalIdentifier = "ADM-LIFE-001",
                    Address = "Admin Address",
                    BirthDate = new DateTime(1980, 1, 1)
                });
            }

            db.Users.AddRange(
                new User
                {
                    UserId = patientId,
                    Username = $"patient_{patientId.ToString("N")[..8]}",
                    Email = $"patient_{patientId.ToString("N")[..8]}@example.com",
                    PasswordHash = "hash",
                    SysRoleId = patientRoleId,
                    ClinicId = clinicId,
                    PersonId = patientPersonId
                },
                new User
                {
                    UserId = doctorId,
                    Username = $"doctor_{doctorId.ToString("N")[..8]}",
                    Email = $"doctor_{doctorId.ToString("N")[..8]}@example.com",
                    PasswordHash = "hash",
                    SysRoleId = doctorRoleId,
                    ClinicId = clinicId,
                    PersonId = doctorPersonId
                });

            if (includeAdmin)
            {
                db.Users.Add(new User
                {
                    UserId = adminId!.Value,
                    Username = $"admin_{adminId.Value.ToString("N")[..8]}",
                    Email = $"admin_{adminId.Value.ToString("N")[..8]}@example.com",
                    PasswordHash = "hash",
                    SysRoleId = adminRoleId,
                    ClinicId = clinicId,
                    PersonId = adminPersonId
                });
            }

            await Task.CompletedTask;
        });

        return new TestActors(clinicId, patientId, doctorId, adminId);
    }

    private async Task<Guid> CreateScheduledAppointmentAsync(TestActors actors, DateTime appointmentDateTimeUtc)
    {
        var appointmentId = Guid.NewGuid();

        await _factory.ExecuteDbAsync(db =>
        {
            db.Appointments.Add(new Appointment
            {
                AppointmentId = appointmentId,
                DoctorId = actors.DoctorId,
                PatientId = actors.PatientId,
                ClinicId = actors.ClinicId,
                AppointmentDateTime = appointmentDateTimeUtc.Kind == DateTimeKind.Utc
                    ? appointmentDateTimeUtc
                    : DateTime.SpecifyKind(appointmentDateTimeUtc, DateTimeKind.Utc),
                Status = AppointmentStatuses.Scheduled,
                PostponeRequestStatus = AppointmentPostponeStatuses.None
            });

            return Task.CompletedTask;
        });

        return appointmentId;
    }

    private static DateTime FutureUtc(int daysAhead, int hour)
    {
        var date = DateTime.UtcNow.Date.AddDays(daysAhead).AddHours(hour);
        return DateTime.SpecifyKind(date, DateTimeKind.Utc);
    }

    private static async Task<Guid> ReadGuidFromResponseAsync(HttpResponseMessage response, string propertyName)
    {
        var payload = await response.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(payload);
        return json.RootElement.GetProperty(propertyName).GetGuid();
    }

    private static void AddRequiredSystemLookups(AppDbContext db)
    {
        db.SysClinicTypes.Add(new SysClinicType
        {
            SysClinicTypeId = Clinic.DefaultSysClinicTypeId,
            Name = "Primary Care",
            IsActive = true
        });

        db.SysOwnershipTypes.Add(new SysOwnershipType
        {
            SysOwnershipTypeId = Clinic.DefaultSysOwnershipTypeId,
            Name = "Physician Owned",
            IsActive = true
        });

        db.SysSourceSystems.Add(new SysSourceSystem
        {
            SysSourceSystemId = Clinic.DefaultSysSourceSystemId,
            Name = "EHR",
            IsActive = true
        });
    }

    private sealed record TestActors(Guid ClinicId, Guid PatientId, Guid DoctorId, Guid? AdminId);
}
