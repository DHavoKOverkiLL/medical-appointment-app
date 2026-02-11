namespace MedicalAppointment.Api.Contracts.Dashboard;

public class AdminDashboardResponse
{
    public int Clinics { get; set; }
    public int Users { get; set; }
    public int Doctors { get; set; }
    public int Patients { get; set; }
    public int Admins { get; set; }
    public int AppointmentsToday { get; set; }
    public int UpcomingAppointments { get; set; }
    public List<ClinicLoadResponse> ClinicLoad { get; set; } = new();
}

public class ClinicLoadResponse
{
    public Guid ClinicId { get; set; }
    public string ClinicName { get; set; } = string.Empty;
    public int Users { get; set; }
    public int Appointments { get; set; }
}
