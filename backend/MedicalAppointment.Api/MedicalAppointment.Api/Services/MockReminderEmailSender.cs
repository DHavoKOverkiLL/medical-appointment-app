namespace MedicalAppointment.Api.Services;

public class MockReminderEmailSender : IReminderEmailSender
{
    private readonly ILogger<MockReminderEmailSender> _logger;

    public MockReminderEmailSender(ILogger<MockReminderEmailSender> logger)
    {
        _logger = logger;
    }

    public Task SendReminderAsync(AppointmentReminderDeliveryContext reminder, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(reminder.RecipientEmail))
        {
            _logger.LogWarning(
                "Mock email reminder skipped because recipient email is missing. AppointmentId={AppointmentId}, UserId={UserId}, ReminderType={ReminderType}",
                reminder.AppointmentId,
                reminder.RecipientUserId,
                reminder.ReminderType);
            return Task.CompletedTask;
        }

        _logger.LogInformation(
            "Mock email reminder sent to {Email}. AppointmentId={AppointmentId}, UserId={UserId}, ReminderType={ReminderType}, Subject={Subject}",
            reminder.RecipientEmail,
            reminder.AppointmentId,
            reminder.RecipientUserId,
            reminder.ReminderType,
            reminder.Title);

        return Task.CompletedTask;
    }
}
