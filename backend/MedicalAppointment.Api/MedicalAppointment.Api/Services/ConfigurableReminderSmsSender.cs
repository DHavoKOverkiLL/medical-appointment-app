using System.Net.Http.Headers;
using System.Text;
using MedicalAppointment.Api.Configuration;
using Microsoft.Extensions.Options;

namespace MedicalAppointment.Api.Services;

public class ConfigurableReminderSmsSender : IReminderSmsSender
{
    private readonly ReminderSmsProviderSettings _settings;
    private readonly AppointmentReminderSettings _reminderSettings;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ConfigurableReminderSmsSender> _logger;

    public ConfigurableReminderSmsSender(
        IOptions<ReminderSmsProviderSettings> settings,
        IOptions<AppointmentReminderSettings> reminderSettings,
        IHttpClientFactory httpClientFactory,
        ILogger<ConfigurableReminderSmsSender> logger)
    {
        _settings = settings.Value;
        _reminderSettings = reminderSettings.Value;
        _httpClientFactory = httpClientFactory;
        _logger = logger;

        ValidateConfiguration();
    }

    public async Task SendReminderAsync(AppointmentReminderDeliveryContext reminder, CancellationToken cancellationToken = default)
    {
        if (!_reminderSettings.Channels.SmsEnabled)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(reminder.RecipientPhoneNumber))
        {
            _logger.LogWarning(
                "SMS reminder skipped because recipient phone number is missing. AppointmentId={AppointmentId}, UserId={UserId}, ReminderType={ReminderType}",
                reminder.AppointmentId,
                reminder.RecipientUserId,
                reminder.ReminderType);
            return;
        }

        var provider = NormalizeProvider(_settings.Provider);
        if (provider == ReminderSmsProviders.None)
        {
            _logger.LogWarning(
                "SMS reminder skipped because provider is disabled. AppointmentId={AppointmentId}, UserId={UserId}, ReminderType={ReminderType}",
                reminder.AppointmentId,
                reminder.RecipientUserId,
                reminder.ReminderType);
            return;
        }

        if (provider == ReminderSmsProviders.Twilio)
        {
            await SendViaTwilioAsync(reminder, cancellationToken);
            return;
        }

        throw new InvalidOperationException($"Unsupported SMS reminder provider '{_settings.Provider}'.");
    }

    private void ValidateConfiguration()
    {
        if (!_reminderSettings.Channels.SmsEnabled)
        {
            return;
        }

        var provider = NormalizeProvider(_settings.Provider);
        if (provider == ReminderSmsProviders.None)
        {
            throw new InvalidOperationException(
                "Appointment reminder SMS channel is enabled, but ReminderProviders:Sms:Provider is set to None.");
        }

        if (provider == ReminderSmsProviders.Twilio)
        {
            if (string.IsNullOrWhiteSpace(_settings.Twilio.AccountSid))
            {
                throw new InvalidOperationException(
                    "ReminderProviders:Sms:Twilio:AccountSid is required when using Twilio SMS provider.");
            }

            if (string.IsNullOrWhiteSpace(_settings.Twilio.AuthToken))
            {
                throw new InvalidOperationException(
                    "ReminderProviders:Sms:Twilio:AuthToken is required when using Twilio SMS provider.");
            }

            if (string.IsNullOrWhiteSpace(_settings.Twilio.FromNumber) &&
                string.IsNullOrWhiteSpace(_settings.Twilio.MessagingServiceSid))
            {
                throw new InvalidOperationException(
                    "ReminderProviders:Sms:Twilio requires either FromNumber or MessagingServiceSid.");
            }
        }
    }

    private async Task SendViaTwilioAsync(AppointmentReminderDeliveryContext reminder, CancellationToken cancellationToken)
    {
        var twilio = _settings.Twilio;
        var endpoint = $"https://api.twilio.com/2010-04-01/Accounts/{twilio.AccountSid}/Messages.json";
        var formValues = new List<KeyValuePair<string, string>>
        {
            new("To", reminder.RecipientPhoneNumber!),
            new("Body", reminder.Message)
        };

        if (!string.IsNullOrWhiteSpace(twilio.MessagingServiceSid))
        {
            formValues.Add(new KeyValuePair<string, string>("MessagingServiceSid", twilio.MessagingServiceSid));
        }
        else
        {
            formValues.Add(new KeyValuePair<string, string>("From", twilio.FromNumber));
        }

        var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new FormUrlEncodedContent(formValues)
        };

        var basicAuth = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{twilio.AccountSid}:{twilio.AuthToken}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", basicAuth);

        var client = _httpClientFactory.CreateClient();
        using var response = await client.SendAsync(request, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        throw new InvalidOperationException(
            $"Twilio reminder delivery failed with status {(int)response.StatusCode}: {responseBody}");
    }

    private static string NormalizeProvider(string? provider)
    {
        var normalized = provider?.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return ReminderSmsProviders.None;
        }

        if (normalized.Equals(ReminderSmsProviders.Twilio, StringComparison.OrdinalIgnoreCase))
        {
            return ReminderSmsProviders.Twilio;
        }

        if (normalized.Equals(ReminderSmsProviders.None, StringComparison.OrdinalIgnoreCase))
        {
            return ReminderSmsProviders.None;
        }

        return normalized;
    }
}
