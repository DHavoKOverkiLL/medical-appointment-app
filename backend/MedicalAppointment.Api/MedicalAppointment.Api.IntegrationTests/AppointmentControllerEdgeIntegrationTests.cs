using System.Net;
using System.Net.Http.Json;
using MedicalAppointment.Api.Contracts.Appointments;
using MedicalAppointment.Api.IntegrationTests.Infrastructure;
using MedicalAppointment.Domain.Constants;
using MedicalAppointment.Domain.Models;

namespace MedicalAppointment.Api.IntegrationTests;

public sealed class AppointmentControllerEdgeIntegrationTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;

    public AppointmentControllerEdgeIntegrationTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Create_ReturnsBadRequest_WhenAppointmentIsInPast()
    {
        await _factory.ResetDatabaseAsync();

        using var client = _factory.CreateAuthenticatedClient(Guid.NewGuid(), Guid.NewGuid(), SystemRoles.Patient);
        var response = await client.PostAsJsonAsync("/api/Appointment", new CreateAppointmentRequest
        {
            DoctorId = Guid.NewGuid(),
            AppointmentDateTime = DateTime.UtcNow.AddMinutes(-15)
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Appointment time must be in the future.", body);
    }

    [Fact]
    public async Task Create_ReturnsBadRequest_WhenPatientAccountIsMissingOrInactive()
    {
        await _factory.ResetDatabaseAsync();

        using var client = _factory.CreateAuthenticatedClient(Guid.NewGuid(), Guid.NewGuid(), SystemRoles.Patient);
        var response = await client.PostAsJsonAsync("/api/Appointment", new CreateAppointmentRequest
        {
            DoctorId = Guid.NewGuid(),
            AppointmentDateTime = DateTime.UtcNow.AddDays(2)
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Patient account is invalid or inactive.", body);
    }

    [Fact]
    public async Task Create_ReturnsBadRequest_WhenDoctorIsMissingOrInvalid()
    {
        var clinicId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var patientRoleId = Guid.NewGuid();
        var patientPersonId = Guid.NewGuid();

        await _factory.ResetDatabaseAsync(async db =>
        {
            AddRequiredSystemLookups(db);
            db.SysRoles.Add(new SysRole
            {
                SysRoleId = patientRoleId,
                Name = SystemRoles.Patient,
                Description = "Patient",
                IsActive = true
            });

            db.Clinics.Add(CreateClinic(clinicId, "Create Clinic"));
            db.Persons.Add(CreatePerson(patientPersonId, "Patient", "One", "PAT-APT-001"));
            db.Users.Add(CreateUser(patientId, patientRoleId, clinicId, patientPersonId, "patient"));
            await Task.CompletedTask;
        });

        using var client = _factory.CreateAuthenticatedClient(patientId, clinicId, SystemRoles.Patient);
        var response = await client.PostAsJsonAsync("/api/Appointment", new CreateAppointmentRequest
        {
            DoctorId = Guid.NewGuid(),
            AppointmentDateTime = DateTime.UtcNow.AddDays(3)
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Selected doctor is invalid, inactive, or from another clinic.", body);
    }

    [Fact]
    public async Task Create_ReturnsBadRequest_WhenDoctorHasNoBookableSchedule()
    {
        var clinicId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var doctorId = Guid.NewGuid();

        var patientRoleId = Guid.NewGuid();
        var doctorRoleId = Guid.NewGuid();
        var patientPersonId = Guid.NewGuid();
        var doctorPersonId = Guid.NewGuid();

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

            db.Clinics.Add(CreateClinic(clinicId, "Bookable Clinic"));
            db.Persons.AddRange(
                CreatePerson(patientPersonId, "Patient", "One", "PAT-APT-002"),
                CreatePerson(doctorPersonId, "Doctor", "One", "DOC-APT-001"));
            db.Users.AddRange(
                CreateUser(patientId, patientRoleId, clinicId, patientPersonId, "patient"),
                CreateUser(doctorId, doctorRoleId, clinicId, doctorPersonId, "doctor"));

            await Task.CompletedTask;
        });

        using var client = _factory.CreateAuthenticatedClient(patientId, clinicId, SystemRoles.Patient);
        var response = await client.PostAsJsonAsync("/api/Appointment", new CreateAppointmentRequest
        {
            DoctorId = doctorId,
            AppointmentDateTime = DateTime.UtcNow.AddDays(4)
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Selected doctor is unavailable at that time.", body);
    }

    [Fact]
    public async Task GetDoctorAvailability_ReturnsBadRequest_WhenAdminDoesNotProvideDoctorId()
    {
        await _factory.ResetDatabaseAsync();

        using var client = _factory.CreateAuthenticatedClient(Guid.NewGuid(), Guid.NewGuid(), SystemRoles.Admin);
        var response = await client.GetAsync("/api/Appointment/doctor-availability");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("doctorId is required for admin requests.", body);
    }

    [Fact]
    public async Task GetDoctorAvailability_ReturnsForbidden_WhenDoctorQueriesAnotherDoctor()
    {
        await _factory.ResetDatabaseAsync();

        using var client = _factory.CreateAuthenticatedClient(Guid.NewGuid(), Guid.NewGuid(), SystemRoles.Doctor);
        var response = await client.GetAsync($"/api/Appointment/doctor-availability?doctorId={Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetAvailableSlots_ReturnsForbidden_WhenNonAdminTargetsAnotherClinicDoctor()
    {
        var clinicAId = Guid.NewGuid();
        var clinicBId = Guid.NewGuid();
        var doctorRoleId = Guid.NewGuid();
        var doctorPersonId = Guid.NewGuid();
        var doctorId = Guid.NewGuid();

        await _factory.ResetDatabaseAsync(async db =>
        {
            AddRequiredSystemLookups(db);
            db.SysRoles.Add(new SysRole
            {
                SysRoleId = doctorRoleId,
                Name = SystemRoles.Doctor,
                Description = "Doctor",
                IsActive = true
            });

            db.Clinics.AddRange(
                CreateClinic(clinicAId, "Doctor Clinic"),
                CreateClinic(clinicBId, "Caller Clinic"));
            db.Persons.Add(CreatePerson(doctorPersonId, "Doctor", "Slots", "DOC-SLOT-001"));
            db.Users.Add(CreateUser(doctorId, doctorRoleId, clinicAId, doctorPersonId, "doctor"));
            await Task.CompletedTask;
        });

        using var client = _factory.CreateAuthenticatedClient(Guid.NewGuid(), clinicBId, SystemRoles.Patient);
        var queryDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30));
        var response = await client.GetAsync($"/api/Appointment/available-slots?doctorId={doctorId}&date={queryDate:yyyy-MM-dd}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private static void AddRequiredSystemLookups(MedicalAppointment.Infrastructure.AppDbContext db)
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

    private static Clinic CreateClinic(Guid clinicId, string name)
    {
        return new Clinic
        {
            ClinicId = clinicId,
            Name = name,
            Code = $"CL-{clinicId.ToString("N")[..6]}",
            LegalName = $"{name} LLC",
            SysClinicTypeId = Clinic.DefaultSysClinicTypeId,
            SysOwnershipTypeId = Clinic.DefaultSysOwnershipTypeId,
            SysSourceSystemId = Clinic.DefaultSysSourceSystemId,
            Timezone = "America/Chicago",
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
    }

    private static Person CreatePerson(Guid personId, string firstName, string lastName, string personalIdentifier)
    {
        return new Person
        {
            PersonId = personId,
            FirstName = firstName,
            LastName = lastName,
            NormalizedName = $"{firstName}{lastName}".Replace(" ", string.Empty).ToUpperInvariant(),
            PersonalIdentifier = personalIdentifier,
            Address = "Address",
            BirthDate = new DateTime(1990, 1, 1)
        };
    }

    private static User CreateUser(Guid userId, Guid roleId, Guid clinicId, Guid personId, string usernamePrefix)
    {
        var suffix = userId.ToString("N")[..8];
        return new User
        {
            UserId = userId,
            Username = $"{usernamePrefix}_{suffix}",
            Email = $"{usernamePrefix}_{suffix}@example.com",
            PasswordHash = "hash",
            SysRoleId = roleId,
            ClinicId = clinicId,
            PersonId = personId
        };
    }
}
