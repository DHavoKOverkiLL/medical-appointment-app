namespace MedicalAppointment.Api.Configuration;

public class AppointmentReminderSettings
{
    public const string SectionName = "AppointmentReminders";

    public bool Enabled { get; set; } = true;
    public int PollIntervalSeconds { get; set; } = 60;
    public int DispatchWindowMinutes { get; set; } = 15;
    public AppointmentReminderChannelSettings Channels { get; set; } = new();
}

public class AppointmentReminderChannelSettings
{
    public bool InAppEnabled { get; set; } = true;
    public bool EmailEnabled { get; set; }
    public bool SmsEnabled { get; set; }
}
