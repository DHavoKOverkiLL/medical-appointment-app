using MedicalAppointment.Api.Configuration;
using MedicalAppointment.Domain.Constants;
using MedicalAppointment.Domain.Models;
using MedicalAppointment.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace MedicalAppointment.Api.Services;

public class AppointmentReminderBackgroundService : BackgroundService
{
    private static readonly ReminderDefinition[] ReminderDefinitions =
    [
        new(AppointmentReminderTypes.Reminder24Hours, TimeSpan.FromHours(24), "24-hour appointment reminder"),
        new(AppointmentReminderTypes.Reminder2Hours, TimeSpan.FromHours(2), "2-hour appointment reminder"),
        new(AppointmentReminderTypes.Reminder15Minutes, TimeSpan.FromMinutes(15), "15-minute appointment reminder")
    ];

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AppointmentReminderBackgroundService> _logger;
    private readonly AppointmentReminderSettings _settings;
    private readonly IReminderEmailSender _emailSender;
    private readonly IReminderSmsSender _smsSender;

    public AppointmentReminderBackgroundService(
        IServiceScopeFactory scopeFactory,
        IOptions<AppointmentReminderSettings> settings,
        IReminderEmailSender emailSender,
        IReminderSmsSender smsSender,
        ILogger<AppointmentReminderBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _settings = settings.Value;
        _emailSender = emailSender;
        _smsSender = smsSender;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_settings.Enabled)
        {
            _logger.LogInformation("Appointment reminder background service is disabled by configuration.");
            return;
        }

        var pollIntervalSeconds = Math.Clamp(_settings.PollIntervalSeconds, 10, 600);
        var pollInterval = TimeSpan.FromSeconds(pollIntervalSeconds);
        _logger.LogInformation(
            "Appointment reminder background service started with poll interval {PollIntervalSeconds}s.",
            pollIntervalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await DispatchDueReminders(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Appointment reminder dispatch iteration failed.");
            }

            try
            {
                await Task.Delay(pollInterval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    private async Task DispatchDueReminders(CancellationToken cancellationToken)
    {
        var channels = _settings.Channels ?? new AppointmentReminderChannelSettings();
        if (!channels.InAppEnabled && !channels.EmailEnabled && !channels.SmsEnabled)
        {
            return;
        }

        var nowUtc = DateTime.UtcNow;
        var maxOffset = ReminderDefinitions.Max(definition => definition.Offset);
        var maxAppointmentUtc = nowUtc.Add(maxOffset).AddHours(1);
        var dispatchWindowMinutes = Math.Clamp(_settings.DispatchWindowMinutes, 1, 240);
        var dispatchWindow = TimeSpan.FromMinutes(dispatchWindowMinutes);

        await using var scope = _scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var candidates = await context.Appointments
            .AsNoTracking()
            .Where(a =>
                a.Status == AppointmentStatuses.Scheduled &&
                a.AppointmentDateTime > nowUtc &&
                a.AppointmentDateTime <= maxAppointmentUtc)
            .Select(a => new ReminderCandidate
            {
                AppointmentId = a.AppointmentId,
                AppointmentDateTimeUtc = a.AppointmentDateTime,
                ClinicName = a.Clinic.Name,
                ClinicTimeZoneId = a.Clinic.Timezone,
                DoctorName = (a.Doctor.Person.FirstName + " " + a.Doctor.Person.LastName).Trim(),
                PatientUserId = a.PatientId,
                PatientDisplayName = (a.Patient.Person.FirstName + " " + a.Patient.Person.LastName).Trim(),
                PatientEmail = a.Patient.Email
            })
            .ToListAsync(cancellationToken);

        if (candidates.Count == 0)
        {
            return;
        }

        var appointmentIds = candidates.Select(x => x.AppointmentId).Distinct().ToList();
        var reminderTypes = ReminderDefinitions.Select(x => x.Type).ToList();
        var existingDispatches = await context.AppointmentReminderDispatches
            .AsNoTracking()
            .Where(d =>
                appointmentIds.Contains(d.AppointmentId) &&
                reminderTypes.Contains(d.ReminderType))
            .Select(d => new ReminderDispatchKey(d.AppointmentId, d.RecipientUserId, d.ReminderType))
            .ToListAsync(cancellationToken);

        var existingDispatchSet = existingDispatches.ToHashSet();
        var dueCount = 0;
        var outboundDeliveries = new List<AppointmentReminderDeliveryContext>();

        foreach (var candidate in candidates)
        {
            foreach (var definition in ReminderDefinitions)
            {
                var dueAtUtc = candidate.AppointmentDateTimeUtc - definition.Offset;
                if (nowUtc < dueAtUtc)
                {
                    continue;
                }

                var overdueBy = nowUtc - dueAtUtc;
                if (overdueBy > dispatchWindow)
                {
                    continue;
                }

                var dispatchKey = new ReminderDispatchKey(
                    candidate.AppointmentId,
                    candidate.PatientUserId,
                    definition.Type);

                if (existingDispatchSet.Contains(dispatchKey))
                {
                    continue;
                }

                var reminderMessage = BuildReminderMessage(candidate, definition);

                if (channels.InAppEnabled)
                {
                    context.UserNotifications.Add(new UserNotification
                    {
                        UserNotificationId = Guid.NewGuid(),
                        UserId = candidate.PatientUserId,
                        AppointmentId = candidate.AppointmentId,
                        ActorUserId = null,
                        Type = NotificationTypes.AppointmentReminder,
                        Title = definition.Title,
                        Message = reminderMessage,
                        IsRead = false,
                        CreatedAtUtc = nowUtc
                    });
                }

                context.AppointmentReminderDispatches.Add(new AppointmentReminderDispatch
                {
                    AppointmentReminderDispatchId = Guid.NewGuid(),
                    AppointmentId = candidate.AppointmentId,
                    RecipientUserId = candidate.PatientUserId,
                    ReminderType = definition.Type,
                    ScheduledForUtc = dueAtUtc,
                    SentAtUtc = nowUtc
                });

                outboundDeliveries.Add(new AppointmentReminderDeliveryContext(
                    candidate.AppointmentId,
                    candidate.PatientUserId,
                    candidate.PatientEmail,
                    candidate.PatientDisplayName,
                    definition.Type,
                    definition.Title,
                    reminderMessage,
                    candidate.AppointmentDateTimeUtc,
                    candidate.ClinicName,
                    candidate.ClinicTimeZoneId,
                    candidate.DoctorName));

                existingDispatchSet.Add(dispatchKey);
                dueCount++;
            }
        }

        if (dueCount == 0)
        {
            return;
        }

        await context.SaveChangesAsync(cancellationToken);

        if (channels.EmailEnabled || channels.SmsEnabled)
        {
            foreach (var delivery in outboundDeliveries)
            {
                if (channels.EmailEnabled)
                {
                    try
                    {
                        await _emailSender.SendReminderAsync(delivery, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(
                            ex,
                            "Email reminder dispatch failed. AppointmentId={AppointmentId}, UserId={UserId}, ReminderType={ReminderType}",
                            delivery.AppointmentId,
                            delivery.RecipientUserId,
                            delivery.ReminderType);
                    }
                }

                if (channels.SmsEnabled)
                {
                    try
                    {
                        await _smsSender.SendReminderAsync(delivery, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(
                            ex,
                            "SMS reminder dispatch failed. AppointmentId={AppointmentId}, UserId={UserId}, ReminderType={ReminderType}",
                            delivery.AppointmentId,
                            delivery.RecipientUserId,
                            delivery.ReminderType);
                    }
                }
            }
        }

        _logger.LogInformation("Dispatched {Count} appointment reminders.", dueCount);
    }

    private static string BuildReminderMessage(ReminderCandidate candidate, ReminderDefinition definition)
    {
        var tz = ResolveClinicTimeZone(candidate.ClinicTimeZoneId);
        var appointmentLocal = TimeZoneInfo.ConvertTimeFromUtc(candidate.AppointmentDateTimeUtc, tz);
        var doctorSegment = string.IsNullOrWhiteSpace(candidate.DoctorName)
            ? "your doctor"
            : $"Dr. {candidate.DoctorName}";
        var clinicSegment = string.IsNullOrWhiteSpace(candidate.ClinicName)
            ? "your clinic"
            : candidate.ClinicName;

        return $"{definition.DisplayLabel}: appointment with {doctorSegment} at {clinicSegment} on {appointmentLocal:yyyy-MM-dd HH:mm} ({tz.Id}).";
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

    private sealed class ReminderCandidate
    {
        public Guid AppointmentId { get; set; }
        public DateTime AppointmentDateTimeUtc { get; set; }
        public Guid PatientUserId { get; set; }
        public string PatientDisplayName { get; set; } = string.Empty;
        public string? PatientEmail { get; set; }
        public string DoctorName { get; set; } = string.Empty;
        public string ClinicName { get; set; } = string.Empty;
        public string ClinicTimeZoneId { get; set; } = "UTC";
    }

    private sealed record ReminderDispatchKey(Guid AppointmentId, Guid RecipientUserId, string ReminderType);
    private sealed record ReminderDefinition(string Type, TimeSpan Offset, string DisplayLabel)
    {
        public string Title => DisplayLabel;
    }
}
