using System.Net;
using MedicalAppointment.Api.IntegrationTests.Infrastructure;
using MedicalAppointment.Domain.Constants;
using MedicalAppointment.Domain.Models;

namespace MedicalAppointment.Api.IntegrationTests;

public sealed class AdminAppointmentExportIntegrationTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;

    public AdminAppointmentExportIntegrationTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ExportAppointmentsCsv_RespectsClinicFilterAndContainsHeader()
    {
        var clinicAId = Guid.NewGuid();
        var clinicBId = Guid.NewGuid();
        var doctorRoleId = Guid.NewGuid();
        var patientRoleId = Guid.NewGuid();
        var appointmentAId = Guid.NewGuid();
        var appointmentBId = Guid.NewGuid();

        await _factory.ResetDatabaseAsync(async db =>
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

            db.SysRoles.AddRange(
                new SysRole
                {
                    SysRoleId = doctorRoleId,
                    Name = SystemRoles.Doctor,
                    Description = "Doctor",
                    IsActive = true
                },
                new SysRole
                {
                    SysRoleId = patientRoleId,
                    Name = SystemRoles.Patient,
                    Description = "Patient",
                    IsActive = true
                });

            db.Clinics.AddRange(
                new Clinic
                {
                    ClinicId = clinicAId,
                    Name = "Clinic Alpha",
                    Code = $"CA-{clinicAId.ToString("N")[..6]}",
                    LegalName = "Clinic Alpha LLC",
                    SysClinicTypeId = Clinic.DefaultSysClinicTypeId,
                    SysOwnershipTypeId = Clinic.DefaultSysOwnershipTypeId,
                    SysSourceSystemId = Clinic.DefaultSysSourceSystemId,
                    Timezone = "America/Chicago",
                    IsActive = true,
                    CreatedAtUtc = DateTime.UtcNow,
                    UpdatedAtUtc = DateTime.UtcNow
                },
                new Clinic
                {
                    ClinicId = clinicBId,
                    Name = "Clinic Beta",
                    Code = $"CB-{clinicBId.ToString("N")[..6]}",
                    LegalName = "Clinic Beta LLC",
                    SysClinicTypeId = Clinic.DefaultSysClinicTypeId,
                    SysOwnershipTypeId = Clinic.DefaultSysOwnershipTypeId,
                    SysSourceSystemId = Clinic.DefaultSysSourceSystemId,
                    Timezone = "America/Chicago",
                    IsActive = true,
                    CreatedAtUtc = DateTime.UtcNow,
                    UpdatedAtUtc = DateTime.UtcNow
                });

            var doctorPersonAId = Guid.NewGuid();
            var patientPersonAId = Guid.NewGuid();
            var doctorPersonBId = Guid.NewGuid();
            var patientPersonBId = Guid.NewGuid();

            db.Persons.AddRange(
                new Person
                {
                    PersonId = doctorPersonAId,
                    FirstName = "Doctor",
                    LastName = "Alpha",
                    NormalizedName = "doctor alpha",
                    PersonalIdentifier = $"PIDA-{doctorPersonAId.ToString("N")[..10]}",
                    Address = "Address A",
                    BirthDate = new DateTime(1981, 1, 1)
                },
                new Person
                {
                    PersonId = patientPersonAId,
                    FirstName = "Patient",
                    LastName = "Alpha",
                    NormalizedName = "patient alpha",
                    PersonalIdentifier = $"PIPA-{patientPersonAId.ToString("N")[..10]}",
                    Address = "Address B",
                    BirthDate = new DateTime(1991, 1, 1)
                },
                new Person
                {
                    PersonId = doctorPersonBId,
                    FirstName = "Doctor",
                    LastName = "Beta",
                    NormalizedName = "doctor beta",
                    PersonalIdentifier = $"PIDB-{doctorPersonBId.ToString("N")[..10]}",
                    Address = "Address C",
                    BirthDate = new DateTime(1982, 1, 1)
                },
                new Person
                {
                    PersonId = patientPersonBId,
                    FirstName = "Patient",
                    LastName = "Beta",
                    NormalizedName = "patient beta",
                    PersonalIdentifier = $"PIPB-{patientPersonBId.ToString("N")[..10]}",
                    Address = "Address D",
                    BirthDate = new DateTime(1992, 1, 1)
                });

            var doctorAId = Guid.NewGuid();
            var patientAId = Guid.NewGuid();
            var doctorBId = Guid.NewGuid();
            var patientBId = Guid.NewGuid();

            db.Users.AddRange(
                new User
                {
                    UserId = doctorAId,
                    Username = $"doctorA_{doctorAId.ToString("N")[..8]}",
                    Email = $"doctorA_{doctorAId.ToString("N")[..8]}@example.com",
                    PasswordHash = "hash",
                    SysRoleId = doctorRoleId,
                    ClinicId = clinicAId,
                    PersonId = doctorPersonAId
                },
                new User
                {
                    UserId = patientAId,
                    Username = $"patientA_{patientAId.ToString("N")[..8]}",
                    Email = $"patientA_{patientAId.ToString("N")[..8]}@example.com",
                    PasswordHash = "hash",
                    SysRoleId = patientRoleId,
                    ClinicId = clinicAId,
                    PersonId = patientPersonAId
                },
                new User
                {
                    UserId = doctorBId,
                    Username = $"doctorB_{doctorBId.ToString("N")[..8]}",
                    Email = $"doctorB_{doctorBId.ToString("N")[..8]}@example.com",
                    PasswordHash = "hash",
                    SysRoleId = doctorRoleId,
                    ClinicId = clinicBId,
                    PersonId = doctorPersonBId
                },
                new User
                {
                    UserId = patientBId,
                    Username = $"patientB_{patientBId.ToString("N")[..8]}",
                    Email = $"patientB_{patientBId.ToString("N")[..8]}@example.com",
                    PasswordHash = "hash",
                    SysRoleId = patientRoleId,
                    ClinicId = clinicBId,
                    PersonId = patientPersonBId
                });

            db.Appointments.AddRange(
                new Appointment
                {
                    AppointmentId = appointmentAId,
                    DoctorId = doctorAId,
                    PatientId = patientAId,
                    ClinicId = clinicAId,
                    AppointmentDateTime = DateTime.UtcNow.AddDays(3),
                    Status = AppointmentStatuses.Scheduled,
                    PostponeRequestStatus = AppointmentPostponeStatuses.None
                },
                new Appointment
                {
                    AppointmentId = appointmentBId,
                    DoctorId = doctorBId,
                    PatientId = patientBId,
                    ClinicId = clinicBId,
                    AppointmentDateTime = DateTime.UtcNow.AddDays(4),
                    Status = AppointmentStatuses.Scheduled,
                    PostponeRequestStatus = AppointmentPostponeStatuses.None
                });

            await Task.CompletedTask;
        });

        using var client = _factory.CreateAuthenticatedClient(Guid.NewGuid(), clinicAId, SystemRoles.Admin);
        var response = await client.GetAsync($"/api/Appointment/all/export?clinicId={clinicAId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/csv", response.Content.Headers.ContentType?.MediaType);

        var contentDisposition = response.Content.Headers.ContentDisposition?.FileName
            ?? response.Content.Headers.ContentDisposition?.FileNameStar
            ?? string.Empty;
        Assert.Contains("appointments-", contentDisposition);

        var csv = await response.Content.ReadAsStringAsync();
        Assert.Contains("AppointmentId,Status,AppointmentDateTimeUtc", csv);
        Assert.Contains(appointmentAId.ToString(), csv);
        Assert.DoesNotContain(appointmentBId.ToString(), csv);
        Assert.Contains("Clinic Alpha", csv);
        Assert.DoesNotContain("Clinic Beta", csv);
    }

    [Fact]
    public async Task ExportAppointmentsCsv_AppliesDateAndStatusFilters()
    {
        var clinicId = Guid.NewGuid();
        var doctorRoleId = Guid.NewGuid();
        var patientRoleId = Guid.NewGuid();
        var doctorPersonId = Guid.NewGuid();
        var patientPersonId = Guid.NewGuid();
        var doctorId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var inScopeAppointmentId = Guid.NewGuid();
        var wrongStatusAppointmentId = Guid.NewGuid();
        var wrongDateAppointmentId = Guid.NewGuid();

        await _factory.ResetDatabaseAsync(async db =>
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

            db.SysRoles.AddRange(
                new SysRole
                {
                    SysRoleId = doctorRoleId,
                    Name = SystemRoles.Doctor,
                    Description = "Doctor",
                    IsActive = true
                },
                new SysRole
                {
                    SysRoleId = patientRoleId,
                    Name = SystemRoles.Patient,
                    Description = "Patient",
                    IsActive = true
                });

            db.Clinics.Add(new Clinic
            {
                ClinicId = clinicId,
                Name = "Clinic Filters",
                Code = $"CF-{clinicId.ToString("N")[..6]}",
                LegalName = "Clinic Filters LLC",
                SysClinicTypeId = Clinic.DefaultSysClinicTypeId,
                SysOwnershipTypeId = Clinic.DefaultSysOwnershipTypeId,
                SysSourceSystemId = Clinic.DefaultSysSourceSystemId,
                Timezone = "America/Chicago",
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            });

            db.Persons.AddRange(
                new Person
                {
                    PersonId = doctorPersonId,
                    FirstName = "Doctor",
                    LastName = "Filter",
                    NormalizedName = "doctor filter",
                    PersonalIdentifier = $"PIDF-{doctorPersonId.ToString("N")[..10]}",
                    Address = "Doctor address",
                    BirthDate = new DateTime(1980, 1, 1)
                },
                new Person
                {
                    PersonId = patientPersonId,
                    FirstName = "Patient",
                    LastName = "Filter",
                    NormalizedName = "patient filter",
                    PersonalIdentifier = $"PIPF-{patientPersonId.ToString("N")[..10]}",
                    Address = "Patient address",
                    BirthDate = new DateTime(1990, 1, 1)
                });

            db.Users.AddRange(
                new User
                {
                    UserId = doctorId,
                    Username = $"doctorF_{doctorId.ToString("N")[..8]}",
                    Email = $"doctorF_{doctorId.ToString("N")[..8]}@example.com",
                    PasswordHash = "hash",
                    SysRoleId = doctorRoleId,
                    ClinicId = clinicId,
                    PersonId = doctorPersonId
                },
                new User
                {
                    UserId = patientId,
                    Username = $"patientF_{patientId.ToString("N")[..8]}",
                    Email = $"patientF_{patientId.ToString("N")[..8]}@example.com",
                    PasswordHash = "hash",
                    SysRoleId = patientRoleId,
                    ClinicId = clinicId,
                    PersonId = patientPersonId
                });

            db.Appointments.AddRange(
                new Appointment
                {
                    AppointmentId = inScopeAppointmentId,
                    DoctorId = doctorId,
                    PatientId = patientId,
                    ClinicId = clinicId,
                    AppointmentDateTime = new DateTime(2026, 2, 15, 10, 0, 0, DateTimeKind.Utc),
                    Status = AppointmentStatuses.Scheduled,
                    PostponeRequestStatus = AppointmentPostponeStatuses.None
                },
                new Appointment
                {
                    AppointmentId = wrongStatusAppointmentId,
                    DoctorId = doctorId,
                    PatientId = patientId,
                    ClinicId = clinicId,
                    AppointmentDateTime = new DateTime(2026, 2, 15, 11, 0, 0, DateTimeKind.Utc),
                    Status = AppointmentStatuses.Cancelled,
                    PostponeRequestStatus = AppointmentPostponeStatuses.None
                },
                new Appointment
                {
                    AppointmentId = wrongDateAppointmentId,
                    DoctorId = doctorId,
                    PatientId = patientId,
                    ClinicId = clinicId,
                    AppointmentDateTime = new DateTime(2026, 2, 17, 10, 0, 0, DateTimeKind.Utc),
                    Status = AppointmentStatuses.Scheduled,
                    PostponeRequestStatus = AppointmentPostponeStatuses.None
                });

            await Task.CompletedTask;
        });

        using var client = _factory.CreateAuthenticatedClient(Guid.NewGuid(), clinicId, SystemRoles.Admin);
        var response = await client.GetAsync($"/api/Appointment/all/export?clinicId={clinicId}&dateFrom=2026-02-15&dateTo=2026-02-15&status=Scheduled");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var csv = await response.Content.ReadAsStringAsync();
        Assert.Contains(inScopeAppointmentId.ToString(), csv);
        Assert.DoesNotContain(wrongStatusAppointmentId.ToString(), csv);
        Assert.DoesNotContain(wrongDateAppointmentId.ToString(), csv);
    }
}
