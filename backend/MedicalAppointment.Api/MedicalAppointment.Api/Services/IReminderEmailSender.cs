namespace MedicalAppointment.Api.Services;

public interface IReminderEmailSender
{
    Task SendReminderAsync(AppointmentReminderDeliveryContext reminder, CancellationToken cancellationToken = default);
}
