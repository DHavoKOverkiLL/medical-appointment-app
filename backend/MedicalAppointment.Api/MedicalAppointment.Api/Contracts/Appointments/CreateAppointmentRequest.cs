using System.ComponentModel.DataAnnotations;

namespace MedicalAppointment.Api.Contracts.Appointments;

public class CreateAppointmentRequest
{
    [Required]
    public Guid DoctorId { get; set; }

    [Required]
    public DateTime AppointmentDateTime { get; set; }
}

