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
- `Channels.EmailEnabled`: send reminders through configured real provider (`Smtp` or `SendGrid`)
- `Channels.SmsEnabled`: send reminders through configured real provider (`Twilio`)

Provider settings live under `ReminderProviders`:

- `ReminderProviders:Email:Provider` = `Smtp` | `SendGrid` | `None`
- `ReminderProviders:Sms:Provider` = `Twilio` | `None`

If a channel is enabled but provider credentials are missing, API startup will fail with a clear configuration error.

Example user-secrets for SMTP + Twilio:

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

Optional SendGrid instead of SMTP:

```powershell
dotnet user-secrets set "ReminderProviders:Email:Provider" "SendGrid"
dotnet user-secrets set "ReminderProviders:Email:FromEmail" "no-reply@yourdomain.com"
dotnet user-secrets set "ReminderProviders:Email:FromName" "Medio"
dotnet user-secrets set "ReminderProviders:Email:SendGrid:ApiKey" "SG.xxxxx"
```

SMS reminders require a patient phone number (`PhoneNumber`) on profile data. The API now supports this field in registration/admin user create/update/profile update flows.

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
