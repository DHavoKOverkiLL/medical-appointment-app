using MedicalAppointment.Domain.Models;
using MedicalAppointment.Domain.Constants;
using Microsoft.EntityFrameworkCore;

namespace MedicalAppointment.Infrastructure;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<Person> Persons { get; set; }
    public DbSet<SysRole> SysRoles { get; set; }
    public DbSet<SysOperation> SysOperations { get; set; }
    public DbSet<SysAccreditation> SysAccreditations { get; set; }
    public DbSet<SysClinicType> SysClinicTypes { get; set; }
    public DbSet<SysOwnershipType> SysOwnershipTypes { get; set; }
    public DbSet<SysSourceSystem> SysSourceSystems { get; set; }
    public DbSet<Clinic> Clinics { get; set; }
    public DbSet<ClinicOperatingHour> ClinicOperatingHours { get; set; }
    public DbSet<ClinicService> ClinicServices { get; set; }
    public DbSet<ClinicInsurancePlan> ClinicInsurancePlans { get; set; }
    public DbSet<ClinicAccreditation> ClinicAccreditations { get; set; }
    public DbSet<DoctorAvailabilityWindow> DoctorAvailabilityWindows { get; set; }
    public DbSet<DoctorAvailabilityBreak> DoctorAvailabilityBreaks { get; set; }
    public DbSet<DoctorAvailabilityOverride> DoctorAvailabilityOverrides { get; set; }
    public DbSet<Appointment> Appointments { get; set; }
    public DbSet<UserNotification> UserNotifications { get; set; }
    public DbSet<AppointmentAuditEvent> AppointmentAuditEvents { get; set; }
    public DbSet<AppointmentReminderDispatch> AppointmentReminderDispatches { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>()
            .HasOne(u => u.Person)
            .WithMany()
            .HasForeignKey(u => u.PersonId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<User>()
            .HasOne(u => u.Clinic)
            .WithMany(c => c.Users)
            .HasForeignKey(u => u.ClinicId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<User>()
            .Property(u => u.UserId)
            .HasDefaultValueSql("NEWID()");

        modelBuilder.Entity<User>()
            .Property(u => u.Username)
            .IsRequired()
            .HasMaxLength(100);

        modelBuilder.Entity<User>()
            .Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(256);

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<User>()
            .HasIndex(u => u.ClinicId);

        modelBuilder.Entity<Person>()
            .Property(p => p.PersonId)
            .HasDefaultValueSql("NEWID()");

        modelBuilder.Entity<Person>()
            .Property(p => p.FirstName)
            .IsRequired()
            .HasMaxLength(100);

        modelBuilder.Entity<Person>()
            .Property(p => p.LastName)
            .IsRequired()
            .HasMaxLength(100);

        modelBuilder.Entity<Person>()
            .Property(p => p.PersonalIdentifier)
            .IsRequired()
            .HasMaxLength(64);

        modelBuilder.Entity<Person>()
            .HasIndex(p => p.PersonalIdentifier)
            .IsUnique();

        modelBuilder.Entity<SysRole>(entity =>
        {
            entity.HasKey(r => r.SysRoleId);
            entity.Property(r => r.Name).IsRequired().HasMaxLength(100);
            entity.Property(r => r.Description).HasMaxLength(200);
            entity.Property(r => r.IsActive).HasDefaultValue(true);
            entity.HasIndex(r => r.Name).IsUnique();
        });

        modelBuilder.Entity<SysOperation>(entity =>
        {
            entity.HasKey(o => o.SysOperationId);
            entity.Property(o => o.SysOperationId).UseIdentityColumn();
            entity.Property(o => o.Name).IsRequired().HasMaxLength(150);
            entity.Property(o => o.IsActive).HasDefaultValue(true);
            entity.HasIndex(o => o.Name).IsUnique();
        });

        modelBuilder.Entity<SysAccreditation>(entity =>
        {
            entity.HasKey(a => a.SysAccreditationId);
            entity.Property(a => a.SysAccreditationId).UseIdentityColumn();
            entity.Property(a => a.Name).IsRequired().HasMaxLength(120);
            entity.Property(a => a.IsActive).HasDefaultValue(true);
            entity.HasIndex(a => a.Name).IsUnique();
        });

        modelBuilder.Entity<SysClinicType>(entity =>
        {
            entity.HasKey(t => t.SysClinicTypeId);
            entity.Property(t => t.SysClinicTypeId).UseIdentityColumn();
            entity.Property(t => t.Name).IsRequired().HasMaxLength(120);
            entity.Property(t => t.IsActive).HasDefaultValue(true);
            entity.HasIndex(t => t.Name).IsUnique();
        });

        modelBuilder.Entity<SysOwnershipType>(entity =>
        {
            entity.HasKey(t => t.SysOwnershipTypeId);
            entity.Property(t => t.SysOwnershipTypeId).UseIdentityColumn();
            entity.Property(t => t.Name).IsRequired().HasMaxLength(120);
            entity.Property(t => t.IsActive).HasDefaultValue(true);
            entity.HasIndex(t => t.Name).IsUnique();
        });

        modelBuilder.Entity<SysSourceSystem>(entity =>
        {
            entity.HasKey(t => t.SysSourceSystemId);
            entity.Property(t => t.SysSourceSystemId).UseIdentityColumn();
            entity.Property(t => t.Name).IsRequired().HasMaxLength(120);
            entity.Property(t => t.IsActive).HasDefaultValue(true);
            entity.HasIndex(t => t.Name).IsUnique();
        });

        modelBuilder.Entity<User>()
            .HasOne(u => u.SysRole)
            .WithMany(r => r.Users)
            .HasForeignKey(u => u.SysRoleId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Clinic>(entity =>
        {
            entity.HasKey(c => c.ClinicId);
            entity.Property(c => c.ClinicId).HasDefaultValueSql("NEWID()");
            entity.Property(c => c.Name).IsRequired().HasMaxLength(150);
            entity.Property(c => c.Code).IsRequired().HasMaxLength(32);
            entity.Property(c => c.LegalName).HasMaxLength(200).HasDefaultValue(string.Empty);
            entity.Property(c => c.SysClinicTypeId).HasDefaultValue(Clinic.DefaultSysClinicTypeId);
            entity.Property(c => c.SysOwnershipTypeId).HasDefaultValue(Clinic.DefaultSysOwnershipTypeId);
            entity.Property(c => c.FoundedOn).HasColumnType("date");

            entity.Property(c => c.NpiOrganization).HasMaxLength(20).HasDefaultValue(string.Empty);
            entity.Property(c => c.Ein).HasMaxLength(20).HasDefaultValue(string.Empty);
            entity.Property(c => c.TaxonomyCode).HasMaxLength(32).HasDefaultValue(string.Empty);
            entity.Property(c => c.StateLicenseFacility).HasMaxLength(64).HasDefaultValue(string.Empty);
            entity.Property(c => c.CliaNumber).HasMaxLength(32).HasDefaultValue(string.Empty);

            entity.Property(c => c.AddressLine1).HasMaxLength(200).HasDefaultValue(string.Empty);
            entity.Property(c => c.AddressLine2).HasMaxLength(200).HasDefaultValue(string.Empty);
            entity.Property(c => c.City).HasMaxLength(100).HasDefaultValue(string.Empty);
            entity.Property(c => c.State).HasMaxLength(100).HasDefaultValue(string.Empty);
            entity.Property(c => c.PostalCode).HasMaxLength(32).HasDefaultValue(string.Empty);
            entity.Property(c => c.CountryCode).HasMaxLength(2).HasDefaultValue("US");
            entity.Property(c => c.Timezone).HasMaxLength(64).HasDefaultValue("America/Chicago");

            entity.Property(c => c.MainPhone).HasMaxLength(32).HasDefaultValue(string.Empty);
            entity.Property(c => c.Fax).HasMaxLength(32).HasDefaultValue(string.Empty);
            entity.Property(c => c.MainEmail).HasMaxLength(256).HasDefaultValue(string.Empty);
            entity.Property(c => c.WebsiteUrl).HasMaxLength(256).HasDefaultValue(string.Empty);
            entity.Property(c => c.PatientPortalUrl).HasMaxLength(256).HasDefaultValue(string.Empty);

            entity.Property(c => c.BookingMethods).HasMaxLength(256).HasDefaultValue(string.Empty);
            entity.Property(c => c.SameDayAvailable).HasDefaultValue(false);

            entity.Property(c => c.HipaaNoticeVersion).HasMaxLength(32).HasDefaultValue(string.Empty);
            entity.Property(c => c.LastSecurityRiskAssessmentOn).HasColumnType("date");
            entity.Property(c => c.SysSourceSystemId).HasDefaultValue(Clinic.DefaultSysSourceSystemId);

            entity.Property(c => c.CreatedAtUtc).HasDefaultValueSql("GETUTCDATE()");
            entity.Property(c => c.UpdatedAtUtc).HasDefaultValueSql("GETUTCDATE()");
            entity.Property(c => c.IsActive).HasDefaultValue(true);
            entity.HasIndex(c => c.Name).IsUnique();
            entity.HasIndex(c => c.Code).IsUnique();
            entity.HasIndex(c => c.SysClinicTypeId);
            entity.HasIndex(c => c.SysOwnershipTypeId);
            entity.HasIndex(c => c.SysSourceSystemId);

            entity.HasOne(c => c.SysClinicType)
                .WithMany(t => t.Clinics)
                .HasForeignKey(c => c.SysClinicTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(c => c.SysOwnershipType)
                .WithMany(t => t.Clinics)
                .HasForeignKey(c => c.SysOwnershipTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(c => c.SysSourceSystem)
                .WithMany(t => t.Clinics)
                .HasForeignKey(c => c.SysSourceSystemId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ClinicOperatingHour>(entity =>
        {
            entity.HasKey(h => h.ClinicOperatingHourId);
            entity.Property(h => h.ClinicOperatingHourId).HasDefaultValueSql("NEWID()");
            entity.Property(h => h.OpenTime).HasColumnType("time");
            entity.Property(h => h.CloseTime).HasColumnType("time");
            entity.Property(h => h.IsClosed).HasDefaultValue(false);

            entity.HasIndex(h => new { h.ClinicId, h.DayOfWeek }).IsUnique();
            entity.HasOne(h => h.Clinic)
                .WithMany(c => c.OperatingHours)
                .HasForeignKey(h => h.ClinicId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.ToTable(t => t.HasCheckConstraint("CK_ClinicOperatingHours_DayOfWeek", "[DayOfWeek] >= 0 AND [DayOfWeek] <= 6"));
        });

        modelBuilder.Entity<ClinicService>(entity =>
        {
            entity.HasKey(s => s.ClinicServiceId);
            entity.Property(s => s.ClinicServiceId).HasDefaultValueSql("NEWID()");
            entity.Property(s => s.SysOperationId).IsRequired();
            entity.Property(s => s.IsTelehealthAvailable).HasDefaultValue(false);
            entity.Property(s => s.IsActive).HasDefaultValue(true);

            entity.HasIndex(s => new { s.ClinicId, s.SysOperationId }).IsUnique();
            entity.HasOne(s => s.Clinic)
                .WithMany(c => c.Services)
                .HasForeignKey(s => s.ClinicId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(s => s.SysOperation)
                .WithMany(o => o.ClinicServices)
                .HasForeignKey(s => s.SysOperationId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ClinicInsurancePlan>(entity =>
        {
            entity.HasKey(p => p.ClinicInsurancePlanId);
            entity.Property(p => p.ClinicInsurancePlanId).HasDefaultValueSql("NEWID()");
            entity.Property(p => p.PayerName).IsRequired().HasMaxLength(120);
            entity.Property(p => p.PlanName).IsRequired().HasMaxLength(120);
            entity.Property(p => p.IsInNetwork).HasDefaultValue(true);
            entity.Property(p => p.IsActive).HasDefaultValue(true);

            entity.HasIndex(p => new { p.ClinicId, p.PayerName, p.PlanName }).IsUnique();
            entity.HasOne(p => p.Clinic)
                .WithMany(c => c.InsurancePlans)
                .HasForeignKey(p => p.ClinicId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ClinicAccreditation>(entity =>
        {
            entity.HasKey(a => a.ClinicAccreditationId);
            entity.Property(a => a.ClinicAccreditationId).HasDefaultValueSql("NEWID()");
            entity.Property(a => a.SysAccreditationId).IsRequired();
            entity.Property(a => a.EffectiveOn).HasColumnType("date");
            entity.Property(a => a.ExpiresOn).HasColumnType("date");
            entity.Property(a => a.IsActive).HasDefaultValue(true);

            entity.HasIndex(a => new { a.ClinicId, a.SysAccreditationId }).IsUnique();
            entity.HasOne(a => a.Clinic)
                .WithMany(c => c.Accreditations)
                .HasForeignKey(a => a.ClinicId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(a => a.SysAccreditation)
                .WithMany(s => s.ClinicAccreditations)
                .HasForeignKey(a => a.SysAccreditationId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Appointment>()
            .Property(a => a.AppointmentId)
            .HasDefaultValueSql("NEWID()");

        modelBuilder.Entity<Appointment>()
            .Property(a => a.Status)
            .IsRequired()
            .HasMaxLength(20)
            .HasDefaultValue(AppointmentStatuses.Scheduled);

        modelBuilder.Entity<Appointment>()
            .Property(a => a.PostponeRequestStatus)
            .IsRequired()
            .HasMaxLength(20)
            .HasDefaultValue(AppointmentPostponeStatuses.None);

        modelBuilder.Entity<Appointment>()
            .Property(a => a.PostponeReason)
            .HasMaxLength(500);

        modelBuilder.Entity<Appointment>()
            .Property(a => a.DoctorResponseNote)
            .HasMaxLength(500);

        modelBuilder.Entity<Appointment>()
            .Property(a => a.CancellationReason)
            .HasMaxLength(500);

        modelBuilder.Entity<Appointment>()
            .HasOne(a => a.Doctor)
            .WithMany()
            .HasForeignKey(a => a.DoctorId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Appointment>()
            .HasOne(a => a.Patient)
            .WithMany()
            .HasForeignKey(a => a.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Appointment>()
            .HasOne(a => a.Clinic)
            .WithMany()
            .HasForeignKey(a => a.ClinicId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Appointment>()
            .HasOne(a => a.CancelledByUser)
            .WithMany()
            .HasForeignKey(a => a.CancelledByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Appointment>()
            .HasIndex(a => a.ClinicId);

        modelBuilder.Entity<Appointment>()
            .HasIndex(a => a.Status);

        modelBuilder.Entity<UserNotification>(entity =>
        {
            entity.HasKey(n => n.UserNotificationId);
            entity.Property(n => n.UserNotificationId).HasDefaultValueSql("NEWID()");
            entity.Property(n => n.Type).IsRequired().HasMaxLength(64);
            entity.Property(n => n.Title).IsRequired().HasMaxLength(160);
            entity.Property(n => n.Message).IsRequired().HasMaxLength(800);
            entity.Property(n => n.IsRead).HasDefaultValue(false);
            entity.Property(n => n.CreatedAtUtc).HasDefaultValueSql("GETUTCDATE()");

            entity.HasIndex(n => n.UserId);
            entity.HasIndex(n => n.AppointmentId);
            entity.HasIndex(n => new { n.UserId, n.IsRead, n.CreatedAtUtc });

            entity.HasOne(n => n.User)
                .WithMany()
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(n => n.ActorUser)
                .WithMany()
                .HasForeignKey(n => n.ActorUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(n => n.Appointment)
                .WithMany()
                .HasForeignKey(n => n.AppointmentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AppointmentAuditEvent>(entity =>
        {
            entity.HasKey(a => a.AppointmentAuditEventId);
            entity.Property(a => a.AppointmentAuditEventId).HasDefaultValueSql("NEWID()");
            entity.Property(a => a.ActorRole).IsRequired().HasMaxLength(32).HasDefaultValue(string.Empty);
            entity.Property(a => a.EventType).IsRequired().HasMaxLength(80);
            entity.Property(a => a.Details).IsRequired().HasMaxLength(1000).HasDefaultValue(string.Empty);
            entity.Property(a => a.OccurredAtUtc).HasDefaultValueSql("GETUTCDATE()");

            entity.HasIndex(a => a.AppointmentId);
            entity.HasIndex(a => a.ClinicId);
            entity.HasIndex(a => new { a.AppointmentId, a.OccurredAtUtc });

            entity.HasOne(a => a.Appointment)
                .WithMany()
                .HasForeignKey(a => a.AppointmentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(a => a.Clinic)
                .WithMany()
                .HasForeignKey(a => a.ClinicId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(a => a.ActorUser)
                .WithMany()
                .HasForeignKey(a => a.ActorUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AppointmentReminderDispatch>(entity =>
        {
            entity.HasKey(r => r.AppointmentReminderDispatchId);
            entity.Property(r => r.AppointmentReminderDispatchId).HasDefaultValueSql("NEWID()");
            entity.Property(r => r.ReminderType).IsRequired().HasMaxLength(64);
            entity.Property(r => r.ScheduledForUtc).IsRequired();
            entity.Property(r => r.SentAtUtc).IsRequired().HasDefaultValueSql("GETUTCDATE()");

            entity.HasIndex(r => r.AppointmentId);
            entity.HasIndex(r => r.RecipientUserId);
            entity.HasIndex(r => new { r.AppointmentId, r.RecipientUserId, r.ReminderType }).IsUnique();

            entity.HasOne(r => r.Appointment)
                .WithMany()
                .HasForeignKey(r => r.AppointmentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(r => r.RecipientUser)
                .WithMany()
                .HasForeignKey(r => r.RecipientUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<DoctorAvailabilityWindow>(entity =>
        {
            entity.HasKey(x => x.DoctorAvailabilityWindowId);
            entity.Property(x => x.DoctorAvailabilityWindowId).HasDefaultValueSql("NEWID()");
            entity.Property(x => x.DayOfWeek).IsRequired();
            entity.Property(x => x.StartTime).HasColumnType("time").IsRequired();
            entity.Property(x => x.EndTime).HasColumnType("time").IsRequired();
            entity.Property(x => x.IsActive).HasDefaultValue(true);

            entity.HasIndex(x => x.DoctorId);
            entity.HasIndex(x => new { x.DoctorId, x.DayOfWeek, x.StartTime, x.EndTime }).IsUnique();

            entity.HasOne(x => x.Doctor)
                .WithMany()
                .HasForeignKey(x => x.DoctorId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.ToTable(t =>
            {
                t.HasCheckConstraint("CK_DoctorAvailabilityWindows_DayOfWeek", "[DayOfWeek] >= 0 AND [DayOfWeek] <= 6");
                t.HasCheckConstraint("CK_DoctorAvailabilityWindows_TimeRange", "[StartTime] < [EndTime]");
            });
        });

        modelBuilder.Entity<DoctorAvailabilityBreak>(entity =>
        {
            entity.HasKey(x => x.DoctorAvailabilityBreakId);
            entity.Property(x => x.DoctorAvailabilityBreakId).HasDefaultValueSql("NEWID()");
            entity.Property(x => x.DayOfWeek).IsRequired();
            entity.Property(x => x.StartTime).HasColumnType("time").IsRequired();
            entity.Property(x => x.EndTime).HasColumnType("time").IsRequired();
            entity.Property(x => x.IsActive).HasDefaultValue(true);

            entity.HasIndex(x => x.DoctorId);
            entity.HasIndex(x => new { x.DoctorId, x.DayOfWeek, x.StartTime, x.EndTime }).IsUnique();

            entity.HasOne(x => x.Doctor)
                .WithMany()
                .HasForeignKey(x => x.DoctorId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.ToTable(t =>
            {
                t.HasCheckConstraint("CK_DoctorAvailabilityBreaks_DayOfWeek", "[DayOfWeek] >= 0 AND [DayOfWeek] <= 6");
                t.HasCheckConstraint("CK_DoctorAvailabilityBreaks_TimeRange", "[StartTime] < [EndTime]");
            });
        });

        modelBuilder.Entity<DoctorAvailabilityOverride>(entity =>
        {
            entity.HasKey(x => x.DoctorAvailabilityOverrideId);
            entity.Property(x => x.DoctorAvailabilityOverrideId).HasDefaultValueSql("NEWID()");
            entity.Property(x => x.Date).HasColumnType("date");
            entity.Property(x => x.StartTime).HasColumnType("time");
            entity.Property(x => x.EndTime).HasColumnType("time");
            entity.Property(x => x.IsAvailable).IsRequired();
            entity.Property(x => x.Reason).HasMaxLength(200).HasDefaultValue(string.Empty);
            entity.Property(x => x.IsActive).HasDefaultValue(true);

            entity.HasIndex(x => x.DoctorId);
            entity.HasIndex(x => new { x.DoctorId, x.Date, x.IsAvailable, x.StartTime, x.EndTime }).IsUnique();

            entity.HasOne(x => x.Doctor)
                .WithMany()
                .HasForeignKey(x => x.DoctorId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.ToTable(t =>
            {
                t.HasCheckConstraint("CK_DoctorAvailabilityOverrides_TimeRange", "[StartTime] IS NULL OR [EndTime] IS NULL OR [StartTime] < [EndTime]");
            });
        });
    }
}
