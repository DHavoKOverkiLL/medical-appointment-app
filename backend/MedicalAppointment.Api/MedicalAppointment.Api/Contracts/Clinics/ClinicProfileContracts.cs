using System.ComponentModel.DataAnnotations;

namespace MedicalAppointment.Api.Contracts.Clinics;

public class ClinicOperatingHourContract
{
    [Range(0, 6)]
    public int DayOfWeek { get; set; }

    [MaxLength(5)]
    public string? Open { get; set; }

    [MaxLength(5)]
    public string? Close { get; set; }

    public bool IsClosed { get; set; }
}

public class ClinicServiceContract
{
    [Range(1, int.MaxValue)]
    public int SysOperationId { get; set; }

    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    public bool IsTelehealthAvailable { get; set; }
}

public class ClinicInsurancePlanContract
{
    [Required]
    [MaxLength(120)]
    public string PayerName { get; set; } = string.Empty;

    [Required]
    [MaxLength(120)]
    public string PlanName { get; set; } = string.Empty;

    public bool IsInNetwork { get; set; } = true;
}

public class ClinicAccreditationContract
{
    [Range(1, int.MaxValue)]
    public int SysAccreditationId { get; set; }

    [MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    public DateOnly? EffectiveOn { get; set; }
    public DateOnly? ExpiresOn { get; set; }
}
