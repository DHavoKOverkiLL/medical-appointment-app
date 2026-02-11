using System.Reflection;
using MedicalAppointment.Api.Configuration;
using MedicalAppointment.Api.Services;
using MedicalAppointment.Domain.Constants;
using MedicalAppointment.Domain.Models;
using MedicalAppointment.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace MedicalAppointment.Api.IntegrationTests;

public sealed class AppointmentReminderBackgroundServiceIntegrationTests
{
    [Fact]
    public async Task DispatchDueReminders_CreatesReminderDispatchesOnlyWithinWindow()
    {
        var dbName = $"ReminderDispatchWindow-{Guid.NewGuid():N}";
        await using var provider = CreateProvider(dbName);
        var nowUtc = DateTime.UtcNow;

        await SeedReminderScenarioAsync(provider, nowUtc, patientEmail: "patient-window@example.com", patientPhone: "+15550000001");

        await using var scope = provider.CreateAsyncScope();
        var service = CreateBackgroundService(
            scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>(),
            new AppointmentReminderSettings
            {
                Enabled = true,
                PollIntervalSeconds = 60,
                DispatchWindowMinutes = 15,
                Channels = new AppointmentReminderChannelSettings
                {
                    InAppEnabled = true,
                    EmailEnabled = false,
                    SmsEnabled = false
                }
            },
            new RecordingEmailSender(),
            new RecordingSmsSender());

        await InvokeDispatchDueRemindersAsync(service, CancellationToken.None);

        await using var assertScope = provider.CreateAsyncScope();
        var db = assertScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var dispatches = await db.AppointmentReminderDispatches.AsNoTracking().ToListAsync();
        var notifications = await db.UserNotifications.AsNoTracking().ToListAsync();

        Assert.Equal(3, dispatches.Count);
        Assert.Equal(3, notifications.Count);

        Assert.Contains(dispatches, d => d.ReminderType == AppointmentReminderTypes.Reminder24Hours);
        Assert.Contains(dispatches, d => d.ReminderType == AppointmentReminderTypes.Reminder2Hours);
        Assert.Contains(dispatches, d => d.ReminderType == AppointmentReminderTypes.Reminder15Minutes);
        Assert.All(notifications, n => Assert.Equal(NotificationTypes.AppointmentReminder, n.Type));
    }

    [Fact]
    public async Task DispatchDueReminders_IsIdempotentAcrossMultipleRuns()
    {
        var dbName = $"ReminderDispatchIdempotent-{Guid.NewGuid():N}";
        await using var provider = CreateProvider(dbName);
        var nowUtc = DateTime.UtcNow;

        await SeedSingleDueAppointmentAsync(provider, nowUtc, patientEmail: "patient-idempotent@example.com", patientPhone: "+15550000002");

        await using var scope = provider.CreateAsyncScope();
        var service = CreateBackgroundService(
            scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>(),
            new AppointmentReminderSettings
            {
                Enabled = true,
                PollIntervalSeconds = 60,
                DispatchWindowMinutes = 15,
                Channels = new AppointmentReminderChannelSettings
                {
                    InAppEnabled = true,
                    EmailEnabled = false,
                    SmsEnabled = false
                }
            },
            new RecordingEmailSender(),
            new RecordingSmsSender());

        await InvokeDispatchDueRemindersAsync(service, CancellationToken.None);
        await InvokeDispatchDueRemindersAsync(service, CancellationToken.None);

        await using var assertScope = provider.CreateAsyncScope();
        var db = assertScope.ServiceProvider.GetRequiredService<AppDbContext>();

        Assert.Equal(1, await db.AppointmentReminderDispatches.CountAsync());
        Assert.Equal(1, await db.UserNotifications.CountAsync());
    }

    [Fact]
    public async Task DispatchDueReminders_HonorsChannelToggles_ForInAppEmailAndSms()
    {
        var dbName = $"ReminderDispatchChannels-{Guid.NewGuid():N}";
        await using var provider = CreateProvider(dbName);
        var nowUtc = DateTime.UtcNow;

        var seeded = await SeedSingleDueAppointmentAsync(
            provider,
            nowUtc,
            patientEmail: "patient-channels@example.com",
            patientPhone: "+15550000003");

        var emailSender = new RecordingEmailSender();
        var smsSender = new RecordingSmsSender();

        await using var scope = provider.CreateAsyncScope();
        var service = CreateBackgroundService(
            scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>(),
            new AppointmentReminderSettings
            {
                Enabled = true,
                PollIntervalSeconds = 60,
                DispatchWindowMinutes = 15,
                Channels = new AppointmentReminderChannelSettings
                {
                    InAppEnabled = false,
                    EmailEnabled = true,
                    SmsEnabled = true
                }
            },
            emailSender,
            smsSender);

        await InvokeDispatchDueRemindersAsync(service, CancellationToken.None);

        await using var assertScope = provider.CreateAsyncScope();
        var db = assertScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var notifications = await db.UserNotifications.AsNoTracking().ToListAsync();
        var dispatches = await db.AppointmentReminderDispatches.AsNoTracking().ToListAsync();

        Assert.Empty(notifications);
        Assert.Single(dispatches);

        var emailDeliveries = emailSender.Deliveries;
        var smsDeliveries = smsSender.Deliveries;

        Assert.Single(emailDeliveries);
        Assert.Single(smsDeliveries);
        Assert.Equal(seeded.AppointmentId, emailDeliveries[0].AppointmentId);
        Assert.Equal(seeded.AppointmentId, smsDeliveries[0].AppointmentId);
        Assert.Equal("+15550000003", smsDeliveries[0].RecipientPhoneNumber);
    }

    [Fact]
    public void ConfigurableEmailSender_ThrowsForMissingProvider_WhenEmailChannelEnabled()
    {
        var settings = Options.Create(new ReminderEmailProviderSettings
        {
            Provider = ReminderEmailProviders.None,
            FromEmail = string.Empty
        });

        var reminderSettings = Options.Create(new AppointmentReminderSettings
        {
            Channels = new AppointmentReminderChannelSettings
            {
                InAppEnabled = true,
                EmailEnabled = true,
                SmsEnabled = false
            }
        });

        Assert.Throws<InvalidOperationException>(() =>
            new ConfigurableReminderEmailSender(
                settings,
                reminderSettings,
                new StubHttpClientFactory(),
                NullLogger<ConfigurableReminderEmailSender>.Instance));
    }

    [Fact]
    public void ConfigurableSmsSender_ThrowsForMissingProvider_WhenSmsChannelEnabled()
    {
        var settings = Options.Create(new ReminderSmsProviderSettings
        {
            Provider = ReminderSmsProviders.None
        });

        var reminderSettings = Options.Create(new AppointmentReminderSettings
        {
            Channels = new AppointmentReminderChannelSettings
            {
                InAppEnabled = true,
                EmailEnabled = false,
                SmsEnabled = true
            }
        });

        Assert.Throws<InvalidOperationException>(() =>
            new ConfigurableReminderSmsSender(
                settings,
                reminderSettings,
                new StubHttpClientFactory(),
                NullLogger<ConfigurableReminderSmsSender>.Instance));
    }

    private static ServiceProvider CreateProvider(string databaseName)
    {
        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(databaseName));
        return services.BuildServiceProvider();
    }

    private static async Task<SeededAppointment> SeedSingleDueAppointmentAsync(
        ServiceProvider provider,
        DateTime nowUtc,
        string patientEmail,
        string? patientPhone)
    {
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var seed = BuildSeedGraph(patientEmail, patientPhone);
        db.AddRange(seed.SeedEntities);

        var appointmentId = Guid.NewGuid();
        db.Appointments.Add(new Appointment
        {
            AppointmentId = appointmentId,
            DoctorId = seed.DoctorUserId,
            PatientId = seed.PatientUserId,
            ClinicId = seed.ClinicId,
            AppointmentDateTime = nowUtc.AddMinutes(15).AddMinutes(-5),
            Status = AppointmentStatuses.Scheduled,
            PostponeRequestStatus = AppointmentPostponeStatuses.None
        });

        await db.SaveChangesAsync();
        return new SeededAppointment(appointmentId, seed.PatientUserId);
    }

    private static async Task SeedReminderScenarioAsync(
        ServiceProvider provider,
        DateTime nowUtc,
        string patientEmail,
        string? patientPhone)
    {
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var seed = BuildSeedGraph(patientEmail, patientPhone);
        db.AddRange(seed.SeedEntities);

        db.Appointments.AddRange(
            new Appointment
            {
                AppointmentId = Guid.NewGuid(),
                DoctorId = seed.DoctorUserId,
                PatientId = seed.PatientUserId,
                ClinicId = seed.ClinicId,
                AppointmentDateTime = nowUtc.AddHours(24).AddMinutes(-5),
                Status = AppointmentStatuses.Scheduled,
                PostponeRequestStatus = AppointmentPostponeStatuses.None
            },
            new Appointment
            {
                AppointmentId = Guid.NewGuid(),
                DoctorId = seed.DoctorUserId,
                PatientId = seed.PatientUserId,
                ClinicId = seed.ClinicId,
                AppointmentDateTime = nowUtc.AddHours(2).AddMinutes(-5),
                Status = AppointmentStatuses.Scheduled,
                PostponeRequestStatus = AppointmentPostponeStatuses.None
            },
            new Appointment
            {
                AppointmentId = Guid.NewGuid(),
                DoctorId = seed.DoctorUserId,
                PatientId = seed.PatientUserId,
                ClinicId = seed.ClinicId,
                AppointmentDateTime = nowUtc.AddMinutes(15).AddMinutes(-5),
                Status = AppointmentStatuses.Scheduled,
                PostponeRequestStatus = AppointmentPostponeStatuses.None
            },
            new Appointment
            {
                AppointmentId = Guid.NewGuid(),
                DoctorId = seed.DoctorUserId,
                PatientId = seed.PatientUserId,
                ClinicId = seed.ClinicId,
                AppointmentDateTime = nowUtc.AddHours(24).AddMinutes(5),
                Status = AppointmentStatuses.Scheduled,
                PostponeRequestStatus = AppointmentPostponeStatuses.None
            },
            new Appointment
            {
                AppointmentId = Guid.NewGuid(),
                DoctorId = seed.DoctorUserId,
                PatientId = seed.PatientUserId,
                ClinicId = seed.ClinicId,
                AppointmentDateTime = nowUtc.AddHours(24).AddMinutes(-30),
                Status = AppointmentStatuses.Scheduled,
                PostponeRequestStatus = AppointmentPostponeStatuses.None
            },
            new Appointment
            {
                AppointmentId = Guid.NewGuid(),
                DoctorId = seed.DoctorUserId,
                PatientId = seed.PatientUserId,
                ClinicId = seed.ClinicId,
                AppointmentDateTime = nowUtc.AddMinutes(15).AddMinutes(-5),
                Status = AppointmentStatuses.Cancelled,
                PostponeRequestStatus = AppointmentPostponeStatuses.None
            });

        await db.SaveChangesAsync();
    }

    private static AppointmentReminderBackgroundService CreateBackgroundService(
        IServiceScopeFactory scopeFactory,
        AppointmentReminderSettings settings,
        IReminderEmailSender emailSender,
        IReminderSmsSender smsSender)
    {
        return new AppointmentReminderBackgroundService(
            scopeFactory,
            Options.Create(settings),
            emailSender,
            smsSender,
            NullLogger<AppointmentReminderBackgroundService>.Instance);
    }

    private static async Task InvokeDispatchDueRemindersAsync(
        AppointmentReminderBackgroundService service,
        CancellationToken cancellationToken)
    {
        var method = typeof(AppointmentReminderBackgroundService)
            .GetMethod("DispatchDueReminders", BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(method);
        var task = method!.Invoke(service, new object?[] { cancellationToken }) as Task;
        Assert.NotNull(task);
        await task!;
    }

    private static ReminderSeed BuildSeedGraph(string patientEmail, string? patientPhone)
    {
        var clinicId = Guid.NewGuid();
        var doctorRoleId = Guid.NewGuid();
        var patientRoleId = Guid.NewGuid();
        var doctorPersonId = Guid.NewGuid();
        var patientPersonId = Guid.NewGuid();
        var doctorUserId = Guid.NewGuid();
        var patientUserId = Guid.NewGuid();

        var clinicType = new SysClinicType
        {
            SysClinicTypeId = Clinic.DefaultSysClinicTypeId,
            Name = "Primary Care",
            IsActive = true
        };

        var ownershipType = new SysOwnershipType
        {
            SysOwnershipTypeId = Clinic.DefaultSysOwnershipTypeId,
            Name = "Physician Owned",
            IsActive = true
        };

        var sourceSystem = new SysSourceSystem
        {
            SysSourceSystemId = Clinic.DefaultSysSourceSystemId,
            Name = "EHR",
            IsActive = true
        };

        var doctorRole = new SysRole
        {
            SysRoleId = doctorRoleId,
            Name = SystemRoles.Doctor,
            Description = "Doctor",
            IsActive = true
        };

        var patientRole = new SysRole
        {
            SysRoleId = patientRoleId,
            Name = SystemRoles.Patient,
            Description = "Patient",
            IsActive = true
        };

        var clinic = new Clinic
        {
            ClinicId = clinicId,
            Name = "Reminder Clinic",
            Code = $"RC-{clinicId.ToString("N")[..6]}",
            LegalName = "Reminder Clinic LLC",
            SysClinicTypeId = Clinic.DefaultSysClinicTypeId,
            SysOwnershipTypeId = Clinic.DefaultSysOwnershipTypeId,
            SysSourceSystemId = Clinic.DefaultSysSourceSystemId,
            Timezone = "UTC",
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        var doctorPerson = new Person
        {
            PersonId = doctorPersonId,
            FirstName = "Doctor",
            LastName = "Reminder",
            NormalizedName = "doctor reminder",
            PersonalIdentifier = $"DOC-{doctorPersonId.ToString("N")[..10]}",
            Address = "Doctor Street",
            BirthDate = new DateTime(1985, 1, 1)
        };

        var patientPerson = new Person
        {
            PersonId = patientPersonId,
            FirstName = "Patient",
            LastName = "Reminder",
            NormalizedName = "patient reminder",
            PersonalIdentifier = $"PAT-{patientPersonId.ToString("N")[..10]}",
            Address = "Patient Street",
            PhoneNumber = patientPhone,
            BirthDate = new DateTime(1991, 1, 1)
        };

        var doctorUser = new User
        {
            UserId = doctorUserId,
            Username = $"doctor_{doctorUserId.ToString("N")[..8]}",
            Email = $"doctor_{doctorUserId.ToString("N")[..8]}@example.com",
            PasswordHash = "hash",
            SysRoleId = doctorRoleId,
            ClinicId = clinicId,
            PersonId = doctorPersonId
        };

        var patientUser = new User
        {
            UserId = patientUserId,
            Username = $"patient_{patientUserId.ToString("N")[..8]}",
            Email = patientEmail,
            PasswordHash = "hash",
            SysRoleId = patientRoleId,
            ClinicId = clinicId,
            PersonId = patientPersonId
        };

        var entities = new object[]
        {
            clinicType,
            ownershipType,
            sourceSystem,
            doctorRole,
            patientRole,
            clinic,
            doctorPerson,
            patientPerson,
            doctorUser,
            patientUser
        };

        return new ReminderSeed(entities, clinicId, doctorUserId, patientUserId);
    }

    private sealed record ReminderSeed(object[] SeedEntities, Guid ClinicId, Guid DoctorUserId, Guid PatientUserId);
    private sealed record SeededAppointment(Guid AppointmentId, Guid PatientUserId);

    private sealed class RecordingEmailSender : IReminderEmailSender
    {
        private readonly List<AppointmentReminderDeliveryContext> _deliveries = new();
        public IReadOnlyList<AppointmentReminderDeliveryContext> Deliveries => _deliveries;

        public Task SendReminderAsync(AppointmentReminderDeliveryContext reminder, CancellationToken cancellationToken = default)
        {
            _deliveries.Add(reminder);
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingSmsSender : IReminderSmsSender
    {
        private readonly List<AppointmentReminderDeliveryContext> _deliveries = new();
        public IReadOnlyList<AppointmentReminderDeliveryContext> Deliveries => _deliveries;

        public Task SendReminderAsync(AppointmentReminderDeliveryContext reminder, CancellationToken cancellationToken = default)
        {
            _deliveries.Add(reminder);
            return Task.CompletedTask;
        }
    }

    private sealed class StubHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name)
        {
            return new HttpClient();
        }
    }
}
