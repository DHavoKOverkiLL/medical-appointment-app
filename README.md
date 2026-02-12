# Medical Appointment App

Full-stack application for medical appointment scheduling, clinic management, user administration, and reminder delivery.

## Overview

The repository contains:

- `backend/MedicalAppointment.Api`: ASP.NET Core Web API (`net10.0`) with EF Core + SQL Server
- `frontend/medical-appointment-ui`: Angular SPA (`v19`) with Angular Material
- `backend/MedicalAppointment.Api/MedicalAppointment.Api.IntegrationTests`: integration test suite (xUnit)
- `.github/workflows/ci.yml`: CI pipeline for backend + frontend build/test/coverage

Core backend areas currently implemented include:

- JWT authentication and role-based authorization
- Clinic tenancy and admin user flows
- Appointment lifecycle + postpone/counter-proposal flows
- Doctor availability and slot generation
- In-app notifications
- Reminder dispatch engine (in-app/email/SMS provider model)

## Tech Stack

- Backend: .NET 10, ASP.NET Core, Entity Framework Core 10, SQL Server
- Frontend: Angular 19, TypeScript, RxJS, Angular Material
- Testing: xUnit (backend), Karma/Jasmine (frontend)
- CI: GitHub Actions

## Prerequisites

- .NET SDK `10.x`
- Node.js `20.x` and npm
- SQL Server instance (local or remote)
- EF Core CLI:

```powershell
dotnet tool install --global dotnet-ef
```

Recommended for local HTTPS:

```powershell
dotnet dev-certs https --trust
```

## Quick Start (Local)

### 1) Configure backend secrets

The API intentionally fails startup if `Jwt:Key` is missing or still set to `CHANGE_ME...`.

```powershell
cd backend\MedicalAppointment.Api\MedicalAppointment.Api
dotnet user-secrets init
dotnet user-secrets set "Jwt:Key" "REPLACE_WITH_A_LONG_RANDOM_SECRET_AT_LEAST_32_CHARS"
dotnet user-secrets set "Jwt:Issuer" "MedicalAppointmentApi"
dotnet user-secrets set "Jwt:Audience" "MedicalAppointmentUi"
```

Optional connection string override:

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=YOUR_SERVER;Database=MedicalAppointmentDb;Trusted_Connection=True;TrustServerCertificate=True;"
```

### 2) Apply database migrations

From repository root:

```powershell
dotnet ef database update --project backend\MedicalAppointment.Api\MedicalAppointment.Infrastructure --startup-project backend\MedicalAppointment.Api\MedicalAppointment.Api --context AppDbContext
```

The API also checks for pending migrations on startup and will fail fast if schema is out of date.

### 3) Run backend

```powershell
cd backend\MedicalAppointment.Api
dotnet run --project MedicalAppointment.Api --launch-profile https
```

Default backend URLs:

- `https://localhost:7074`
- `http://localhost:5169`

### 4) Run frontend

```powershell
cd frontend\medical-appointment-ui
npm ci
npm start
```

Frontend URL:

- `http://localhost:4200`

The SPA auto-selects API base URL in `frontend/medical-appointment-ui/src/app/core/api.config.ts`:

- localhost frontend -> `https://localhost:7074`
- non-localhost frontend -> `https://medio-api.greenriver-343eb6db.westeurope.azurecontainerapps.io`

### 5) Access API docs

- Swagger UI: `https://localhost:7074/swagger`

## Reminder Configuration

Reminder behavior is configured under `AppointmentReminders` and provider settings under `ReminderProviders` (`appsettings.json` / user-secrets / env vars).

Relevant keys:

- `AppointmentReminders:Enabled`
- `AppointmentReminders:PollIntervalSeconds`
- `AppointmentReminders:DispatchWindowMinutes`
- `AppointmentReminders:Channels:InAppEnabled`
- `AppointmentReminders:Channels:EmailEnabled`
- `AppointmentReminders:Channels:SmsEnabled`
- `ReminderProviders:Email:Provider` (`Smtp`, `SendGrid`, `Brevo`, `None`)
- `ReminderProviders:Sms:Provider` (`Twilio`, `None`)

If a reminder channel is enabled but provider credentials are incomplete, startup validation throws a configuration error.

Example SMTP + Twilio user-secrets:

```powershell
cd backend\MedicalAppointment.Api\MedicalAppointment.Api

dotnet user-secrets set "ReminderProviders:Email:Provider" "Smtp"
dotnet user-secrets set "ReminderProviders:Email:FromEmail" "no-reply@yourdomain.com"
dotnet user-secrets set "ReminderProviders:Email:FromName" "Medio"
dotnet user-secrets set "ReminderProviders:Email:Smtp:Host" "smtp.yourdomain.com"
dotnet user-secrets set "ReminderProviders:Email:Smtp:Port" "587"
dotnet user-secrets set "ReminderProviders:Email:Smtp:EnableSsl" "true"
dotnet user-secrets set "ReminderProviders:Email:Smtp:Username" "smtp-user"
dotnet user-secrets set "ReminderProviders:Email:Smtp:Password" "smtp-password"

dotnet user-secrets set "ReminderProviders:Sms:Provider" "Twilio"
dotnet user-secrets set "ReminderProviders:Sms:Twilio:AccountSid" "ACxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx"
dotnet user-secrets set "ReminderProviders:Sms:Twilio:AuthToken" "your-twilio-auth-token"
dotnet user-secrets set "ReminderProviders:Sms:Twilio:FromNumber" "+15551234567"
```

Example SendGrid:

```powershell
dotnet user-secrets set "ReminderProviders:Email:Provider" "SendGrid"
dotnet user-secrets set "ReminderProviders:Email:FromEmail" "no-reply@yourdomain.com"
dotnet user-secrets set "ReminderProviders:Email:FromName" "Medio"
dotnet user-secrets set "ReminderProviders:Email:SendGrid:ApiKey" "SG.xxxxx"
```

Example Brevo API:

```powershell
dotnet user-secrets set "ReminderProviders:Email:Provider" "Brevo"
dotnet user-secrets set "ReminderProviders:Email:FromEmail" "no-reply@yourdomain.com"
dotnet user-secrets set "ReminderProviders:Email:FromName" "Medio"
dotnet user-secrets set "ReminderProviders:Email:Brevo:ApiKey" "xkeysib-xxxxxx"
```

## Email Verification Configuration

New user registration now requires email verification before login access is granted.

Flow summary:

- `POST /api/User/register` creates the account and issues a verification code
- `POST /api/User/login` returns `403` for unverified users and can trigger a new code (subject to cooldown/rate limits)
- `POST /api/User/verify-email` validates the code and activates the account
- `POST /api/User/resend-verification-code` sends a new code when allowed

Configuration keys:

- `EmailVerification:Enabled`
- `EmailVerification:CodeLength`
- `EmailVerification:CodeTtlMinutes`
- `EmailVerification:ResendCooldownSeconds`
- `EmailVerification:MaxFailedAttemptsPerCode`
- `EmailVerification:MaxSendsPerDay`
- `EmailVerification:BrevoTemplateId`
- `EmailVerification:TemplateAppUrl`
- `EmailVerification:TemplateSupportEmail`
- `EmailVerification:HashKey`

`EmailVerification:HashKey` should be set to a long random secret in user-secrets or env vars. If omitted, the API falls back to `Jwt:Key`.

Example setup:

```powershell
cd backend\MedicalAppointment.Api\MedicalAppointment.Api

# Generate and set a random hashing secret
$secret = -join ((1..64) | ForEach-Object { '{0:x}' -f (Get-Random -Maximum 16) })
dotnet user-secrets set "EmailVerification:HashKey" $secret

# Configure transactional email sender for verification emails
dotnet user-secrets set "ReminderProviders:Email:Provider" "Brevo"
dotnet user-secrets set "ReminderProviders:Email:FromEmail" "no-reply@yourdomain.com"
dotnet user-secrets set "ReminderProviders:Email:FromName" "Medio"
dotnet user-secrets set "ReminderProviders:Email:Brevo:ApiKey" "<your-brevo-api-key>"

# Optional Brevo transactional template integration
dotnet user-secrets set "EmailVerification:BrevoTemplateId" "2"
dotnet user-secrets set "EmailVerification:TemplateAppUrl" "http://localhost:4200/login"
dotnet user-secrets set "EmailVerification:TemplateSupportEmail" "support@yourdomain.com"
```

Note: verification emails use the same configured email provider as reminder emails.  
If `EmailVerification:BrevoTemplateId` is set (>0) and provider is `Brevo`, verification emails are sent using Brevo `templateId` + dynamic params (`code`, `ttlMinutes`, `firstName`, `appUrl`, `supportEmail`, `year`).

## Running Tests

### Backend

```powershell
cd backend\MedicalAppointment.Api
dotnet test MedicalAppointment.Api.sln --configuration Release --collect:"XPlat Code Coverage" --results-directory TestResults --logger "trx;LogFileName=test-results.trx"
```

### Frontend

```powershell
cd frontend\medical-appointment-ui
npm test
```

CI-style frontend coverage run:

```powershell
npm run test:ci:coverage
```

## CI Quality Gates

GitHub Actions workflow: `.github/workflows/ci.yml`

- Backend:
  - restore/build/test on .NET 10
  - on test failure, failed test names/messages are emitted as GitHub check annotations
  - coverage floor (excluding `Infrastructure/Migrations` and generated `obj` sources):
    - line >= `55%`
    - branch >= `40%`
- Frontend:
  - install/build/test with headless Chrome
  - coverage artifacts uploaded

## Security Notes

- JWT signing key is mandatory and must not be a placeholder.
- JWT validation enforces issuer, audience, signature, and expiry.
- API reads bearer token from auth header and optional secure cookie name (`Jwt:CookieName`, default `medio_access_token`).
- Rate limiting is applied to auth routes (login/register policies).
- CORS allowed origins are configured via `Cors:AllowedOrigins` (array) or `Cors:AllowedOrigins` (comma/semicolon separated string).
- Default CORS origin fallback is `http://localhost:4200` when no origins are configured.

Example production CORS env vars:

```powershell
Cors__AllowedOrigins__0=https://app.mediohealth.ro
Cors__AllowedOrigins__1=http://localhost:4200
```

Important: configure origins as scheme + host (+ optional port), without trailing slash.

## Troubleshooting

- `Jwt:Key is missing or still set to placeholder`:
  - configure `Jwt:Key` via user-secrets or environment variables.
- `Database has pending migrations`:
  - run the `dotnet ef database update` command shown above.
- Frontend cannot reach API:
  - verify backend is running on `https://localhost:7074`
  - verify `API_BASE_URL` in `frontend/medical-appointment-ui/src/app/core/api.config.ts`
  - ensure local dev certificate is trusted.
- Azure Static Web Apps returns `404` on refresh/deep links (`/login`, `/dashboard/...`):
  - ensure `frontend/medical-appointment-ui/public/staticwebapp.config.json` is present in deployed artifacts.
