using MedicalAppointment.Infrastructure;
using MedicalAppointment.Api.Configuration;
using MedicalAppointment.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using System.Text;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Add DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var configuredCorsOrigins = new List<string>();
var corsOriginArray = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
if (corsOriginArray is not null)
{
    configuredCorsOrigins.AddRange(corsOriginArray
        .Select(NormalizeCorsOrigin)
        .OfType<string>());
}

var corsOriginCsv = builder.Configuration["Cors:AllowedOrigins"];
if (!string.IsNullOrWhiteSpace(corsOriginCsv))
{
    configuredCorsOrigins.AddRange(
        corsOriginCsv.Split(new[] { ',', ';' }, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Select(NormalizeCorsOrigin)
            .OfType<string>());
}

var allowedCorsOrigins = configuredCorsOrigins
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .ToArray();

if (allowedCorsOrigins.Length == 0)
{
    allowedCorsOrigins =
    [
        "http://localhost:4200",
        "https://app.mediohealth.ro"
    ];
}

// Add CORS policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        policy.WithOrigins(allowedCorsOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
    ?? throw new InvalidOperationException("Jwt settings are missing.");

if (string.IsNullOrWhiteSpace(jwtSettings.Key) || jwtSettings.Key.Contains("CHANGE_ME", StringComparison.OrdinalIgnoreCase))
{
    throw new InvalidOperationException("Jwt:Key is missing or still set to placeholder. Configure it via user-secrets or environment variables.");
}

if (string.IsNullOrWhiteSpace(jwtSettings.Issuer) || string.IsNullOrWhiteSpace(jwtSettings.Audience))
{
    throw new InvalidOperationException("Jwt:Issuer and Jwt:Audience are required.");
}

builder.Services.AddSingleton(jwtSettings);
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<ISystemInfoService, SystemInfoService>();
builder.Services.Configure<AppointmentReminderSettings>(
    builder.Configuration.GetSection(AppointmentReminderSettings.SectionName));
builder.Services.Configure<EmailVerificationSettings>(
    builder.Configuration.GetSection(EmailVerificationSettings.SectionName));
builder.Services.Configure<ReminderEmailProviderSettings>(
    builder.Configuration.GetSection(ReminderEmailProviderSettings.SectionName));
builder.Services.Configure<ReminderSmsProviderSettings>(
    builder.Configuration.GetSection(ReminderSmsProviderSettings.SectionName));
builder.Services.AddHttpClient();
builder.Services.AddSingleton<IReminderEmailSender, ConfigurableReminderEmailSender>();
builder.Services.AddSingleton<IReminderSmsSender, ConfigurableReminderSmsSender>();
builder.Services.AddSingleton<ITransactionalEmailSender, ConfigurableTransactionalEmailSender>();
builder.Services.AddScoped<IEmailVerificationService, EmailVerificationService>();
builder.Services.AddHostedService<AppointmentReminderBackgroundService>();

var keyBytes = Encoding.UTF8.GetBytes(jwtSettings.Key);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var authCookieName = string.IsNullOrWhiteSpace(jwtSettings.CookieName)
            ? "medio_access_token"
            : jwtSettings.CookieName.Trim();

        options.RequireHttpsMetadata = true;
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                if (string.IsNullOrWhiteSpace(context.Token) &&
                    context.Request.Cookies.TryGetValue(authCookieName, out var cookieToken) &&
                    !string.IsNullOrWhiteSpace(cookieToken))
                {
                    context.Token = cookieToken;
                }

                return Task.CompletedTask;
            }
        };

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            RequireExpirationTime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy("AuthLogin", httpContext =>
    {
        var partitionKey = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            });
    });

    options.AddPolicy("AuthRegister", httpContext =>
    {
        var partitionKey = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 3,
                Window = TimeSpan.FromMinutes(5),
                QueueLimit = 0,
                AutoReplenishment = true
            });
    });

    options.AddPolicy("AuthEmailVerification", httpContext =>
    {
        var partitionKey = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 8,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            });
    });

    options.AddPolicy("AuthVerificationResend", httpContext =>
    {
        var partitionKey = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(5),
                QueueLimit = 0,
                AutoReplenishment = true
            });
    });
});
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "MedicalAppointment API", Version = "v1" });

    // Add JWT Auth support
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Enter: Bearer {your token}",
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecuritySchemeReference("Bearer", document, externalResource: null),
            []
        }
    });
});

var app = builder.Build();

// Fail fast when the relational schema is behind the current model.
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (dbContext.Database.IsRelational())
    {
        var pendingMigrations = dbContext.Database.GetPendingMigrations().ToList();
        if (pendingMigrations.Count > 0)
        {
            throw new InvalidOperationException(
                "Database has pending migrations. Apply them before starting the API. " +
                $"Pending: {string.Join(", ", pendingMigrations)}");
        }
    }
}

// Enable Swagger UI
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Enable CORS before auth
app.UseCors("AllowAngular");

app.UseHttpsRedirection();

app.UseRateLimiter();

// Enable auth middleware
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

static string? NormalizeCorsOrigin(string? origin)
{
    if (string.IsNullOrWhiteSpace(origin))
    {
        return null;
    }

    var trimmed = origin.Trim();
    if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var parsedUri))
    {
        return null;
    }

    if (!string.Equals(parsedUri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) &&
        !string.Equals(parsedUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
    {
        return null;
    }

    return parsedUri.GetLeftPart(UriPartial.Authority);
}

public partial class Program;
