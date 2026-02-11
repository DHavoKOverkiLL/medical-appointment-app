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

The SPA currently points to `https://localhost:7074` in `frontend/medical-appointment-ui/src/app/core/api.config.ts`.

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
- `ReminderProviders:Email:Provider` (`Smtp`, `SendGrid`, `None`)
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
- CORS is currently restricted to `http://localhost:4200` for local development; update policy before deploying to other origins.

## Troubleshooting

- `Jwt:Key is missing or still set to placeholder`:
  - configure `Jwt:Key` via user-secrets or environment variables.
- `Database has pending migrations`:
  - run the `dotnet ef database update` command shown above.
- Frontend cannot reach API:
  - verify backend is running on `https://localhost:7074`
  - verify `API_BASE_URL` in `frontend/medical-appointment-ui/src/app/core/api.config.ts`
  - ensure local dev certificate is trusted.
