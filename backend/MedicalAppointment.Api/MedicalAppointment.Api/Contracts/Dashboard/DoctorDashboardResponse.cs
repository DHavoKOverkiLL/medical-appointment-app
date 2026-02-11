namespace MedicalAppointment.Api.Contracts.Dashboard;

public class DoctorDashboardResponse
{
    public string ClinicName { get; set; } = string.Empty;
    public int AppointmentsToday { get; set; }
    public int UpcomingAppointments { get; set; }
    public int UniquePatientsUpcoming { get; set; }
    public DateTime? NextAppointmentUtc { get; set; }
}
