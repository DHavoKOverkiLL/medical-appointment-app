using System.Net;
using System.Net.Http.Json;
using MedicalAppointment.Api.IntegrationTests.Infrastructure;
using MedicalAppointment.Domain.Constants;
using MedicalAppointment.Domain.Models;
using Xunit;

namespace MedicalAppointment.Api.IntegrationTests;

public sealed class AvailabilitySlotsIntegrationTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;

    public AvailabilitySlotsIntegrationTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AvailableSlots_ExcludesIntervalsDefinedAsWeeklyBreaks()
    {
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(45));
        var actors = await SeedBaseAsync(
            date,
            timezone: "America/Chicago",
            clinicOpen: TimeSpan.FromHours(8),
            clinicClose: TimeSpan.FromHours(18));

        await _factory.ExecuteDbAsync(db =>
        {
            db.DoctorAvailabilityWindows.Add(new DoctorAvailabilityWindow
            {
                DoctorAvailabilityWindowId = Guid.NewGuid(),
                DoctorId = actors.DoctorId,
                DayOfWeek = (int)date.DayOfWeek,
                StartTime = TimeSpan.FromHours(9),
                EndTime = TimeSpan.FromHours(12),
                IsActive = true
            });

            db.DoctorAvailabilityBreaks.Add(new DoctorAvailabilityBreak
            {
                DoctorAvailabilityBreakId = Guid.NewGuid(),
                DoctorId = actors.DoctorId,
                DayOfWeek = (int)date.DayOfWeek,
                StartTime = TimeSpan.FromHours(10),
                EndTime = TimeSpan.FromHours(10.5),
                IsActive = true
            });

            return Task.CompletedTask;
        });

        using var client = _factory.CreateAuthenticatedClient(actors.PatientId, actors.ClinicId, SystemRoles.Patient);
        var response = await client.GetAsync($"/api/Appointment/available-slots?doctorId={actors.DoctorId}&date={date:yyyy-MM-dd}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<AvailableSlotsResponse>();
        Assert.NotNull(payload);

        var localTimes = payload!.Slots.Select(slot => slot.LocalTime).ToArray();
        Assert.Equal(new[] { "09:00", "09:30", "10:30", "11:00", "11:30" }, localTimes);
    }

    [Fact]
    public async Task AvailableSlots_ReturnsNoSlotsWhenDateMarkedUnavailable()
    {
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(52));
        var actors = await SeedBaseAsync(
            date,
            timezone: "America/Chicago",
            clinicOpen: TimeSpan.FromHours(8),
            clinicClose: TimeSpan.FromHours(18));

        await _factory.ExecuteDbAsync(db =>
        {
            db.DoctorAvailabilityWindows.Add(new DoctorAvailabilityWindow
            {
                DoctorAvailabilityWindowId = Guid.NewGuid(),
                DoctorId = actors.DoctorId,
                DayOfWeek = (int)date.DayOfWeek,
                StartTime = TimeSpan.FromHours(9),
                EndTime = TimeSpan.FromHours(17),
                IsActive = true
            });

            db.DoctorAvailabilityOverrides.Add(new DoctorAvailabilityOverride
            {
                DoctorAvailabilityOverrideId = Guid.NewGuid(),
                DoctorId = actors.DoctorId,
                Date = date,
                StartTime = null,
                EndTime = null,
                IsAvailable = false,
                Reason = "On leave",
                IsActive = true
            });

            return Task.CompletedTask;
        });

        using var client = _factory.CreateAuthenticatedClient(actors.PatientId, actors.ClinicId, SystemRoles.Patient);
        var response = await client.GetAsync($"/api/Appointment/available-slots?doctorId={actors.DoctorId}&date={date:yyyy-MM-dd}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<AvailableSlotsResponse>();
        Assert.NotNull(payload);
        Assert.Empty(payload!.Slots);
    }

    [Fact]
    public async Task AvailableSlots_SkipsNonExistingLocalTimesOnDstSpringForwardDay()
    {
        var year = DateTime.UtcNow.Year + 1;
        var date = GetSecondSundayOfMarch(year);
        var actors = await SeedBaseAsync(
            date,
            timezone: "America/Chicago",
            clinicOpen: TimeSpan.FromHours(1),
            clinicClose: TimeSpan.FromHours(4));

        await _factory.ExecuteDbAsync(db =>
        {
            db.DoctorAvailabilityWindows.Add(new DoctorAvailabilityWindow
            {
                DoctorAvailabilityWindowId = Guid.NewGuid(),
                DoctorId = actors.DoctorId,
                DayOfWeek = (int)date.DayOfWeek,
                StartTime = TimeSpan.FromHours(1),
                EndTime = TimeSpan.FromHours(4),
                IsActive = true
            });

            return Task.CompletedTask;
        });

        using var client = _factory.CreateAuthenticatedClient(actors.PatientId, actors.ClinicId, SystemRoles.Patient);
        var response = await client.GetAsync($"/api/Appointment/available-slots?doctorId={actors.DoctorId}&date={date:yyyy-MM-dd}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<AvailableSlotsResponse>();
        Assert.NotNull(payload);

        var localTimes = payload!.Slots.Select(slot => slot.LocalTime).ToArray();
        Assert.DoesNotContain("02:00", localTimes);
        Assert.DoesNotContain("02:30", localTimes);
        Assert.Contains("01:00", localTimes);
        Assert.Contains("03:00", localTimes);
    }

    private async Task<TestActors> SeedBaseAsync(
        DateOnly date,
        string timezone,
        TimeSpan clinicOpen,
        TimeSpan clinicClose)
    {
        var clinicId = Guid.NewGuid();
        var doctorRoleId = Guid.NewGuid();
        var patientRoleId = Guid.NewGuid();
        var adminRoleId = Guid.NewGuid();
        var doctorId = Guid.NewGuid();
        var patientId = Guid.NewGuid();

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
                },
                new SysRole
                {
                    SysRoleId = adminRoleId,
                    Name = SystemRoles.Admin,
                    Description = "Admin",
                    IsActive = true
                });

            db.Clinics.Add(new Clinic
            {
                ClinicId = clinicId,
                Name = $"Clinic {clinicId.ToString("N")[..8]}",
                Code = $"C{clinicId.ToString("N")[..7]}",
                LegalName = "Clinic Legal Name",
                SysClinicTypeId = Clinic.DefaultSysClinicTypeId,
                SysOwnershipTypeId = Clinic.DefaultSysOwnershipTypeId,
                SysSourceSystemId = Clinic.DefaultSysSourceSystemId,
                Timezone = timezone,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            });

            db.ClinicOperatingHours.Add(new ClinicOperatingHour
            {
                ClinicOperatingHourId = Guid.NewGuid(),
                ClinicId = clinicId,
                DayOfWeek = (int)date.DayOfWeek,
                OpenTime = clinicOpen,
                CloseTime = clinicClose,
                IsClosed = false
            });

            var doctorPersonId = Guid.NewGuid();
            var patientPersonId = Guid.NewGuid();

            db.Persons.AddRange(
                new Person
                {
                    PersonId = doctorPersonId,
                    FirstName = "Doctor",
                    LastName = "One",
                    NormalizedName = "doctor one",
                    PersonalIdentifier = $"PID-{doctorPersonId.ToString("N")[..10]}",
                    Address = "Address 1",
                    BirthDate = new DateTime(1980, 1, 1)
                },
                new Person
                {
                    PersonId = patientPersonId,
                    FirstName = "Patient",
                    LastName = "One",
                    NormalizedName = "patient one",
                    PersonalIdentifier = $"PID-{patientPersonId.ToString("N")[..10]}",
                    Address = "Address 2",
                    BirthDate = new DateTime(1990, 1, 1)
                });

            db.Users.AddRange(
                new User
                {
                    UserId = doctorId,
                    Username = $"doctor_{doctorId.ToString("N")[..8]}",
                    Email = $"doctor_{doctorId.ToString("N")[..8]}@example.com",
                    PasswordHash = "hash",
                    SysRoleId = doctorRoleId,
                    ClinicId = clinicId,
                    PersonId = doctorPersonId
                },
                new User
                {
                    UserId = patientId,
                    Username = $"patient_{patientId.ToString("N")[..8]}",
                    Email = $"patient_{patientId.ToString("N")[..8]}@example.com",
                    PasswordHash = "hash",
                    SysRoleId = patientRoleId,
                    ClinicId = clinicId,
                    PersonId = patientPersonId
                });

            await Task.CompletedTask;
        });

        return new TestActors(clinicId, doctorId, patientId);
    }

    private static DateOnly GetSecondSundayOfMarch(int year)
    {
        var firstMarch = new DateOnly(year, 3, 1);
        var offset = ((int)DayOfWeek.Sunday - (int)firstMarch.DayOfWeek + 7) % 7;
        var firstSunday = firstMarch.AddDays(offset);
        return firstSunday.AddDays(7);
    }

    private sealed record TestActors(Guid ClinicId, Guid DoctorId, Guid PatientId);

    private sealed record AvailableSlotsResponse(string Timezone, List<AvailableSlotResponse> Slots);

    private sealed record AvailableSlotResponse(string LocalTime);
}
