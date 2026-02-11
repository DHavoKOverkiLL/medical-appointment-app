namespace MedicalAppointment.Api.Configuration;

public class EmailVerificationSettings
{
    public const string SectionName = "EmailVerification";

    public bool Enabled { get; set; } = true;
    public int CodeLength { get; set; } = 6;
    public int CodeTtlMinutes { get; set; } = 10;
    public int ResendCooldownSeconds { get; set; } = 60;
    public int MaxFailedAttemptsPerCode { get; set; } = 5;
    public int MaxSendsPerDay { get; set; } = 10;
    public int BrevoTemplateId { get; set; }
    public string TemplateAppUrl { get; set; } = string.Empty;
    public string TemplateSupportEmail { get; set; } = string.Empty;
    public string HashKey { get; set; } = string.Empty;
}
