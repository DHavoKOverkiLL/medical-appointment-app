using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Mail;
using MedicalAppointment.Api.Configuration;
using Microsoft.Extensions.Options;

namespace MedicalAppointment.Api.Services;

public class ConfigurableTransactionalEmailSender : ITransactionalEmailSender
{
    private readonly ReminderEmailProviderSettings _settings;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ConfigurableTransactionalEmailSender> _logger;

    public ConfigurableTransactionalEmailSender(
        IOptions<ReminderEmailProviderSettings> settings,
        IHttpClientFactory httpClientFactory,
        ILogger<ConfigurableTransactionalEmailSender> logger)
    {
        _settings = settings.Value;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<TransactionalEmailSendResult> SendAsync(
        TransactionalEmailMessage message,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(message.RecipientEmail))
        {
            return new TransactionalEmailSendResult(
                TransactionalEmailSendStatus.Failed,
                "Recipient email is required.");
        }

        var provider = NormalizeProvider(_settings.Provider);
        if (provider == ReminderEmailProviders.None)
        {
            _logger.LogWarning(
                "Transactional email skipped because provider is not configured. Recipient={RecipientEmail}",
                message.RecipientEmail);
            return new TransactionalEmailSendResult(TransactionalEmailSendStatus.SkippedNotConfigured);
        }

        if (string.IsNullOrWhiteSpace(_settings.FromEmail))
        {
            _logger.LogWarning(
                "Transactional email skipped because FromEmail is not configured. Recipient={RecipientEmail}",
                message.RecipientEmail);
            return new TransactionalEmailSendResult(TransactionalEmailSendStatus.SkippedNotConfigured);
        }

        try
        {
            if (provider == ReminderEmailProviders.Smtp)
            {
                await SendViaSmtpAsync(message, cancellationToken);
                return new TransactionalEmailSendResult(TransactionalEmailSendStatus.Sent);
            }

            if (provider == ReminderEmailProviders.SendGrid)
            {
                await SendViaSendGridAsync(message, cancellationToken);
                return new TransactionalEmailSendResult(TransactionalEmailSendStatus.Sent);
            }

            return new TransactionalEmailSendResult(
                TransactionalEmailSendStatus.Failed,
                $"Unsupported email provider '{_settings.Provider}'.");
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Transactional email delivery failed. Provider={Provider}, Recipient={RecipientEmail}",
                provider,
                message.RecipientEmail);
            return new TransactionalEmailSendResult(TransactionalEmailSendStatus.Failed, ex.Message);
        }
    }

    private async Task SendViaSmtpAsync(TransactionalEmailMessage message, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_settings.Smtp.Host) || _settings.Smtp.Port <= 0)
        {
            throw new InvalidOperationException("SMTP host and port must be configured for transactional emails.");
        }

        using var mail = new MailMessage
        {
            Subject = message.Subject,
            Body = message.Body,
            IsBodyHtml = false
        };

        mail.From = new MailAddress(_settings.FromEmail, _settings.FromName);
        mail.To.Add(new MailAddress(message.RecipientEmail, message.RecipientDisplayName));

        using var smtpClient = new SmtpClient(_settings.Smtp.Host, _settings.Smtp.Port)
        {
            EnableSsl = _settings.Smtp.EnableSsl
        };

        if (!string.IsNullOrWhiteSpace(_settings.Smtp.Username))
        {
            smtpClient.Credentials = new NetworkCredential(_settings.Smtp.Username, _settings.Smtp.Password);
        }

        await smtpClient.SendMailAsync(mail).WaitAsync(cancellationToken);
    }

    private async Task SendViaSendGridAsync(TransactionalEmailMessage message, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_settings.SendGrid.ApiKey))
        {
            throw new InvalidOperationException("SendGrid ApiKey must be configured for transactional emails.");
        }

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
                            email = message.RecipientEmail,
                            name = message.RecipientDisplayName
                        }
                    }
                }
            },
            from = new
            {
                email = _settings.FromEmail,
                name = _settings.FromName
            },
            subject = message.Subject,
            content = new[]
            {
                new
                {
                    type = "text/plain",
                    value = message.Body
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
            $"SendGrid transactional delivery failed with status {(int)response.StatusCode}: {responseBody}");
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

        if (normalized.Equals(ReminderEmailProviders.None, StringComparison.OrdinalIgnoreCase))
        {
            return ReminderEmailProviders.None;
        }

        return normalized;
    }
}
