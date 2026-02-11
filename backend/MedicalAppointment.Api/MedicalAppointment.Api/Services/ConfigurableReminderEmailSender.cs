using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Mail;
using MedicalAppointment.Api.Configuration;
using Microsoft.Extensions.Options;

namespace MedicalAppointment.Api.Services;

public class ConfigurableReminderEmailSender : IReminderEmailSender
{
    private readonly ReminderEmailProviderSettings _settings;
    private readonly AppointmentReminderSettings _reminderSettings;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ConfigurableReminderEmailSender> _logger;

    public ConfigurableReminderEmailSender(
        IOptions<ReminderEmailProviderSettings> settings,
        IOptions<AppointmentReminderSettings> reminderSettings,
        IHttpClientFactory httpClientFactory,
        ILogger<ConfigurableReminderEmailSender> logger)
    {
        _settings = settings.Value;
        _reminderSettings = reminderSettings.Value;
        _httpClientFactory = httpClientFactory;
        _logger = logger;

        ValidateConfiguration();
    }

    public async Task SendReminderAsync(AppointmentReminderDeliveryContext reminder, CancellationToken cancellationToken = default)
    {
        if (!_reminderSettings.Channels.EmailEnabled)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(reminder.RecipientEmail))
        {
            _logger.LogWarning(
                "Email reminder skipped because recipient email is missing. AppointmentId={AppointmentId}, UserId={UserId}, ReminderType={ReminderType}",
                reminder.AppointmentId,
                reminder.RecipientUserId,
                reminder.ReminderType);
            return;
        }

        var provider = NormalizeProvider(_settings.Provider);
        if (provider == ReminderEmailProviders.None)
        {
            _logger.LogWarning(
                "Email reminder skipped because provider is disabled. AppointmentId={AppointmentId}, UserId={UserId}, ReminderType={ReminderType}",
                reminder.AppointmentId,
                reminder.RecipientUserId,
                reminder.ReminderType);
            return;
        }

        if (provider == ReminderEmailProviders.Smtp)
        {
            await SendViaSmtpAsync(reminder, cancellationToken);
            return;
        }

        if (provider == ReminderEmailProviders.SendGrid)
        {
            await SendViaSendGridAsync(reminder, cancellationToken);
            return;
        }

        if (provider == ReminderEmailProviders.Brevo)
        {
            await SendViaBrevoAsync(reminder, cancellationToken);
            return;
        }

        throw new InvalidOperationException($"Unsupported email reminder provider '{_settings.Provider}'.");
    }

    private void ValidateConfiguration()
    {
        if (!_reminderSettings.Channels.EmailEnabled)
        {
            return;
        }

        var provider = NormalizeProvider(_settings.Provider);
        if (provider == ReminderEmailProviders.None)
        {
            throw new InvalidOperationException(
                "Appointment reminder email channel is enabled, but ReminderProviders:Email:Provider is set to None.");
        }

        if (string.IsNullOrWhiteSpace(_settings.FromEmail))
        {
            throw new InvalidOperationException(
                "ReminderProviders:Email:FromEmail is required when email reminders are enabled.");
        }

        if (provider == ReminderEmailProviders.Smtp)
        {
            if (string.IsNullOrWhiteSpace(_settings.Smtp.Host))
            {
                throw new InvalidOperationException(
                    "ReminderProviders:Email:Smtp:Host is required when using SMTP email provider.");
            }

            if (_settings.Smtp.Port <= 0)
            {
                throw new InvalidOperationException(
                    "ReminderProviders:Email:Smtp:Port must be a positive value.");
            }
        }

        if (provider == ReminderEmailProviders.SendGrid && string.IsNullOrWhiteSpace(_settings.SendGrid.ApiKey))
        {
            throw new InvalidOperationException(
                "ReminderProviders:Email:SendGrid:ApiKey is required when using SendGrid email provider.");
        }

        if (provider == ReminderEmailProviders.Brevo && string.IsNullOrWhiteSpace(_settings.Brevo.ApiKey))
        {
            throw new InvalidOperationException(
                "ReminderProviders:Email:Brevo:ApiKey is required when using Brevo email provider.");
        }
    }

    private async Task SendViaSmtpAsync(AppointmentReminderDeliveryContext reminder, CancellationToken cancellationToken)
    {
        using var message = new MailMessage
        {
            Subject = reminder.Title,
            Body = reminder.Message,
            IsBodyHtml = false
        };

        message.From = new MailAddress(_settings.FromEmail, _settings.FromName);
        message.To.Add(new MailAddress(reminder.RecipientEmail!, reminder.RecipientDisplayName));

        using var smtpClient = new SmtpClient(_settings.Smtp.Host, _settings.Smtp.Port)
        {
            EnableSsl = _settings.Smtp.EnableSsl
        };

        if (!string.IsNullOrWhiteSpace(_settings.Smtp.Username))
        {
            smtpClient.Credentials = new NetworkCredential(_settings.Smtp.Username, _settings.Smtp.Password);
        }

        await smtpClient.SendMailAsync(message).WaitAsync(cancellationToken);
    }

    private async Task SendViaSendGridAsync(AppointmentReminderDeliveryContext reminder, CancellationToken cancellationToken)
    {
        var payload = new
        {
            personalizations = new[]
            {
                new
                {
                    to = new[]
                    {
                        new
                        {
                            email = reminder.RecipientEmail,
                            name = reminder.RecipientDisplayName
                        }
                    }
                }
            },
            from = new
            {
                email = _settings.FromEmail,
                name = _settings.FromName
            },
            subject = reminder.Title,
            content = new[]
            {
                new
                {
                    type = "text/plain",
                    value = reminder.Message
                }
            }
        };

        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.sendgrid.com/v3/mail/send");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.SendGrid.ApiKey);
        request.Content = JsonContent.Create(payload);

        var client = _httpClientFactory.CreateClient();
        using var response = await client.SendAsync(request, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        throw new InvalidOperationException(
            $"SendGrid reminder delivery failed with status {(int)response.StatusCode}: {responseBody}");
    }

    private async Task SendViaBrevoAsync(AppointmentReminderDeliveryContext reminder, CancellationToken cancellationToken)
    {
        var payload = new
        {
            sender = new
            {
                email = _settings.FromEmail,
                name = _settings.FromName
            },
            to = new[]
            {
                new
                {
                    email = reminder.RecipientEmail,
                    name = reminder.RecipientDisplayName
                }
            },
            subject = reminder.Title,
            textContent = reminder.Message
        };

        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.brevo.com/v3/smtp/email");
        request.Headers.TryAddWithoutValidation("api-key", _settings.Brevo.ApiKey);
        request.Content = JsonContent.Create(payload);

        var client = _httpClientFactory.CreateClient();
        using var response = await client.SendAsync(request, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        throw new InvalidOperationException(
            $"Brevo reminder delivery failed with status {(int)response.StatusCode}: {responseBody}");
    }

    private static string NormalizeProvider(string? provider)
    {
        var normalized = provider?.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return ReminderEmailProviders.None;
        }

        if (normalized.Equals(ReminderEmailProviders.Smtp, StringComparison.OrdinalIgnoreCase))
        {
            return ReminderEmailProviders.Smtp;
        }

        if (normalized.Equals(ReminderEmailProviders.SendGrid, StringComparison.OrdinalIgnoreCase))
        {
            return ReminderEmailProviders.SendGrid;
        }

        if (normalized.Equals(ReminderEmailProviders.Brevo, StringComparison.OrdinalIgnoreCase))
        {
            return ReminderEmailProviders.Brevo;
        }

        if (normalized.Equals(ReminderEmailProviders.None, StringComparison.OrdinalIgnoreCase))
        {
            return ReminderEmailProviders.None;
        }

        return normalized;
    }
}
