namespace MedicalAppointment.Api.Contracts.Dashboard;

public class PatientDashboardResponse
{
    public string ClinicName { get; set; } = string.Empty;
    public int UpcomingAppointments { get; set; }
    public DateTime? NextAppointmentUtc { get; set; }
    public int DoctorsInClinic { get; set; }
}
