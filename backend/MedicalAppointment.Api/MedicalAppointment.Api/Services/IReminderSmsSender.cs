namespace MedicalAppointment.Api.Services;

public interface IReminderSmsSender
{
    Task SendReminderAsync(AppointmentReminderDeliveryContext reminder, CancellationToken cancellationToken = default);
}
