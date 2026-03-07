# Seragenda — Obrigenie Backend API

ASP.NET Core 8 REST API powering the **Obrigenie** teacher agenda application.
Serves both the API endpoints and the Blazor WebAssembly frontend static files.

> Full documentation (architecture, deployment, API reference) is in the [frontend README](../../csharpcallender/Obrigenie/README.md).

---

## Quick Start

```bash
# 1. Restore packages
dotnet restore seragenda.sln

# 2. Create appsettings.Development.json with your credentials (see Configuration below)

# 3. Apply database migrations
dotnet ef database update --project seragenda

# 4. Run
dotnet run --project seragenda
# API: http://localhost:5276
# Swagger: http://localhost:5276/swagger
```

---

## Configuration

Create `seragenda/appsettings.Development.json` (git-ignored):

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=seragenda;Username=postgres;Password=..."
  },
  "JwtSettings": {
    "SecretKey": "your-secret-key-min-32-chars"
  },
  "EmailSettings": {
    "Host": "smtp-relay.brevo.com",
    "Port": "587",
    "Username": "brevo-smtp-login",
    "Password": "brevo-smtp-key",
    "FromEmail": "noreply@yourdomain.com",
    "FromName": "Obrigenie"
  },
  "GoogleAuth": {
    "ClientId": "...",
    "ClientSecret": "..."
  },
  "MicrosoftAuth": {
    "ClientId": "...",
    "ClientSecret": "..."
  },
  "AppSettings": {
    "FrontendUrl": "http://localhost:5276"
  },
  "Cors": {
    "AllowedOrigins": [ "http://localhost:5276" ]
  }
}
```

---

## API Endpoints Summary

| Group | Routes |
|---|---|
| Auth | POST `/api/auth/login`, `/api/auth/register`, GET `/api/auth/confirm`, `/api/auth/google`, `/api/auth/microsoft`, `/api/auth/exchange` |
| Courses | GET/POST/DELETE `/api/courses`, GET `/api/courses/date/{date}` |
| Notes | GET `/api/notes/date/{date}`, `/api/notes/range`, POST/DELETE `/api/notes` |
| License | POST `/api/access/validate`, GET `/api/access/check` |
| Reference | GET `/api/ref/cours`, `/api/ref/niveaux/{code}`, `/api/ref/domaines/{code}/{niveau}` |
| Admin | GET/POST/DELETE `/api/admin/licenses`, GET `/api/update-scolaire` |
| Health | GET `/api/health` |

---

## Deployment (Production)

```bash
# Build
dotnet publish seragenda/seragenda.csproj -c Release -o ./publish

# Restart service on server
sudo systemctl restart serapi
```

Create `/var/www/serapi/appsettings.Production.json` on the server with production credentials.
The Data Protection keys directory must exist: `/var/www/serapi/dataprotection-keys`
