using System.Net;
using System.Text;
using System.Text.Json;
using MedicalAppointment.Api.Configuration;
using MedicalAppointment.Api.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace MedicalAppointment.Api.IntegrationTests;

public sealed class ReminderProviderSendersTests
{
    [Fact]
    public void ConfigurableReminderEmailSender_AllowsMissingProviderConfiguration_WhenChannelDisabled()
    {
        var sender = CreateEmailSender(
            settings: new ReminderEmailProviderSettings
            {
                Provider = ReminderEmailProviders.None,
                FromEmail = string.Empty
            },
            reminderSettings: new AppointmentReminderSettings
            {
                Channels = new AppointmentReminderChannelSettings
                {
                    InAppEnabled = true,
                    EmailEnabled = false,
                    SmsEnabled = false
                }
            });

        Assert.NotNull(sender);
    }

    [Fact]
    public void ConfigurableReminderEmailSender_ThrowsForMissingFromEmail_WhenChannelEnabled()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => CreateEmailSender(
            settings: new ReminderEmailProviderSettings
            {
                Provider = ReminderEmailProviders.SendGrid,
                FromEmail = string.Empty,
                SendGrid = new ReminderSendGridSettings
                {
                    ApiKey = "SG.test"
                }
            },
            reminderSettings: EnabledEmailSettings()));

        Assert.Contains("FromEmail", exception.Message);
    }

    [Fact]
    public void ConfigurableReminderEmailSender_ThrowsForMissingSendGridApiKey_WhenSendGridSelected()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => CreateEmailSender(
            settings: new ReminderEmailProviderSettings
            {
                Provider = ReminderEmailProviders.SendGrid,
                FromEmail = "no-reply@medio.test",
                SendGrid = new ReminderSendGridSettings
                {
                    ApiKey = string.Empty
                }
            },
            reminderSettings: EnabledEmailSettings()));

        Assert.Contains("SendGrid:ApiKey", exception.Message);
    }

    [Fact]
    public void ConfigurableReminderEmailSender_ThrowsForMissingSmtpHost_WhenSmtpSelected()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => CreateEmailSender(
            settings: new ReminderEmailProviderSettings
            {
                Provider = ReminderEmailProviders.Smtp,
                FromEmail = "no-reply@medio.test",
                Smtp = new ReminderSmtpSettings
                {
                    Host = string.Empty,
                    Port = 587
                }
            },
            reminderSettings: EnabledEmailSettings()));

        Assert.Contains("Smtp:Host", exception.Message);
    }

    [Fact]
    public void ConfigurableReminderEmailSender_ThrowsForInvalidSmtpPort_WhenSmtpSelected()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => CreateEmailSender(
            settings: new ReminderEmailProviderSettings
            {
                Provider = ReminderEmailProviders.Smtp,
                FromEmail = "no-reply@medio.test",
                Smtp = new ReminderSmtpSettings
                {
                    Host = "smtp.medio.test",
                    Port = 0
                }
            },
            reminderSettings: EnabledEmailSettings()));

        Assert.Contains("Smtp:Port", exception.Message);
    }

    [Fact]
    public async Task ConfigurableReminderEmailSender_SkipsRequest_WhenRecipientEmailMissing()
    {
        var handler = new CapturingHttpMessageHandler();
        var factory = new CapturingHttpClientFactory(handler);
        var sender = CreateEmailSender(
            settings: SendGridEmailSettings(),
            reminderSettings: EnabledEmailSettings(),
            httpClientFactory: factory);

        await sender.SendReminderAsync(BuildReminder(recipientEmail: null));

        Assert.Empty(handler.Requests);
        Assert.Equal(0, factory.CreateClientCalls);
    }

    [Fact]
    public async Task ConfigurableReminderEmailSender_UsesSendGridProvider_WhenConfigured()
    {
        var handler = new CapturingHttpMessageHandler((_, _) => new HttpResponseMessage(HttpStatusCode.Accepted));
        var factory = new CapturingHttpClientFactory(handler);
        var sender = CreateEmailSender(
            settings: SendGridEmailSettings(),
            reminderSettings: EnabledEmailSettings(),
            httpClientFactory: factory);

        await sender.SendReminderAsync(BuildReminder());

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("https://api.sendgrid.com/v3/mail/send", request.RequestUri);
        Assert.Equal("Bearer", request.AuthorizationScheme);
        Assert.Equal("SG.test", request.AuthorizationParameter);
        Assert.Equal("application/json", request.ContentType);
        Assert.Equal(1, factory.CreateClientCalls);

        using var body = JsonDocument.Parse(request.Body);
        Assert.Equal("Reminder title", body.RootElement.GetProperty("subject").GetString());
        Assert.Equal(
            "patient@medio.test",
            body.RootElement.GetProperty("personalizations")[0].GetProperty("to")[0].GetProperty("email").GetString());
        Assert.Equal(
            "Appointment reminder body",
            body.RootElement.GetProperty("content")[0].GetProperty("value").GetString());
    }

    [Fact]
    public async Task ConfigurableReminderEmailSender_ThrowsWithStatusCode_WhenSendGridFails()
    {
        var handler = new CapturingHttpMessageHandler((_, _) =>
            new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("invalid payload")
            });
        var sender = CreateEmailSender(
            settings: SendGridEmailSettings(),
            reminderSettings: EnabledEmailSettings(),
            httpClientFactory: new CapturingHttpClientFactory(handler));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => sender.SendReminderAsync(BuildReminder()));

        Assert.Contains("status 400", exception.Message);
        Assert.Contains("invalid payload", exception.Message);
    }

    [Fact]
    public async Task ConfigurableReminderEmailSender_ThrowsForUnsupportedProvider()
    {
        var sender = CreateEmailSender(
            settings: new ReminderEmailProviderSettings
            {
                Provider = "CustomProvider",
                FromEmail = "no-reply@medio.test"
            },
            reminderSettings: EnabledEmailSettings());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => sender.SendReminderAsync(BuildReminder()));

        Assert.Contains("Unsupported email reminder provider", exception.Message);
    }

    [Fact]
    public void ConfigurableReminderSmsSender_AllowsMissingProviderConfiguration_WhenChannelDisabled()
    {
        var sender = CreateSmsSender(
            settings: new ReminderSmsProviderSettings
            {
                Provider = ReminderSmsProviders.None
            },
            reminderSettings: new AppointmentReminderSettings
            {
                Channels = new AppointmentReminderChannelSettings
                {
                    InAppEnabled = true,
                    EmailEnabled = false,
                    SmsEnabled = false
                }
            });

        Assert.NotNull(sender);
    }

    [Fact]
    public void ConfigurableReminderSmsSender_ThrowsForMissingAccountSid_WhenTwilioSelected()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => CreateSmsSender(
            settings: new ReminderSmsProviderSettings
            {
                Provider = ReminderSmsProviders.Twilio,
                Twilio = new TwilioReminderSmsSettings
                {
                    AccountSid = string.Empty,
                    AuthToken = "auth-token",
                    FromNumber = "+15550000000"
                }
            },
            reminderSettings: EnabledSmsSettings()));

        Assert.Contains("AccountSid", exception.Message);
    }

    [Fact]
    public void ConfigurableReminderSmsSender_ThrowsForMissingAuthToken_WhenTwilioSelected()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => CreateSmsSender(
            settings: new ReminderSmsProviderSettings
            {
                Provider = ReminderSmsProviders.Twilio,
                Twilio = new TwilioReminderSmsSettings
                {
                    AccountSid = "AC123",
                    AuthToken = string.Empty,
                    FromNumber = "+15550000000"
                }
            },
            reminderSettings: EnabledSmsSettings()));

        Assert.Contains("AuthToken", exception.Message);
    }

    [Fact]
    public void ConfigurableReminderSmsSender_ThrowsForMissingFromAndMessagingService_WhenTwilioSelected()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => CreateSmsSender(
            settings: new ReminderSmsProviderSettings
            {
                Provider = ReminderSmsProviders.Twilio,
                Twilio = new TwilioReminderSmsSettings
                {
                    AccountSid = "AC123",
                    AuthToken = "auth-token",
                    FromNumber = string.Empty,
                    MessagingServiceSid = string.Empty
                }
            },
            reminderSettings: EnabledSmsSettings()));

        Assert.Contains("FromNumber or MessagingServiceSid", exception.Message);
    }

    [Fact]
    public async Task ConfigurableReminderSmsSender_SkipsRequest_WhenRecipientPhoneMissing()
    {
        var handler = new CapturingHttpMessageHandler();
        var factory = new CapturingHttpClientFactory(handler);
        var sender = CreateSmsSender(
            settings: TwilioSmsSettings(fromNumber: "+15550000000"),
            reminderSettings: EnabledSmsSettings(),
            httpClientFactory: factory);

        await sender.SendReminderAsync(BuildReminder(recipientPhone: null));

        Assert.Empty(handler.Requests);
        Assert.Equal(0, factory.CreateClientCalls);
    }

    [Fact]
    public async Task ConfigurableReminderSmsSender_UsesTwilioFromNumber_WhenConfigured()
    {
        var handler = new CapturingHttpMessageHandler((_, _) => new HttpResponseMessage(HttpStatusCode.Created));
        var factory = new CapturingHttpClientFactory(handler);
        var sender = CreateSmsSender(
            settings: TwilioSmsSettings(fromNumber: "+15550000000"),
            reminderSettings: EnabledSmsSettings(),
            httpClientFactory: factory);

        await sender.SendReminderAsync(BuildReminder());

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("https://api.twilio.com/2010-04-01/Accounts/AC123/Messages.json", request.RequestUri);
        Assert.Equal("Basic", request.AuthorizationScheme);
        Assert.Equal(Convert.ToBase64String(Encoding.ASCII.GetBytes("AC123:auth-token")), request.AuthorizationParameter);
        Assert.Equal("application/x-www-form-urlencoded", request.ContentType);
        Assert.Equal(1, factory.CreateClientCalls);

        var form = ParseFormBody(request.Body);
        Assert.Equal("+15551112222", form["To"]);
        Assert.Equal("Appointment reminder body", form["Body"]);
        Assert.Equal("+15550000000", form["From"]);
        Assert.DoesNotContain("MessagingServiceSid", form.Keys);
    }

    [Fact]
    public async Task ConfigurableReminderSmsSender_UsesMessagingServiceSid_WhenConfigured()
    {
        var handler = new CapturingHttpMessageHandler((_, _) => new HttpResponseMessage(HttpStatusCode.Created));
        var sender = CreateSmsSender(
            settings: TwilioSmsSettings(fromNumber: null, messagingServiceSid: "MG999"),
            reminderSettings: EnabledSmsSettings(),
            httpClientFactory: new CapturingHttpClientFactory(handler));

        await sender.SendReminderAsync(BuildReminder());

        var request = Assert.Single(handler.Requests);
        var form = ParseFormBody(request.Body);
        Assert.Equal("+15551112222", form["To"]);
        Assert.Equal("Appointment reminder body", form["Body"]);
        Assert.Equal("MG999", form["MessagingServiceSid"]);
        Assert.DoesNotContain("From", form.Keys);
    }

    [Fact]
    public async Task ConfigurableReminderSmsSender_ThrowsWithStatusCode_WhenTwilioFails()
    {
        var handler = new CapturingHttpMessageHandler((_, _) =>
            new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("twilio rejected request")
            });
        var sender = CreateSmsSender(
            settings: TwilioSmsSettings(fromNumber: "+15550000000"),
            reminderSettings: EnabledSmsSettings(),
            httpClientFactory: new CapturingHttpClientFactory(handler));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => sender.SendReminderAsync(BuildReminder()));

        Assert.Contains("status 400", exception.Message);
        Assert.Contains("twilio rejected request", exception.Message);
    }

    [Fact]
    public async Task ConfigurableReminderSmsSender_ThrowsForUnsupportedProvider()
    {
        var sender = CreateSmsSender(
            settings: new ReminderSmsProviderSettings
            {
                Provider = "CustomSms",
                Twilio = new TwilioReminderSmsSettings
                {
                    AccountSid = "AC123",
                    AuthToken = "auth-token",
                    FromNumber = "+15550000000"
                }
            },
            reminderSettings: EnabledSmsSettings());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => sender.SendReminderAsync(BuildReminder()));

        Assert.Contains("Unsupported SMS reminder provider", exception.Message);
    }

    private static ConfigurableReminderEmailSender CreateEmailSender(
        ReminderEmailProviderSettings settings,
        AppointmentReminderSettings reminderSettings,
        IHttpClientFactory? httpClientFactory = null)
    {
        return new ConfigurableReminderEmailSender(
            Options.Create(settings),
            Options.Create(reminderSettings),
            httpClientFactory ?? new CapturingHttpClientFactory(new CapturingHttpMessageHandler()),
            NullLogger<ConfigurableReminderEmailSender>.Instance);
    }

    private static ConfigurableReminderSmsSender CreateSmsSender(
        ReminderSmsProviderSettings settings,
        AppointmentReminderSettings reminderSettings,
        IHttpClientFactory? httpClientFactory = null)
    {
        return new ConfigurableReminderSmsSender(
            Options.Create(settings),
            Options.Create(reminderSettings),
            httpClientFactory ?? new CapturingHttpClientFactory(new CapturingHttpMessageHandler()),
            NullLogger<ConfigurableReminderSmsSender>.Instance);
    }

    private static AppointmentReminderSettings EnabledEmailSettings()
    {
        return new AppointmentReminderSettings
        {
            Channels = new AppointmentReminderChannelSettings
            {
                InAppEnabled = true,
                EmailEnabled = true,
                SmsEnabled = false
            }
        };
    }

    private static AppointmentReminderSettings EnabledSmsSettings()
    {
        return new AppointmentReminderSettings
        {
            Channels = new AppointmentReminderChannelSettings
            {
                InAppEnabled = true,
                EmailEnabled = false,
                SmsEnabled = true
            }
        };
    }

    private static ReminderEmailProviderSettings SendGridEmailSettings()
    {
        return new ReminderEmailProviderSettings
        {
            Provider = ReminderEmailProviders.SendGrid,
            FromEmail = "no-reply@medio.test",
            FromName = "Medio",
            SendGrid = new ReminderSendGridSettings
            {
                ApiKey = "SG.test"
            }
        };
    }

    private static ReminderSmsProviderSettings TwilioSmsSettings(string? fromNumber, string? messagingServiceSid = null)
    {
        return new ReminderSmsProviderSettings
        {
            Provider = ReminderSmsProviders.Twilio,
            Twilio = new TwilioReminderSmsSettings
            {
                AccountSid = "AC123",
                AuthToken = "auth-token",
                FromNumber = fromNumber ?? string.Empty,
                MessagingServiceSid = messagingServiceSid ?? string.Empty
            }
        };
    }

    private static AppointmentReminderDeliveryContext BuildReminder(string? recipientEmail = "patient@medio.test", string? recipientPhone = "+15551112222")
    {
        return new AppointmentReminderDeliveryContext(
            AppointmentId: Guid.NewGuid(),
            RecipientUserId: Guid.NewGuid(),
            RecipientEmail: recipientEmail,
            RecipientPhoneNumber: recipientPhone,
            RecipientDisplayName: "Patient Test",
            ReminderType: "Reminder24Hours",
            Title: "Reminder title",
            Message: "Appointment reminder body",
            AppointmentDateTimeUtc: DateTime.UtcNow.AddHours(2),
            ClinicName: "Medio Clinic",
            ClinicTimezoneId: "UTC",
            DoctorName: "Doctor Test");
    }

    private static Dictionary<string, string> ParseFormBody(string body)
    {
        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var part in body.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var pieces = part.Split('=', 2);
            var key = Uri.UnescapeDataString(pieces[0].Replace("+", " "));
            var value = pieces.Length > 1
                ? Uri.UnescapeDataString(pieces[1].Replace("+", " "))
                : string.Empty;
            map[key] = value;
        }

        return map;
    }

    private sealed class CapturingHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpClient _httpClient;

        public CapturingHttpClientFactory(HttpMessageHandler handler)
        {
            _httpClient = new HttpClient(handler);
        }

        public int CreateClientCalls { get; private set; }

        public HttpClient CreateClient(string name)
        {
            CreateClientCalls++;
            return _httpClient;
        }
    }

    private sealed class CapturingHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, string, HttpResponseMessage> _responder;

        public CapturingHttpMessageHandler(Func<HttpRequestMessage, string, HttpResponseMessage>? responder = null)
        {
            _responder = responder ?? ((_, _) => new HttpResponseMessage(HttpStatusCode.OK));
        }

        public List<CapturedRequest> Requests { get; } = new();

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var body = request.Content is null
                ? string.Empty
                : await request.Content.ReadAsStringAsync(cancellationToken);

            Requests.Add(new CapturedRequest(
                Method: request.Method,
                RequestUri: request.RequestUri?.ToString(),
                AuthorizationScheme: request.Headers.Authorization?.Scheme,
                AuthorizationParameter: request.Headers.Authorization?.Parameter,
                ContentType: request.Content?.Headers.ContentType?.MediaType,
                Body: body));

            return _responder(request, body);
        }
    }

    private sealed record CapturedRequest(
        HttpMethod Method,
        string? RequestUri,
        string? AuthorizationScheme,
        string? AuthorizationParameter,
        string? ContentType,
        string Body);
}
