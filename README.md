# medical-appointment-app

Web app for scheduling medical appointments and managing consult requests.

## Secure local setup

### 1. Configure backend secrets (required)

```powershell
cd backend\MedicalAppointment.Api\MedicalAppointment.Api
dotnet user-secrets init
dotnet user-secrets set "Jwt:Key" "REPLACE_WITH_A_LONG_RANDOM_SECRET_AT_LEAST_32_CHARS"
```

Optional (if you want to override connection string from config):

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=YOUR_SERVER;Database=MedicalAppointmentDb;Trusted_Connection=True;TrustServerCertificate=True;"
```

### 2. Apply database migrations

```powershell
cd c:\Medio
dotnet ef database update --project backend\MedicalAppointment.Api\MedicalAppointment.Infrastructure --startup-project backend\MedicalAppointment.Api\MedicalAppointment.Api --context AppDbContext
```

### Reminder engine configuration (Phase 2)

`AppointmentReminders` settings are in `appsettings.json` / `appsettings.Development.json`:

- `Enabled`: turn scheduler on/off
- `PollIntervalSeconds`: how often due reminders are scanned
- `DispatchWindowMinutes`: max delay accepted for sending a due reminder
- `Channels.InAppEnabled`: create in-app reminder notifications
- `Channels.EmailEnabled`: send reminders via pluggable email sender (mock provider registered by default)
- `Channels.SmsEnabled`: send reminders via pluggable SMS sender (mock provider registered by default)

### 3. Start backend

```powershell
cd backend\MedicalAppointment.Api
dotnet run --project MedicalAppointment.Api --launch-profile https
```

### 4. Start frontend

```powershell
cd frontend\medical-appointment-ui
npm install
npm start
```

Open:

- UI: `http://localhost:4200`
- API Swagger: `https://localhost:7074/swagger`
