using System.Security.Cryptography;
using System.Text;
using MedicalAppointment.Api.Configuration;
using MedicalAppointment.Domain.Models;
using MedicalAppointment.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace MedicalAppointment.Api.Services;

public class EmailVerificationService : IEmailVerificationService
{
    private readonly AppDbContext _context;
    private readonly ITransactionalEmailSender _emailSender;
    private readonly EmailVerificationSettings _settings;
    private readonly ILogger<EmailVerificationService> _logger;
    private readonly string _hashKey;

    public EmailVerificationService(
        AppDbContext context,
        ITransactionalEmailSender emailSender,
        IOptions<EmailVerificationSettings> settings,
        JwtSettings jwtSettings,
        ILogger<EmailVerificationService> logger)
    {
        _context = context;
        _emailSender = emailSender;
        _settings = settings.Value;
        _logger = logger;
        _hashKey = string.IsNullOrWhiteSpace(_settings.HashKey) ? jwtSettings.Key : _settings.HashKey.Trim();
    }

    public async Task<EmailVerificationIssueResult> IssueCodeAsync(
        User user,
        string trigger,
        CancellationToken cancellationToken = default)
    {
        if (!_settings.Enabled)
        {
            return new EmailVerificationIssueResult(EmailVerificationIssueStatus.FeatureDisabled);
        }

        if (user.IsEmailVerified)
        {
            return new EmailVerificationIssueResult(EmailVerificationIssueStatus.UserAlreadyVerified);
        }

        var nowUtc = DateTime.UtcNow;
        var resendCooldownSeconds = GetResendCooldownSeconds();
        if (resendCooldownSeconds > 0 && user.VerificationEmailLastSentAtUtc.HasValue)
        {
            var nextAllowedAtUtc = user.VerificationEmailLastSentAtUtc.Value.AddSeconds(resendCooldownSeconds);
            if (nextAllowedAtUtc > nowUtc)
            {
                return new EmailVerificationIssueResult(
                    EmailVerificationIssueStatus.CooldownActive,
                    NextAllowedAtUtc: nextAllowedAtUtc);
            }
        }

        var dailyLimit = GetMaxSendsPerDay();
        var startOfDayUtc = nowUtc.Date;
        var sentToday = await _context.UserEmailVerificationCodes
            .AsNoTracking()
            .CountAsync(c => c.UserId == user.UserId && c.CreatedAtUtc >= startOfDayUtc, cancellationToken);

        if (sentToday >= dailyLimit)
        {
            return new EmailVerificationIssueResult(EmailVerificationIssueStatus.DailyLimitReached);
        }

        var activeCodes = await _context.UserEmailVerificationCodes
            .Where(c => c.UserId == user.UserId && c.ConsumedAtUtc == null && c.ExpiresAtUtc > nowUtc)
            .ToListAsync(cancellationToken);

        foreach (var activeCode in activeCodes)
        {
            activeCode.ExpiresAtUtc = nowUtc;
        }

        var codeLength = GetCodeLength();
        var code = GenerateNumericCode(codeLength);
        var codeHash = EmailVerificationCodeHasher.ComputeHash(user.UserId, code, _hashKey);
        var expiresAtUtc = nowUtc.AddMinutes(GetCodeTtlMinutes());

        _context.UserEmailVerificationCodes.Add(new UserEmailVerificationCode
        {
            UserEmailVerificationCodeId = Guid.NewGuid(),
            UserId = user.UserId,
            CodeHash = codeHash,
            CreatedAtUtc = nowUtc,
            ExpiresAtUtc = expiresAtUtc,
            Trigger = NormalizeTrigger(trigger)
        });

        user.VerificationEmailLastSentAtUtc = nowUtc;
        await _context.SaveChangesAsync(cancellationToken);

        var sendResult = await _emailSender.SendAsync(
            BuildVerificationMessage(user, code, expiresAtUtc),
            cancellationToken);

        if (sendResult.Status == TransactionalEmailSendStatus.Sent)
        {
            return new EmailVerificationIssueResult(
                EmailVerificationIssueStatus.Sent,
                ExpiresAtUtc: expiresAtUtc);
        }

        if (sendResult.Status == TransactionalEmailSendStatus.SkippedNotConfigured)
        {
            _logger.LogWarning(
                "Verification code created but email delivery is not configured. UserId={UserId}, Email={Email}",
                user.UserId,
                user.Email);
            return new EmailVerificationIssueResult(
                EmailVerificationIssueStatus.DeliveryNotConfigured,
                ExpiresAtUtc: expiresAtUtc);
        }

        _logger.LogWarning(
            "Verification code created but delivery failed. UserId={UserId}, Email={Email}, Error={Error}",
            user.UserId,
            user.Email,
            sendResult.Error);

        return new EmailVerificationIssueResult(
            EmailVerificationIssueStatus.DeliveryFailed,
            ExpiresAtUtc: expiresAtUtc);
    }

    public async Task<EmailVerificationCheckResult> VerifyCodeAsync(
        string email,
        string code,
        CancellationToken cancellationToken = default)
    {
        if (!_settings.Enabled)
        {
            return new EmailVerificationCheckResult(EmailVerificationCheckStatus.FeatureDisabled);
        }

        var normalizedEmail = NormalizeEmail(email);
        var normalizedCode = EmailVerificationCodeHasher.NormalizeCode(code);
        if (string.IsNullOrWhiteSpace(normalizedEmail) || string.IsNullOrWhiteSpace(normalizedCode))
        {
            return new EmailVerificationCheckResult(EmailVerificationCheckStatus.InvalidOrExpired);
        }

        var nowUtc = DateTime.UtcNow;
        var user = await _context.Users.FirstOrDefaultAsync(
            u => u.Email.ToLower() == normalizedEmail,
            cancellationToken);

        if (user == null)
        {
            return new EmailVerificationCheckResult(EmailVerificationCheckStatus.InvalidOrExpired);
        }

        if (user.IsEmailVerified)
        {
            return new EmailVerificationCheckResult(EmailVerificationCheckStatus.UserAlreadyVerified);
        }

        var maxFailedAttempts = GetMaxFailedAttemptsPerCode();
        var activeCode = await _context.UserEmailVerificationCodes
            .Where(c => c.UserId == user.UserId && c.ConsumedAtUtc == null)
            .OrderByDescending(c => c.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (activeCode == null || activeCode.ExpiresAtUtc <= nowUtc || activeCode.AttemptCount >= maxFailedAttempts)
        {
            return new EmailVerificationCheckResult(EmailVerificationCheckStatus.InvalidOrExpired);
        }

        var expectedHash = EmailVerificationCodeHasher.ComputeHash(user.UserId, normalizedCode, _hashKey);
        if (!HashesEqual(expectedHash, activeCode.CodeHash))
        {
            activeCode.AttemptCount += 1;
            activeCode.LastAttemptAtUtc = nowUtc;
            if (activeCode.AttemptCount >= maxFailedAttempts)
            {
                activeCode.ExpiresAtUtc = nowUtc;
            }

            await _context.SaveChangesAsync(cancellationToken);
            return new EmailVerificationCheckResult(EmailVerificationCheckStatus.InvalidOrExpired);
        }

        activeCode.ConsumedAtUtc = nowUtc;
        user.IsEmailVerified = true;
        user.EmailVerifiedAtUtc = nowUtc;

        await _context.SaveChangesAsync(cancellationToken);
        return new EmailVerificationCheckResult(EmailVerificationCheckStatus.Success);
    }

    private TransactionalEmailMessage BuildVerificationMessage(User user, string code, DateTime expiresAtUtc)
    {
        var username = string.IsNullOrWhiteSpace(user.Username) ? "there" : user.Username.Trim();
        var ttlMinutes = GetCodeTtlMinutes();
        var templateParams = BuildTemplateParams(user, code, ttlMinutes);
        var templateId = GetBrevoTemplateId();

        var subject = "Verify your Medio account";
        var body =
            $"Hi {username},{Environment.NewLine}{Environment.NewLine}" +
            $"Your Medio verification code is: {code}{Environment.NewLine}{Environment.NewLine}" +
            $"This code expires in {ttlMinutes} minute(s) at {expiresAtUtc:yyyy-MM-dd HH:mm:ss} UTC.{Environment.NewLine}{Environment.NewLine}" +
            $"If you did not create this account, you can ignore this message.";

        return new TransactionalEmailMessage(
            user.Email,
            user.Username,
            subject,
            body,
            templateId,
            templateParams);
    }

    private static string NormalizeTrigger(string trigger)
    {
        var normalized = string.IsNullOrWhiteSpace(trigger) ? EmailVerificationTriggers.ManualResend : trigger.Trim();
        return normalized.Length <= 64 ? normalized : normalized[..64];
    }

    private static string NormalizeEmail(string email)
    {
        return email.Trim().ToLowerInvariant();
    }

    private static bool HashesEqual(string left, string right)
    {
        var leftBytes = Encoding.UTF8.GetBytes(left);
        var rightBytes = Encoding.UTF8.GetBytes(right);
        return CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
    }

    private static string GenerateNumericCode(int length)
    {
        var maxExclusive = 1;
        for (var i = 0; i < length; i++)
        {
            maxExclusive *= 10;
        }

        var number = RandomNumberGenerator.GetInt32(0, maxExclusive);
        return number.ToString($"D{length}");
    }

    private int GetCodeLength()
    {
        return Math.Clamp(_settings.CodeLength, 4, 8);
    }

    private int GetCodeTtlMinutes()
    {
        return Math.Clamp(_settings.CodeTtlMinutes, 1, 60);
    }

    private int GetResendCooldownSeconds()
    {
        return Math.Clamp(_settings.ResendCooldownSeconds, 0, 3600);
    }

    private int GetMaxFailedAttemptsPerCode()
    {
        return Math.Clamp(_settings.MaxFailedAttemptsPerCode, 1, 20);
    }

    private int GetMaxSendsPerDay()
    {
        return Math.Clamp(_settings.MaxSendsPerDay, 1, 1000);
    }

    private int? GetBrevoTemplateId()
    {
        return _settings.BrevoTemplateId > 0 ? _settings.BrevoTemplateId : null;
    }

    private IReadOnlyDictionary<string, object?> BuildTemplateParams(User user, string code, int ttlMinutes)
    {
        var firstName = user.Person?.FirstName;
        if (string.IsNullOrWhiteSpace(firstName))
        {
            firstName = user.Username;
        }

        if (string.IsNullOrWhiteSpace(firstName))
        {
            firstName = "there";
        }

        var parameters = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["code"] = code,
            ["ttlMinutes"] = ttlMinutes,
            ["firstName"] = firstName.Trim()
        };

        if (!string.IsNullOrWhiteSpace(_settings.TemplateAppUrl))
        {
            parameters["appUrl"] = _settings.TemplateAppUrl.Trim();
        }

        if (!string.IsNullOrWhiteSpace(_settings.TemplateSupportEmail))
        {
            parameters["supportEmail"] = _settings.TemplateSupportEmail.Trim();
        }

        parameters["year"] = DateTime.UtcNow.Year;
        return parameters;
    }
}
