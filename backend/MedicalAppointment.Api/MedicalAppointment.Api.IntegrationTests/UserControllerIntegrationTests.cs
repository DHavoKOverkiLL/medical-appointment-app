using System.Net;
using System.Net.Http.Json;
using MedicalAppointment.Api.Contracts.Auth;
using MedicalAppointment.Api.IntegrationTests.Infrastructure;
using MedicalAppointment.Api.Services;
using MedicalAppointment.Domain.Constants;
using MedicalAppointment.Domain.Models;
using MedicalAppointment.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MedicalAppointment.Api.IntegrationTests;

public sealed class UserControllerIntegrationTests : IClassFixture<ApiWebApplicationFactory>
{
    private const string IntegrationJwtKey = "integration-tests-super-secret-key-1234567890";
    private readonly ApiWebApplicationFactory _factory;

    public UserControllerIntegrationTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Register_NormalizesEmailPersonalIdentifierAndPhone_WhenPayloadIsValid()
    {
        var clinicId = Guid.NewGuid();
        var patientRoleId = Guid.NewGuid();

        await _factory.ResetDatabaseAsync(async db =>
        {
            AddRequiredSystemLookups(db);
            db.SysRoles.Add(new SysRole
            {
                SysRoleId = patientRoleId,
                Name = SystemRoles.Patient,
                Description = "Patient",
                IsActive = true
            });
            db.Clinics.Add(CreateClinic(clinicId, "Register Clinic"));
            await Task.CompletedTask;
        });

        using var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/User/register", new RegisterRequest
        {
            Username = "  register_user  ",
            Email = "  Register.User@Example.COM ",
            Password = "Passw0rd!",
            FirstName = "  Jane ",
            LastName = "  Doe ",
            PersonalIdentifier = "  ab 123  ",
            Address = "  Main Street  ",
            PhoneNumber = " +15551234567 ",
            BirthDate = new DateTime(1994, 5, 20),
            ClinicId = clinicId
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<RegisterResponse>();
        Assert.NotNull(payload);
        Assert.Equal("register.user@example.com", payload!.Email);
        Assert.Equal(SystemRoles.Patient, payload.Role);
        Assert.Equal(clinicId, payload.ClinicId);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = await db.Users
            .Include(u => u.Person)
            .Include(u => u.SysRole)
            .SingleAsync();

        Assert.Equal("register_user", user.Username);
        Assert.Equal("register.user@example.com", user.Email);
        Assert.Equal("AB123", user.Person.PersonalIdentifier);
        Assert.Equal("Main Street", user.Person.Address);
        Assert.Equal("+15551234567", user.Person.PhoneNumber);
        Assert.Equal(SystemRoles.Patient, user.SysRole?.Name);
        Assert.False(user.IsEmailVerified);
        Assert.Null(user.EmailVerifiedAtUtc);

        var verificationCode = await db.UserEmailVerificationCodes.SingleAsync(c => c.UserId == user.UserId);
        Assert.Null(verificationCode.ConsumedAtUtc);
        Assert.True(verificationCode.ExpiresAtUtc > verificationCode.CreatedAtUtc);
        Assert.Equal(EmailVerificationTriggers.Registration, verificationCode.Trigger);
    }

    [Fact]
    public async Task Register_ReturnsValidationProblem_WhenPhoneNumberIsInvalid()
    {
        var clinicId = Guid.NewGuid();
        var patientRoleId = Guid.NewGuid();

        await _factory.ResetDatabaseAsync(async db =>
        {
            AddRequiredSystemLookups(db);
            db.SysRoles.Add(new SysRole
            {
                SysRoleId = patientRoleId,
                Name = SystemRoles.Patient,
                Description = "Patient",
                IsActive = true
            });
            db.Clinics.Add(CreateClinic(clinicId, "Validation Clinic"));
            await Task.CompletedTask;
        });

        using var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/User/register", new RegisterRequest
        {
            Username = "validation_user",
            Email = "validation.user@example.com",
            Password = "Passw0rd!",
            FirstName = "Validation",
            LastName = "User",
            PersonalIdentifier = "VAL123",
            Address = "Address",
            PhoneNumber = "invalid-phone-value",
            BirthDate = new DateTime(1990, 1, 1),
            ClinicId = clinicId
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(0, await db.Users.CountAsync());
        Assert.Equal(0, await db.Persons.CountAsync());
    }

    [Fact]
    public async Task Login_ReturnsForbiddenAndIssuesVerificationCode_WhenUserEmailIsNotVerified()
    {
        var clinicId = Guid.NewGuid();
        var patientRoleId = Guid.NewGuid();
        var personId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        const string password = "Passw0rd!";

        await _factory.ResetDatabaseAsync(async db =>
        {
            AddRequiredSystemLookups(db);
            db.SysRoles.Add(new SysRole
            {
                SysRoleId = patientRoleId,
                Name = SystemRoles.Patient,
                Description = "Patient",
                IsActive = true
            });

            db.Clinics.Add(CreateClinic(clinicId, "Login Clinic"));
            db.Persons.Add(CreatePerson(personId, "Login", "User", "LOGIN001"));

            var user = CreateUser(userId, patientRoleId, clinicId, personId, "login_user");
            user.IsEmailVerified = false;
            user.PasswordHash = new PasswordHasher<User>().HashPassword(user, password);
            db.Users.Add(user);

            await Task.CompletedTask;
        });

        using var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/User/login", new LoginRequest
        {
            Email = $"login_user_{userId.ToString("N")[..8]}@example.com",
            Password = password
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<EmailVerificationRequiredResponse>();
        Assert.NotNull(payload);
        Assert.True(payload!.RequiresEmailVerification);
        Assert.Equal($"login_user_{userId.ToString("N")[..8]}@example.com", payload.Email);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var issuedCode = await db.UserEmailVerificationCodes
            .OrderByDescending(c => c.CreatedAtUtc)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        Assert.NotNull(issuedCode);
        Assert.Equal(EmailVerificationTriggers.LoginUnverified, issuedCode!.Trigger);
    }

    [Fact]
    public async Task VerifyEmail_MarksAccountVerified_WhenCodeIsValid()
    {
        var clinicId = Guid.NewGuid();
        var patientRoleId = Guid.NewGuid();
        var personId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        const string code = "123456";
        var email = $"verify_{userId.ToString("N")[..8]}@example.com";

        await _factory.ResetDatabaseAsync(async db =>
        {
            AddRequiredSystemLookups(db);
            db.SysRoles.Add(new SysRole
            {
                SysRoleId = patientRoleId,
                Name = SystemRoles.Patient,
                Description = "Patient",
                IsActive = true
            });

            db.Clinics.Add(CreateClinic(clinicId, "Verify Clinic"));
            db.Persons.Add(CreatePerson(personId, "Verify", "User", "VERIFY001"));

            var user = CreateUser(userId, patientRoleId, clinicId, personId, "verify_user");
            user.Email = email;
            user.IsEmailVerified = false;
            db.Users.Add(user);

            db.UserEmailVerificationCodes.Add(new UserEmailVerificationCode
            {
                UserEmailVerificationCodeId = Guid.NewGuid(),
                UserId = userId,
                CodeHash = EmailVerificationCodeHasher.ComputeHash(userId, code, IntegrationJwtKey),
                CreatedAtUtc = DateTime.UtcNow,
                ExpiresAtUtc = DateTime.UtcNow.AddMinutes(10),
                Trigger = EmailVerificationTriggers.Registration
            });

            await Task.CompletedTask;
        });

        using var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/User/verify-email", new VerifyEmailRequest
        {
            Email = email,
            Code = code
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<VerifyEmailResponse>();
        Assert.NotNull(payload);
        Assert.True(payload!.EmailVerified);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = await db.Users.SingleAsync(u => u.UserId == userId);
        var verificationCode = await db.UserEmailVerificationCodes.SingleAsync(c => c.UserId == userId);

        Assert.True(user.IsEmailVerified);
        Assert.NotNull(user.EmailVerifiedAtUtc);
        Assert.NotNull(verificationCode.ConsumedAtUtc);
    }

    [Fact]
    public async Task GetDoctors_ReturnsForbidden_WhenClinicFilterIsUsedByNonAdmin()
    {
        await _factory.ResetDatabaseAsync();

        using var client = _factory.CreateAuthenticatedClient(
            userId: Guid.NewGuid(),
            clinicId: Guid.NewGuid(),
            role: SystemRoles.Patient);

        var response = await client.GetAsync($"/api/User/doctors?clinicId={Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UpdateUserRole_ReturnsBadRequest_WhenDemotingLastActiveAdmin()
    {
        var clinicId = Guid.NewGuid();
        var adminRoleId = Guid.NewGuid();
        var doctorRoleId = Guid.NewGuid();
        var adminPersonId = Guid.NewGuid();
        var adminUserId = Guid.NewGuid();

        await _factory.ResetDatabaseAsync(async db =>
        {
            AddRequiredSystemLookups(db);
            db.SysRoles.AddRange(
                new SysRole
                {
                    SysRoleId = adminRoleId,
                    Name = SystemRoles.Admin,
                    Description = "Admin",
                    IsActive = true
                },
                new SysRole
                {
                    SysRoleId = doctorRoleId,
                    Name = SystemRoles.Doctor,
                    Description = "Doctor",
                    IsActive = true
                });

            db.Clinics.Add(CreateClinic(clinicId, "Role Clinic"));
            db.Persons.Add(CreatePerson(adminPersonId, "Admin", "One", "ADMIN001"));
            db.Users.Add(CreateUser(adminUserId, adminRoleId, clinicId, adminPersonId, "admin"));

            await Task.CompletedTask;
        });

        using var client = _factory.CreateAuthenticatedClient(adminUserId, clinicId, SystemRoles.Admin);
        var response = await client.PutAsJsonAsync(
            $"/api/User/{adminUserId}/role",
            new UpdateUserRoleRequest { RoleName = SystemRoles.Doctor });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("At least one active admin must remain in the system.", body);
    }

    [Fact]
    public async Task UpdateUser_ReturnsConflict_WhenChangingClinicWithExistingAppointments()
    {
        var clinicAId = Guid.NewGuid();
        var clinicBId = Guid.NewGuid();
        var adminRoleId = Guid.NewGuid();
        var patientRoleId = Guid.NewGuid();
        var doctorRoleId = Guid.NewGuid();

        var adminPersonId = Guid.NewGuid();
        var patientPersonId = Guid.NewGuid();
        var doctorPersonId = Guid.NewGuid();

        var adminUserId = Guid.NewGuid();
        var patientUserId = Guid.NewGuid();
        var doctorUserId = Guid.NewGuid();

        await _factory.ResetDatabaseAsync(async db =>
        {
            AddRequiredSystemLookups(db);
            db.SysRoles.AddRange(
                new SysRole
                {
                    SysRoleId = adminRoleId,
                    Name = SystemRoles.Admin,
                    Description = "Admin",
                    IsActive = true
                },
                new SysRole
                {
                    SysRoleId = patientRoleId,
                    Name = SystemRoles.Patient,
                    Description = "Patient",
                    IsActive = true
                },
                new SysRole
                {
                    SysRoleId = doctorRoleId,
                    Name = SystemRoles.Doctor,
                    Description = "Doctor",
                    IsActive = true
                });

            db.Clinics.AddRange(
                CreateClinic(clinicAId, "Clinic A"),
                CreateClinic(clinicBId, "Clinic B"));

            db.Persons.AddRange(
                CreatePerson(adminPersonId, "Admin", "One", "ADM001"),
                CreatePerson(patientPersonId, "Patient", "One", "PAT001"),
                CreatePerson(doctorPersonId, "Doctor", "One", "DOC001"));

            db.Users.AddRange(
                CreateUser(adminUserId, adminRoleId, clinicAId, adminPersonId, "admin"),
                CreateUser(patientUserId, patientRoleId, clinicAId, patientPersonId, "patient"),
                CreateUser(doctorUserId, doctorRoleId, clinicAId, doctorPersonId, "doctor"));

            db.Appointments.Add(new Appointment
            {
                AppointmentId = Guid.NewGuid(),
                DoctorId = doctorUserId,
                PatientId = patientUserId,
                ClinicId = clinicAId,
                AppointmentDateTime = DateTime.UtcNow.AddDays(5),
                Status = AppointmentStatuses.Scheduled,
                PostponeRequestStatus = AppointmentPostponeStatuses.None
            });

            await Task.CompletedTask;
        });

        using var client = _factory.CreateAuthenticatedClient(adminUserId, clinicAId, SystemRoles.Admin);
        var response = await client.PutAsJsonAsync(
            $"/api/User/{patientUserId}",
            new AdminUpdateUserRequest
            {
                Username = "patient_updated",
                Email = "patient.updated@example.com",
                FirstName = "Patient",
                LastName = "Updated",
                PersonalIdentifier = "PAT001",
                Address = "Address Updated",
                PhoneNumber = "+15550000001",
                BirthDate = new DateTime(1990, 1, 1),
                ClinicId = clinicBId
            });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Cannot move user to another clinic while they have appointments.", body);
    }

    [Fact]
    public async Task UpdateMyProfile_KeepsPhoneNull_WhenPhoneIsNotProvided()
    {
        var clinicId = Guid.NewGuid();
        var patientRoleId = Guid.NewGuid();
        var personId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        await _factory.ResetDatabaseAsync(async db =>
        {
            AddRequiredSystemLookups(db);
            db.SysRoles.Add(new SysRole
            {
                SysRoleId = patientRoleId,
                Name = SystemRoles.Patient,
                Description = "Patient",
                IsActive = true
            });

            db.Clinics.Add(CreateClinic(clinicId, "Profile Clinic"));
            db.Persons.Add(CreatePerson(personId, "Patient", "Profile", "PRO001", phoneNumber: "+15550000002"));
            db.Users.Add(CreateUser(userId, patientRoleId, clinicId, personId, "patient_profile"));

            await Task.CompletedTask;
        });

        using var client = _factory.CreateAuthenticatedClient(userId, clinicId, SystemRoles.Patient);
        var response = await client.PutAsJsonAsync(
            "/api/User/me/profile",
            new UpdateMyProfileRequest
            {
                FirstName = "  Updated ",
                LastName = " Name  ",
                Address = " Updated Address ",
                PhoneNumber = null,
                BirthDate = new DateTime(1993, 7, 15)
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<UserSummaryResponse>();
        Assert.NotNull(payload);
        Assert.Null(payload!.PhoneNumber);
        Assert.Equal("Updated", payload.FirstName);
        Assert.Equal("Name", payload.LastName);
        Assert.Equal("Updated Address", payload.Address);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var person = await db.Persons.SingleAsync(p => p.PersonId == personId);
        Assert.Null(person.PhoneNumber);
        Assert.Equal("Updated", person.FirstName);
        Assert.Equal("Name", person.LastName);
        Assert.Equal("Updated Address", person.Address);
    }

    private static void AddRequiredSystemLookups(AppDbContext db)
    {
        db.SysClinicTypes.Add(new SysClinicType
        {
            SysClinicTypeId = Clinic.DefaultSysClinicTypeId,
            Name = "Primary Care",
            IsActive = true
        });

        db.SysOwnershipTypes.Add(new SysOwnershipType
        {
            SysOwnershipTypeId = Clinic.DefaultSysOwnershipTypeId,
            Name = "Physician Owned",
            IsActive = true
        });

        db.SysSourceSystems.Add(new SysSourceSystem
        {
            SysSourceSystemId = Clinic.DefaultSysSourceSystemId,
            Name = "EHR",
            IsActive = true
        });
    }

    private static Clinic CreateClinic(Guid clinicId, string name)
    {
        return new Clinic
        {
            ClinicId = clinicId,
            Name = name,
            Code = $"CL-{clinicId.ToString("N")[..6]}",
            LegalName = $"{name} LLC",
            SysClinicTypeId = Clinic.DefaultSysClinicTypeId,
            SysOwnershipTypeId = Clinic.DefaultSysOwnershipTypeId,
            SysSourceSystemId = Clinic.DefaultSysSourceSystemId,
            Timezone = "America/Chicago",
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
    }

    private static Person CreatePerson(Guid personId, string firstName, string lastName, string personalIdentifier, string? phoneNumber = null)
    {
        return new Person
        {
            PersonId = personId,
            FirstName = firstName,
            LastName = lastName,
            NormalizedName = $"{firstName}{lastName}".Replace(" ", string.Empty).ToUpperInvariant(),
            PersonalIdentifier = personalIdentifier,
            Address = "Address",
            PhoneNumber = phoneNumber,
            BirthDate = new DateTime(1990, 1, 1)
        };
    }

    private static User CreateUser(Guid userId, Guid roleId, Guid clinicId, Guid personId, string usernamePrefix)
    {
        var suffix = userId.ToString("N")[..8];
        return new User
        {
            UserId = userId,
            Username = $"{usernamePrefix}_{suffix}",
            Email = $"{usernamePrefix}_{suffix}@example.com",
            PasswordHash = "hash",
            SysRoleId = roleId,
            ClinicId = clinicId,
            PersonId = personId
        };
    }
}
