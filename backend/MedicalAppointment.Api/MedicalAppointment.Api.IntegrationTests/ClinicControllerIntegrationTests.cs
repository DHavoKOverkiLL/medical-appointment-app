using System.Net;
using System.Net.Http.Json;
using MedicalAppointment.Api.Contracts.Clinics;
using MedicalAppointment.Api.IntegrationTests.Infrastructure;
using MedicalAppointment.Domain.Constants;
using MedicalAppointment.Domain.Models;
using MedicalAppointment.Infrastructure;

namespace MedicalAppointment.Api.IntegrationTests;

public sealed class ClinicControllerIntegrationTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;

    public ClinicControllerIntegrationTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetActiveClinics_ReturnsOnlyActiveClinics_OrderedByName()
    {
        await _factory.ResetDatabaseAsync(async db =>
        {
            AddRequiredSystemLookups(db, includeOperationAndAccreditation: true);

            db.Clinics.AddRange(
                CreateClinic(Guid.NewGuid(), "Zenith Clinic", "ZEN001", isActive: true),
                CreateClinic(Guid.NewGuid(), "Alpha Clinic", "ALP001", isActive: true),
                CreateClinic(Guid.NewGuid(), "Dormant Clinic", "DOR001", isActive: false));

            await Task.CompletedTask;
        });

        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/Clinic/public");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<List<ClinicSummaryResponse>>();
        Assert.NotNull(payload);
        Assert.Equal(2, payload!.Count);
        Assert.Collection(
            payload,
            first => Assert.Equal("Alpha Clinic", first.Name),
            second => Assert.Equal("Zenith Clinic", second.Name));
        Assert.DoesNotContain(payload, clinic => clinic.Name == "Dormant Clinic");
    }

    [Fact]
    public async Task CreateClinic_ReturnsProblem_WhenNoOperationsAreConfigured()
    {
        await _factory.ResetDatabaseAsync(async db =>
        {
            AddRequiredSystemLookups(db, includeOperationAndAccreditation: false);
            await Task.CompletedTask;
        });

        using var client = _factory.CreateAuthenticatedClient(Guid.NewGuid(), Guid.NewGuid(), SystemRoles.Admin);
        var response = await client.PostAsJsonAsync("/api/Clinic", BuildCreateClinicRequest("No Ops Clinic", "NOOPS1"));

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("No service operations configured", body);
    }

    [Fact]
    public async Task CreateClinic_ReturnsBadRequest_WhenOperatingHoursContainDuplicateDays()
    {
        await _factory.ResetDatabaseAsync();

        using var client = _factory.CreateAuthenticatedClient(Guid.NewGuid(), Guid.NewGuid(), SystemRoles.Admin);
        var request = BuildCreateClinicRequest("Dup Hours Clinic", "DUP001");
        request.OperatingHours = new List<ClinicOperatingHourContract>
        {
            new() { DayOfWeek = 1, Open = "08:00", Close = "16:00", IsClosed = false },
            new() { DayOfWeek = 1, Open = "09:00", Close = "17:00", IsClosed = false }
        };

        var response = await client.PostAsJsonAsync("/api/Clinic", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Operating hours cannot contain duplicate dayOfWeek entries.", body);
    }

    [Fact]
    public async Task UpdateClinic_ReturnsConflict_WhenCodeAlreadyExistsForAnotherClinic()
    {
        var clinicToUpdateId = Guid.NewGuid();
        var otherClinicId = Guid.NewGuid();

        await _factory.ResetDatabaseAsync(async db =>
        {
            AddRequiredSystemLookups(db, includeOperationAndAccreditation: true);

            db.Clinics.AddRange(
                CreateClinic(clinicToUpdateId, "First Clinic", "FIRST1", isActive: true),
                CreateClinic(otherClinicId, "Second Clinic", "SECOND1", isActive: true));

            await Task.CompletedTask;
        });

        using var client = _factory.CreateAuthenticatedClient(Guid.NewGuid(), Guid.NewGuid(), SystemRoles.Admin);
        var request = BuildUpdateClinicRequest("First Clinic Updated", "SECOND1");
        var response = await client.PutAsJsonAsync($"/api/Clinic/{clinicToUpdateId}", request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("A clinic with this code already exists.", body);
    }

    private static CreateClinicRequest BuildCreateClinicRequest(string name, string code)
    {
        return new CreateClinicRequest
        {
            Name = name,
            Code = code,
            MainEmail = "contact@clinic.test",
            WebsiteUrl = "https://clinic.test",
            PatientPortalUrl = "https://portal.clinic.test",
            SysClinicTypeId = Clinic.DefaultSysClinicTypeId,
            SysOwnershipTypeId = Clinic.DefaultSysOwnershipTypeId,
            SysSourceSystemId = Clinic.DefaultSysSourceSystemId
        };
    }

    private static UpdateClinicRequest BuildUpdateClinicRequest(string name, string code)
    {
        return new UpdateClinicRequest
        {
            Name = name,
            Code = code,
            MainEmail = "contact@clinic.test",
            WebsiteUrl = "https://clinic.test",
            PatientPortalUrl = "https://portal.clinic.test",
            SysClinicTypeId = Clinic.DefaultSysClinicTypeId,
            SysOwnershipTypeId = Clinic.DefaultSysOwnershipTypeId,
            SysSourceSystemId = Clinic.DefaultSysSourceSystemId,
            IsActive = true
        };
    }

    private static void AddRequiredSystemLookups(AppDbContext db, bool includeOperationAndAccreditation)
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

        if (includeOperationAndAccreditation)
        {
            db.SysOperations.Add(new SysOperation
            {
                SysOperationId = 1,
                Name = "General Medicine",
                IsActive = true
            });

            db.SysAccreditations.Add(new SysAccreditation
            {
                SysAccreditationId = 1,
                Name = "JCI",
                IsActive = true
            });
        }
    }

    private static Clinic CreateClinic(Guid clinicId, string name, string code, bool isActive)
    {
        return new Clinic
        {
            ClinicId = clinicId,
            Name = name,
            Code = code,
            LegalName = $"{name} LLC",
            SysClinicTypeId = Clinic.DefaultSysClinicTypeId,
            SysOwnershipTypeId = Clinic.DefaultSysOwnershipTypeId,
            SysSourceSystemId = Clinic.DefaultSysSourceSystemId,
            Timezone = "America/Chicago",
            IsActive = isActive,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
    }
}
