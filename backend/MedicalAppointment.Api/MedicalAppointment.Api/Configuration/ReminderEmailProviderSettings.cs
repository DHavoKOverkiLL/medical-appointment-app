namespace MedicalAppointment.Api.Configuration;

public class ReminderEmailProviderSettings
{
    public const string SectionName = "ReminderProviders:Email";

    public string Provider { get; set; } = ReminderEmailProviders.None;
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = "Medio";
    public ReminderSmtpSettings Smtp { get; set; } = new();
    public ReminderSendGridSettings SendGrid { get; set; } = new();
}

public class ReminderSmtpSettings
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public bool EnableSsl { get; set; } = true;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class ReminderSendGridSettings
{
    public string ApiKey { get; set; } = string.Empty;
}

public static class ReminderEmailProviders
{
    public const string None = "None";
    public const string Smtp = "Smtp";
    public const string SendGrid = "SendGrid";
}
