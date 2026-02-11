using System.Net;
using System.Text.Json;
using MedicalAppointment.Api.IntegrationTests.Infrastructure;
using MedicalAppointment.Domain.Constants;
using MedicalAppointment.Domain.Models;

namespace MedicalAppointment.Api.IntegrationTests;

public sealed class NotificationControllerIntegrationTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;

    public NotificationControllerIntegrationTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Notifications_MarkAllRead_UpdatesUnreadCount()
    {
        var clinicId = Guid.NewGuid();
        var patientRoleId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var targetPersonId = Guid.NewGuid();
        var otherPersonId = Guid.NewGuid();

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

            db.SysRoles.Add(new SysRole
            {
                SysRoleId = patientRoleId,
                Name = SystemRoles.Patient,
                Description = "Patient",
                IsActive = true
            });

            db.Clinics.Add(new Clinic
            {
                ClinicId = clinicId,
                Name = "Clinic Notifications",
                Code = $"CN-{clinicId.ToString("N")[..6]}",
                LegalName = "Clinic Notifications LLC",
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
                    PersonId = targetPersonId,
                    FirstName = "Target",
                    LastName = "User",
                    NormalizedName = "target user",
                    PersonalIdentifier = $"PIDN-{targetPersonId.ToString("N")[..10]}",
                    Address = "Address 1",
                    BirthDate = new DateTime(1990, 1, 1)
                },
                new Person
                {
                    PersonId = otherPersonId,
                    FirstName = "Other",
                    LastName = "User",
                    NormalizedName = "other user",
                    PersonalIdentifier = $"PIDO-{otherPersonId.ToString("N")[..10]}",
                    Address = "Address 2",
                    BirthDate = new DateTime(1991, 1, 1)
                });

            db.Users.AddRange(
                new User
                {
                    UserId = targetUserId,
                    Username = $"target_{targetUserId.ToString("N")[..8]}",
                    Email = $"target_{targetUserId.ToString("N")[..8]}@example.com",
                    PasswordHash = "hash",
                    SysRoleId = patientRoleId,
                    ClinicId = clinicId,
                    PersonId = targetPersonId
                },
                new User
                {
                    UserId = otherUserId,
                    Username = $"other_{otherUserId.ToString("N")[..8]}",
                    Email = $"other_{otherUserId.ToString("N")[..8]}@example.com",
                    PasswordHash = "hash",
                    SysRoleId = patientRoleId,
                    ClinicId = clinicId,
                    PersonId = otherPersonId
                });

            db.UserNotifications.AddRange(
                new UserNotification
                {
                    UserNotificationId = Guid.NewGuid(),
                    UserId = targetUserId,
                    Type = NotificationTypes.PostponeRequested,
                    Title = "Unread 1",
                    Message = "Unread notification",
                    IsRead = false,
                    CreatedAtUtc = DateTime.UtcNow.AddMinutes(-5)
                },
                new UserNotification
                {
                    UserNotificationId = Guid.NewGuid(),
                    UserId = targetUserId,
                    Type = NotificationTypes.AppointmentCancelled,
                    Title = "Read 1",
                    Message = "Read notification",
                    IsRead = true,
                    CreatedAtUtc = DateTime.UtcNow.AddMinutes(-10),
                    ReadAtUtc = DateTime.UtcNow.AddMinutes(-8)
                },
                new UserNotification
                {
                    UserNotificationId = Guid.NewGuid(),
                    UserId = otherUserId,
                    Type = NotificationTypes.AppointmentCancelled,
                    Title = "Other user unread",
                    Message = "Unread notification for another user",
                    IsRead = false,
                    CreatedAtUtc = DateTime.UtcNow.AddMinutes(-3)
                });

            await Task.CompletedTask;
        });

        using var client = _factory.CreateAuthenticatedClient(targetUserId, clinicId, SystemRoles.Patient);

        var unreadBeforeResponse = await client.GetAsync("/api/Notification/unread-count");
        Assert.Equal(HttpStatusCode.OK, unreadBeforeResponse.StatusCode);
        var unreadBeforePayload = JsonDocument.Parse(await unreadBeforeResponse.Content.ReadAsStringAsync());
        Assert.Equal(1, unreadBeforePayload.RootElement.GetProperty("unreadCount").GetInt32());

        var markAllResponse = await client.PostAsync("/api/Notification/read-all", content: null);
        Assert.Equal(HttpStatusCode.OK, markAllResponse.StatusCode);
        var markAllPayload = JsonDocument.Parse(await markAllResponse.Content.ReadAsStringAsync());
        Assert.Equal(1, markAllPayload.RootElement.GetProperty("markedCount").GetInt32());

        var unreadAfterResponse = await client.GetAsync("/api/Notification/unread-count");
        Assert.Equal(HttpStatusCode.OK, unreadAfterResponse.StatusCode);
        var unreadAfterPayload = JsonDocument.Parse(await unreadAfterResponse.Content.ReadAsStringAsync());
        Assert.Equal(0, unreadAfterPayload.RootElement.GetProperty("unreadCount").GetInt32());
    }
}
