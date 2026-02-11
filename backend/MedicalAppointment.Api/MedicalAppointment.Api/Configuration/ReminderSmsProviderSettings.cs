namespace MedicalAppointment.Api.Configuration;

public class ReminderSmsProviderSettings
{
    public const string SectionName = "ReminderProviders:Sms";

    public string Provider { get; set; } = ReminderSmsProviders.None;
    public TwilioReminderSmsSettings Twilio { get; set; } = new();
}

public class TwilioReminderSmsSettings
{
    public string AccountSid { get; set; } = string.Empty;
    public string AuthToken { get; set; } = string.Empty;
    public string FromNumber { get; set; } = string.Empty;
    public string MessagingServiceSid { get; set; } = string.Empty;
}

public static class ReminderSmsProviders
{
    public const string None = "None";
    public const string Twilio = "Twilio";
}
