namespace MedicalAppointment.Api.Services;

public class MockReminderSmsSender : IReminderSmsSender
{
    private readonly ILogger<MockReminderSmsSender> _logger;

    public MockReminderSmsSender(ILogger<MockReminderSmsSender> logger)
    {
        _logger = logger;
    }

    public Task SendReminderAsync(AppointmentReminderDeliveryContext reminder, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Mock SMS reminder sent. AppointmentId={AppointmentId}, UserId={UserId}, ReminderType={ReminderType}, Message={Message}",
            reminder.AppointmentId,
            reminder.RecipientUserId,
            reminder.ReminderType,
            reminder.Message);

        return Task.CompletedTask;
    }
}
