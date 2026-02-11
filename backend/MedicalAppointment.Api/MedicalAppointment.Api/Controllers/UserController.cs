using MedicalAppointment.Api.Contracts.Auth;
using MedicalAppointment.Api.Configuration;
using MedicalAppointment.Api.Extensions;
using MedicalAppointment.Api.Services;
using MedicalAppointment.Domain.Constants;
using MedicalAppointment.Domain.Models;
using MedicalAppointment.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace MedicalAppointment.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private const int MaxFailedLoginAttempts = 5;
    private static readonly TimeSpan LoginLockoutDuration = TimeSpan.FromMinutes(15);

    private readonly AppDbContext _context;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IEmailVerificationService _emailVerificationService;
    private readonly ISystemInfoService _systemInfoService;
    private readonly JwtSettings _jwtSettings;

    public UserController(
        AppDbContext context,
        IJwtTokenService jwtTokenService,
        IEmailVerificationService emailVerificationService,
        ISystemInfoService systemInfoService,
        JwtSettings jwtSettings)
    {
        _context = context;
        _jwtTokenService = jwtTokenService;
        _emailVerificationService = emailVerificationService;
        _systemInfoService = systemInfoService;
        _jwtSettings = jwtSettings;
    }

    [HttpGet]
    [Authorize(Roles = SystemRoles.Admin)]
    public async Task<IActionResult> GetAllUsers([FromQuery] Guid? clinicId = null)
    {
        var usersQuery = _context.Users
            .AsNoTracking()
            .Include(u => u.Person)
            .Include(u => u.SysRole)
            .Include(u => u.Clinic)
            .AsQueryable();

        if (clinicId.HasValue)
        {
            usersQuery = usersQuery.Where(u => u.ClinicId == clinicId.Value);
        }

        var users = await usersQuery
            .OrderBy(u => u.Person.LastName)
            .ThenBy(u => u.Person.FirstName)
            .Select(u => new UserSummaryResponse
            {
                UserId = u.UserId,
                Username = u.Username,
                Email = u.Email,
                Role = u.SysRole != null ? u.SysRole.Name : string.Empty,
                FirstName = u.Person.FirstName,
                LastName = u.Person.LastName,
                PersonalIdentifier = u.Person.PersonalIdentifier,
                Address = u.Person.Address,
                PhoneNumber = u.Person.PhoneNumber,
                BirthDate = u.Person.BirthDate,
                ClinicId = u.ClinicId,
                ClinicName = u.Clinic.Name
            })
            .ToListAsync();

        return Ok(users);
    }

    [HttpPost("register")]
    [AllowAnonymous]
    [EnableRateLimiting("AuthRegister")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var normalizedEmail = NormalizeEmail(request.Email);
        var normalizedPersonalIdentifier = NormalizePersonalIdentifier(request.PersonalIdentifier);
        var normalizedUsername = NormalizeUsername(request.Username);

        if (request.ClinicId == Guid.Empty)
            return BadRequest("Clinic is required.");

        var duplicateIdentityExists =
            await _context.Users.AnyAsync(u =>
                u.Email.ToLower() == normalizedEmail ||
                u.Username.ToLower() == normalizedUsername.ToLower()) ||
            await _context.Persons.AnyAsync(p => p.PersonalIdentifier == normalizedPersonalIdentifier);

        if (duplicateIdentityExists)
        {
            return Conflict("Registration could not be completed with the provided details.");
        }

        var clinic = await _context.Clinics
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.ClinicId == request.ClinicId && c.IsActive);

        if (clinic == null)
            return BadRequest("Selected clinic is invalid or inactive.");

        var patientRole = await _context.SysRoles
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Name == SystemRoles.Patient && r.IsActive);

        if (patientRole == null)
            return Problem("Patient role is missing or inactive. Contact support.", statusCode: StatusCodes.Status500InternalServerError);

        var person = new Person
        {
            PersonId = Guid.NewGuid(),
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            NormalizedName = $"{request.FirstName}{request.LastName}".Replace(" ", string.Empty).Trim().ToUpperInvariant(),
            PersonalIdentifier = normalizedPersonalIdentifier,
            Address = (request.Address ?? string.Empty).Trim(),
            PhoneNumber = NormalizePhoneNumber(request.PhoneNumber),
            BirthDate = request.BirthDate.Date
        };

        var user = new User
        {
            UserId = Guid.NewGuid(),
            Username = normalizedUsername,
            Email = normalizedEmail,
            PersonId = person.PersonId,
            Person = person,
            SysRoleId = patientRole.SysRoleId,
            ClinicId = clinic.ClinicId
        };

        var hasher = new PasswordHasher<User>();
        user.PasswordHash = hasher.HashPassword(user, request.Password);

        _context.Persons.Add(person);
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var verificationIssueResult = await _emailVerificationService.IssueCodeAsync(
            user,
            EmailVerificationTriggers.Registration,
            HttpContext.RequestAborted);

        var response = new RegisterResponse
        {
            UserId = user.UserId,
            Email = user.Email,
            Role = SystemRoles.Patient,
            ClinicId = clinic.ClinicId,
            ClinicName = clinic.Name,
            RequiresEmailVerification = true,
            VerificationEmailSent = verificationIssueResult.Status == EmailVerificationIssueStatus.Sent,
            VerificationCodeExpiresAtUtc = verificationIssueResult.ExpiresAtUtc
        };

        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpPost("admin")]
    [Authorize(Roles = SystemRoles.Admin)]
    public async Task<IActionResult> CreateUser([FromBody] AdminCreateUserRequest request)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        if (request.ClinicId == Guid.Empty)
        {
            return BadRequest("Clinic is required.");
        }

        var normalizedEmail = NormalizeEmail(request.Email);
        var normalizedPersonalIdentifier = NormalizePersonalIdentifier(request.PersonalIdentifier);
        var normalizedUsername = NormalizeUsername(request.Username);

        if (await _context.Users.AnyAsync(u => u.Email.ToLower() == normalizedEmail))
            return Conflict("A user with this email already exists.");

        if (await _context.Users.AnyAsync(u => u.Username.ToLower() == normalizedUsername.ToLower()))
            return Conflict("A user with this username already exists.");

        if (await _context.Persons.AnyAsync(p => p.PersonalIdentifier == normalizedPersonalIdentifier))
            return Conflict("A person with this Personal Identifier already exists.");

        var role = await _context.SysRoles
            .FirstOrDefaultAsync(r => r.Name.ToLower() == request.RoleName.Trim().ToLower() && r.IsActive);

        if (role == null)
            return BadRequest("Selected role is invalid or inactive.");

        var clinic = await _context.Clinics.FirstOrDefaultAsync(c => c.ClinicId == request.ClinicId && c.IsActive);
        if (clinic == null)
            return BadRequest("Selected clinic is invalid or inactive.");

        var person = new Person
        {
            PersonId = Guid.NewGuid(),
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            NormalizedName = $"{request.FirstName}{request.LastName}".Replace(" ", string.Empty).Trim().ToUpperInvariant(),
            PersonalIdentifier = normalizedPersonalIdentifier,
            Address = (request.Address ?? string.Empty).Trim(),
            PhoneNumber = NormalizePhoneNumber(request.PhoneNumber),
            BirthDate = request.BirthDate.Date
        };

        var user = new User
        {
            UserId = Guid.NewGuid(),
            Username = normalizedUsername,
            Email = normalizedEmail,
            PersonId = person.PersonId,
            Person = person,
            SysRoleId = role.SysRoleId,
            ClinicId = clinic.ClinicId
        };

        var hasher = new PasswordHasher<User>();
        user.PasswordHash = hasher.HashPassword(user, request.Password);

        _context.Persons.Add(person);
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created, new UserSummaryResponse
        {
            UserId = user.UserId,
            Username = user.Username,
            Email = user.Email,
            Role = role.Name,
            FirstName = person.FirstName,
            LastName = person.LastName,
            PersonalIdentifier = person.PersonalIdentifier,
            Address = person.Address,
            PhoneNumber = person.PhoneNumber,
            BirthDate = person.BirthDate,
            ClinicId = clinic.ClinicId,
            ClinicName = clinic.Name
        });
    }

    [HttpPut("{userId:guid}")]
    [Authorize(Roles = SystemRoles.Admin)]
    public async Task<IActionResult> UpdateUser(Guid userId, [FromBody] AdminUpdateUserRequest request)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        if (request.ClinicId == Guid.Empty)
        {
            return BadRequest("Clinic is required.");
        }

        var user = await _context.Users
            .Include(u => u.Person)
            .Include(u => u.SysRole)
            .Include(u => u.Clinic)
            .FirstOrDefaultAsync(u => u.UserId == userId);

        if (user == null)
        {
            return NotFound("User not found.");
        }

        var normalizedEmail = NormalizeEmail(request.Email);
        var normalizedUsername = NormalizeUsername(request.Username);
        var normalizedPersonalIdentifier = NormalizePersonalIdentifier(request.PersonalIdentifier);

        if (await _context.Users.AnyAsync(u => u.UserId != userId && u.Email.ToLower() == normalizedEmail))
            return Conflict("A user with this email already exists.");

        if (await _context.Users.AnyAsync(u => u.UserId != userId && u.Username.ToLower() == normalizedUsername.ToLower()))
            return Conflict("A user with this username already exists.");

        if (await _context.Persons.AnyAsync(p => p.PersonId != user.PersonId && p.PersonalIdentifier == normalizedPersonalIdentifier))
            return Conflict("A person with this Personal Identifier already exists.");

        var clinic = await _context.Clinics.FirstOrDefaultAsync(c => c.ClinicId == request.ClinicId && c.IsActive);
        if (clinic == null)
            return BadRequest("Selected clinic is invalid or inactive.");

        if (user.ClinicId != request.ClinicId)
        {
            var hasAppointments = await _context.Appointments.AnyAsync(a => a.PatientId == userId || a.DoctorId == userId);
            if (hasAppointments)
            {
                return Conflict("Cannot move user to another clinic while they have appointments.");
            }
        }

        user.Username = normalizedUsername;
        user.Email = normalizedEmail;
        user.ClinicId = clinic.ClinicId;

        user.Person.FirstName = request.FirstName.Trim();
        user.Person.LastName = request.LastName.Trim();
        user.Person.NormalizedName = $"{request.FirstName}{request.LastName}".Replace(" ", string.Empty).Trim().ToUpperInvariant();
        user.Person.PersonalIdentifier = normalizedPersonalIdentifier;
        user.Person.Address = (request.Address ?? string.Empty).Trim();
        user.Person.PhoneNumber = NormalizePhoneNumber(request.PhoneNumber);
        user.Person.BirthDate = request.BirthDate.Date;

        await _context.SaveChangesAsync();

        return Ok(new UserSummaryResponse
        {
            UserId = user.UserId,
            Username = user.Username,
            Email = user.Email,
            Role = user.SysRole?.Name ?? string.Empty,
            FirstName = user.Person.FirstName,
            LastName = user.Person.LastName,
            PersonalIdentifier = user.Person.PersonalIdentifier,
            Address = user.Person.Address,
            PhoneNumber = user.Person.PhoneNumber,
            BirthDate = user.Person.BirthDate,
            ClinicId = user.ClinicId,
            ClinicName = clinic.Name
        });
    }

    [HttpPut("{userId:guid}/role")]
    [Authorize(Roles = SystemRoles.Admin)]
    public async Task<IActionResult> UpdateUserRole(Guid userId, [FromBody] UpdateUserRoleRequest request)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var targetUser = await _context.Users
            .Include(u => u.SysRole)
            .FirstOrDefaultAsync(u => u.UserId == userId);

        if (targetUser == null)
        {
            return NotFound("User not found.");
        }

        var requestedRoleName = request.RoleName.Trim();
        var role = await _context.SysRoles.FirstOrDefaultAsync(r => r.Name.ToLower() == requestedRoleName.ToLower() && r.IsActive);
        if (role == null)
        {
            return BadRequest("Selected role is invalid or inactive.");
        }

        var currentRoleName = targetUser.SysRole?.Name ?? string.Empty;
        if (currentRoleName == SystemRoles.Admin && role.Name != SystemRoles.Admin)
        {
            var adminCount = await _context.Users.CountAsync(u => u.SysRole != null && u.SysRole.Name == SystemRoles.Admin && u.SysRole.IsActive);
            if (adminCount <= 1)
            {
                return BadRequest("At least one active admin must remain in the system.");
            }
        }

        targetUser.SysRoleId = role.SysRoleId;
        await _context.SaveChangesAsync();

        return Ok(new
        {
            targetUser.UserId,
            Role = role.Name
        });
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("AuthLogin")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var nowUtc = DateTime.UtcNow;
        var normalizedEmail = NormalizeEmail(request.Email);

        var user = await _context.Users
            .Include(u => u.SysRole)
            .Include(u => u.Clinic)
            .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);

        if (user == null || user.SysRole == null || !user.SysRole.IsActive || user.Clinic == null || !user.Clinic.IsActive)
        {
            return Unauthorized("Invalid credentials");
        }

        if (user.LockoutEndUtc.HasValue && user.LockoutEndUtc.Value > nowUtc)
        {
            return Unauthorized("Invalid credentials");
        }

        var hasher = new PasswordHasher<User>();
        var result = hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);

        if (result == PasswordVerificationResult.Failed)
        {
            user.FailedLoginAttempts += 1;
            user.LastFailedLoginAtUtc = nowUtc;

            if (user.FailedLoginAttempts >= MaxFailedLoginAttempts)
            {
                user.LockoutEndUtc = nowUtc.Add(LoginLockoutDuration);
                user.FailedLoginAttempts = 0;
            }

            await _context.SaveChangesAsync();
            return Unauthorized("Invalid credentials");
        }

        var shouldSaveUser = false;
        if (result == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = hasher.HashPassword(user, request.Password);
            shouldSaveUser = true;
        }

        if (user.FailedLoginAttempts > 0 || user.LockoutEndUtc.HasValue || user.LastFailedLoginAtUtc.HasValue)
        {
            user.FailedLoginAttempts = 0;
            user.LockoutEndUtc = null;
            user.LastFailedLoginAtUtc = null;
            shouldSaveUser = true;
        }

        if (shouldSaveUser)
        {
            await _context.SaveChangesAsync();
        }

        if (!user.IsEmailVerified)
        {
            var verificationIssueResult = await _emailVerificationService.IssueCodeAsync(
                user,
                EmailVerificationTriggers.LoginUnverified,
                HttpContext.RequestAborted);

            var verificationMessage = BuildVerificationRequiredMessage(verificationIssueResult);
            return StatusCode(StatusCodes.Status403Forbidden, new EmailVerificationRequiredResponse
            {
                Email = user.Email,
                VerificationEmailSent = verificationIssueResult.Status == EmailVerificationIssueStatus.Sent,
                NextAllowedAtUtc = verificationIssueResult.NextAllowedAtUtc,
                Message = verificationMessage
            });
        }

        var normalizedRoleName = NormalizeRoleName(user.SysRole.Name);
        var (token, expiresAtUtc) = _jwtTokenService.CreateToken(user, normalizedRoleName);
        AppendAuthCookie(token, expiresAtUtc);

        return Ok(new LoginResponse
        {
            Token = token,
            ExpiresAtUtc = expiresAtUtc,
            UserId = user.UserId,
            Email = user.Email,
            Role = normalizedRoleName,
            ClinicId = user.ClinicId,
            ClinicName = user.Clinic.Name
        });
    }

    [HttpPost("verify-email")]
    [AllowAnonymous]
    [EnableRateLimiting("AuthEmailVerification")]
    public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailRequest request)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var verificationResult = await _emailVerificationService.VerifyCodeAsync(
            request.Email,
            request.Code,
            HttpContext.RequestAborted);

        if (verificationResult.Status == EmailVerificationCheckStatus.Success)
        {
            return Ok(new VerifyEmailResponse
            {
                EmailVerified = true,
                Message = "Email verified successfully. You can now log in."
            });
        }

        if (verificationResult.Status == EmailVerificationCheckStatus.UserAlreadyVerified)
        {
            return Ok(new VerifyEmailResponse
            {
                EmailVerified = true,
                Message = "Email is already verified."
            });
        }

        if (verificationResult.Status == EmailVerificationCheckStatus.FeatureDisabled)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new VerifyEmailResponse
            {
                EmailVerified = false,
                Message = "Email verification is currently unavailable."
            });
        }

        return BadRequest(new VerifyEmailResponse
        {
            EmailVerified = false,
            Message = "Invalid or expired verification code."
        });
    }

    [HttpPost("resend-verification-code")]
    [AllowAnonymous]
    [EnableRateLimiting("AuthVerificationResend")]
    public async Task<IActionResult> ResendVerificationCode([FromBody] ResendEmailVerificationRequest request)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var normalizedEmail = NormalizeEmail(request.Email);
        var user = await _context.Users.FirstOrDefaultAsync(
            u => u.Email.ToLower() == normalizedEmail,
            HttpContext.RequestAborted);

        if (user == null || user.IsEmailVerified)
        {
            return Ok(new ResendEmailVerificationResponse
            {
                VerificationEmailSent = false,
                Message = "If your account exists and requires verification, a code has been sent."
            });
        }

        var issueResult = await _emailVerificationService.IssueCodeAsync(
            user,
            EmailVerificationTriggers.ManualResend,
            HttpContext.RequestAborted);

        if (issueResult.Status == EmailVerificationIssueStatus.CooldownActive ||
            issueResult.Status == EmailVerificationIssueStatus.DailyLimitReached)
        {
            return StatusCode(StatusCodes.Status429TooManyRequests, new ResendEmailVerificationResponse
            {
                VerificationEmailSent = false,
                NextAllowedAtUtc = issueResult.NextAllowedAtUtc,
                Message = BuildResendMessage(issueResult)
            });
        }

        if (issueResult.Status == EmailVerificationIssueStatus.FeatureDisabled)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new ResendEmailVerificationResponse
            {
                VerificationEmailSent = false,
                Message = BuildResendMessage(issueResult)
            });
        }

        return Ok(new ResendEmailVerificationResponse
        {
            VerificationEmailSent = issueResult.Status == EmailVerificationIssueStatus.Sent,
            NextAllowedAtUtc = issueResult.NextAllowedAtUtc,
            Message = BuildResendMessage(issueResult)
        });
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    public IActionResult Logout()
    {
        ClearAuthCookie();
        return Ok(new { Message = "Logged out." });
    }

    [HttpGet("secure")]
    [Authorize]
    public IActionResult GetSecureInfo()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var email = User.FindFirstValue(ClaimTypes.Email);
        var clinicName = User.FindFirstValue("clinic_name");
        return Ok($"This is protected. Hello user {email} with ID {userId} from clinic {clinicName}!");
    }

    [HttpGet("/api/SysRoles")]
    [Authorize(Roles = SystemRoles.Admin)]
    public async Task<IActionResult> GetAllRoles()
    {
        var roles = await _systemInfoService.GetActiveRolesAsync(HttpContext.RequestAborted);
        return Ok(roles);
    }

    [HttpGet("doctors")]
    [Authorize]
    public async Task<IActionResult> GetDoctors([FromQuery] Guid? clinicId = null)
    {
        Guid resolvedClinicId;
        var role = User.FindFirstValue(ClaimTypes.Role);

        if (clinicId.HasValue)
        {
            if (role != SystemRoles.Admin)
            {
                return Forbid();
            }

            resolvedClinicId = clinicId.Value;
        }
        else if (!User.TryGetClinicId(out resolvedClinicId))
        {
            return Unauthorized("Invalid clinic claim in token.");
        }

        var doctors = await _context.Users
            .AsNoTracking()
            .Include(u => u.Person)
            .Include(u => u.SysRole)
            .Include(u => u.Clinic)
            .Where(u =>
                u.ClinicId == resolvedClinicId &&
                u.Clinic.IsActive &&
                u.SysRole != null &&
                u.SysRole.Name == SystemRoles.Doctor &&
                u.SysRole.IsActive)
            .OrderBy(u => u.Person.LastName)
            .ThenBy(u => u.Person.FirstName)
            .Select(u => new DoctorSummaryResponse
            {
                Id = u.UserId,
                Name = (u.Person.FirstName + " " + u.Person.LastName).Trim(),
                ClinicId = u.ClinicId,
                ClinicName = u.Clinic.Name
            })
            .ToListAsync();

        return Ok(doctors);
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetMe()
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized("Invalid token.");
        }

        var user = await _context.Users
            .AsNoTracking()
            .Include(u => u.Person)
            .Include(u => u.SysRole)
            .Include(u => u.Clinic)
            .FirstOrDefaultAsync(u => u.UserId == userId);

        if (user == null || user.SysRole == null || user.Clinic == null)
        {
            return NotFound("User not found.");
        }

        return Ok(new UserSummaryResponse
        {
            UserId = user.UserId,
            Username = user.Username,
            Email = user.Email,
            Role = user.SysRole.Name,
            FirstName = user.Person.FirstName,
            LastName = user.Person.LastName,
            PersonalIdentifier = user.Person.PersonalIdentifier,
            Address = user.Person.Address,
            PhoneNumber = user.Person.PhoneNumber,
            BirthDate = user.Person.BirthDate,
            ClinicId = user.ClinicId,
            ClinicName = user.Clinic.Name
        });
    }

    [HttpPut("me/profile")]
    [Authorize(Roles = SystemRoles.Patient + "," + SystemRoles.Doctor + "," + SystemRoles.Admin)]
    public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateMyProfileRequest request)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized("Invalid token.");
        }

        var user = await _context.Users
            .Include(u => u.Person)
            .Include(u => u.SysRole)
            .Include(u => u.Clinic)
            .FirstOrDefaultAsync(u => u.UserId == userId);

        if (user == null || user.SysRole == null || user.Clinic == null)
        {
            return NotFound("User not found.");
        }

        user.Person.FirstName = request.FirstName.Trim();
        user.Person.LastName = request.LastName.Trim();
        user.Person.NormalizedName = $"{request.FirstName}{request.LastName}".Replace(" ", string.Empty).Trim().ToUpperInvariant();
        user.Person.Address = (request.Address ?? string.Empty).Trim();
        user.Person.PhoneNumber = NormalizePhoneNumber(request.PhoneNumber);
        user.Person.BirthDate = request.BirthDate.Date;

        await _context.SaveChangesAsync();

        return Ok(new UserSummaryResponse
        {
            UserId = user.UserId,
            Username = user.Username,
            Email = user.Email,
            Role = user.SysRole.Name,
            FirstName = user.Person.FirstName,
            LastName = user.Person.LastName,
            PersonalIdentifier = user.Person.PersonalIdentifier,
            Address = user.Person.Address,
            PhoneNumber = user.Person.PhoneNumber,
            BirthDate = user.Person.BirthDate,
            ClinicId = user.ClinicId,
            ClinicName = user.Clinic.Name
        });
    }

    [HttpPut("me/account")]
    [Authorize(Roles = SystemRoles.Patient + "," + SystemRoles.Doctor + "," + SystemRoles.Admin)]
    public async Task<IActionResult> UpdateMyAccount([FromBody] UpdateMyAccountSettingsRequest request)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized("Invalid token.");
        }

        var user = await _context.Users
            .Include(u => u.Person)
            .Include(u => u.SysRole)
            .Include(u => u.Clinic)
            .FirstOrDefaultAsync(u => u.UserId == userId);

        if (user == null || user.SysRole == null || user.Clinic == null)
        {
            return NotFound("User not found.");
        }

        var hasher = new PasswordHasher<User>();
        var currentPasswordResult = hasher.VerifyHashedPassword(user, user.PasswordHash, request.CurrentPassword);

        if (currentPasswordResult == PasswordVerificationResult.Failed)
        {
            return Unauthorized("Current password is incorrect.");
        }

        var normalizedEmail = NormalizeEmail(request.Email);
        var normalizedUsername = NormalizeUsername(request.Username);

        if (await _context.Users.AnyAsync(u => u.UserId != userId && u.Email.ToLower() == normalizedEmail))
        {
            return Conflict("A user with this email already exists.");
        }

        if (await _context.Users.AnyAsync(u => u.UserId != userId && u.Username.ToLower() == normalizedUsername.ToLower()))
        {
            return Conflict("A user with this username already exists.");
        }

        user.Email = normalizedEmail;
        user.Username = normalizedUsername;

        if (!string.IsNullOrWhiteSpace(request.NewPassword))
        {
            user.PasswordHash = hasher.HashPassword(user, request.NewPassword);
        }
        else if (currentPasswordResult == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = hasher.HashPassword(user, request.CurrentPassword);
        }

        await _context.SaveChangesAsync();

        return Ok(new
        {
            Message = "Account settings updated.",
            user.Username,
            user.Email
        });
    }

    private static string BuildVerificationRequiredMessage(EmailVerificationIssueResult issueResult)
    {
        if (issueResult.Status == EmailVerificationIssueStatus.Sent)
        {
            return "Your account is not verified yet. A new verification code has been sent to your email.";
        }

        if (issueResult.Status == EmailVerificationIssueStatus.CooldownActive)
        {
            return "Your account is not verified yet. A code was sent recently. Try again after the cooldown period.";
        }

        if (issueResult.Status == EmailVerificationIssueStatus.DailyLimitReached)
        {
            return "Your account is not verified yet. Daily verification email limit reached. Try again later.";
        }

        if (issueResult.Status == EmailVerificationIssueStatus.DeliveryNotConfigured)
        {
            return "Your account is not verified yet. Verification email delivery is not configured. Contact support.";
        }

        if (issueResult.Status == EmailVerificationIssueStatus.DeliveryFailed)
        {
            return "Your account is not verified yet. We could not deliver the verification email right now. Try again shortly.";
        }

        if (issueResult.Status == EmailVerificationIssueStatus.FeatureDisabled)
        {
            return "Your account is not verified yet. Email verification is currently unavailable.";
        }

        return "Your account is not verified yet. Please verify your email to continue.";
    }

    private static string BuildResendMessage(EmailVerificationIssueResult issueResult)
    {
        if (issueResult.Status == EmailVerificationIssueStatus.Sent)
        {
            return "Verification code sent.";
        }

        if (issueResult.Status == EmailVerificationIssueStatus.CooldownActive)
        {
            return "Verification code was sent recently. Please wait before requesting another code.";
        }

        if (issueResult.Status == EmailVerificationIssueStatus.DailyLimitReached)
        {
            return "Daily verification email limit reached. Try again later.";
        }

        if (issueResult.Status == EmailVerificationIssueStatus.DeliveryNotConfigured)
        {
            return "Email delivery is not configured.";
        }

        if (issueResult.Status == EmailVerificationIssueStatus.DeliveryFailed)
        {
            return "Failed to send verification code. Try again later.";
        }

        if (issueResult.Status == EmailVerificationIssueStatus.FeatureDisabled)
        {
            return "Email verification is currently unavailable.";
        }

        if (issueResult.Status == EmailVerificationIssueStatus.UserAlreadyVerified)
        {
            return "Email is already verified.";
        }

        return "Unable to process resend request.";
    }

    private void AppendAuthCookie(string token, DateTime expiresAtUtc)
    {
        var cookieName = string.IsNullOrWhiteSpace(_jwtSettings.CookieName)
            ? "medio_access_token"
            : _jwtSettings.CookieName.Trim();

        Response.Cookies.Append(cookieName, token, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Expires = new DateTimeOffset(DateTime.SpecifyKind(expiresAtUtc, DateTimeKind.Utc)),
            IsEssential = true,
            Path = "/"
        });
    }

    private void ClearAuthCookie()
    {
        var cookieName = string.IsNullOrWhiteSpace(_jwtSettings.CookieName)
            ? "medio_access_token"
            : _jwtSettings.CookieName.Trim();

        Response.Cookies.Delete(cookieName, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Path = "/"
        });
    }

    private static string NormalizeEmail(string email)
    {
        return email.Trim().ToLowerInvariant();
    }

    private static string NormalizePersonalIdentifier(string identifier)
    {
        return identifier.Replace(" ", string.Empty).Trim().ToUpperInvariant();
    }

    private static string NormalizeUsername(string username)
    {
        return username.Trim();
    }

    private static string? NormalizePhoneNumber(string? phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            return null;
        }

        return phoneNumber.Trim();
    }

    private static string NormalizeRoleName(string roleName)
    {
        if (string.Equals(roleName, SystemRoles.Admin, StringComparison.OrdinalIgnoreCase))
        {
            return SystemRoles.Admin;
        }

        if (string.Equals(roleName, SystemRoles.Doctor, StringComparison.OrdinalIgnoreCase))
        {
            return SystemRoles.Doctor;
        }

        if (string.Equals(roleName, SystemRoles.Patient, StringComparison.OrdinalIgnoreCase))
        {
            return SystemRoles.Patient;
        }

        return roleName.Trim();
    }
}
