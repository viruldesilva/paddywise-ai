# PaddyWise-AI Production Deployment Guide

This document outlines the production deployment architecture, required environment variables, hosting configurations for Render, Vercel, and Neon PostgreSQL, and mobile APK release build instructions.

---

## 1. Architecture Overview

```
                      +-----------------------------+
                      |   Neon PostgreSQL Database   |
                      +-----------------------------+
                                     ^
                                     | ConnectionStrings:DefaultConnection
                                     v
+-----------------------+     +-------------------------------+
|   PaddyWise Web (SPA)  | --> | PaddyWise Backend (ASP.NET 8) |
|      (on Vercel)      |     |       (on Render Docker)      |
+-----------------------+     +-------------------------------+
                                     ^
+-----------------------+            |
| PaddyWise Mobile (APK)| -----------+
|    (Flutter Android)  |
+-----------------------+
```

- **Backend Web API**: Hosted on **Render** as a Linux Docker container listening on the dynamically assigned `PORT`.
- **Admin / Officer Web Portal**: Hosted on **Vercel** with Vite SPA client-side routing rewrites (`vercel.json`).
- **Farmer Mobile App**: Android APK built with Flutter using `--dart-define=API_BASE_URL=...`.
- **Database**: Managed **Neon PostgreSQL** instance (migrations applied manually from developer machine).

---

## 2. Environment Variables

> **IMPORTANT**: Never commit production values or API keys to source control. Set these in the corresponding hosting platform's dashboard.

### 2.1 Backend (Render Web Service)

ASP.NET Core automatically maps environment variables using double underscores (`__`) as section separators:

| Environment Variable Name | Required | Description |
|---|---|---|
| `ConnectionStrings__DefaultConnection` | **Yes** | Neon PostgreSQL connection string (`Host=...;Database=...;Username=...;Password=...;SslMode=Require`) |
| `Jwt__Key` | **Yes** | Symmetric signing key for JWT tokens (must be at least 32 characters long) |
| `Jwt__Issuer` | **Yes** | JWT issuer identifier (e.g. `PaddyWiseApi`) |
| `Jwt__Audience` | **Yes** | JWT audience identifier (e.g. `PaddyWiseClient`) |
| `Jwt__AccessTokenExpiryMinutes` | No | Access token expiration in minutes (defaults to `20`) |
| `Jwt__RefreshTokenExpiryDays` | No | Refresh token expiration in days (defaults to `7`) |
| `Cors__AllowedOrigins` | **Yes** | Comma-separated list of allowed frontend origins (e.g. `https://paddywise-web.vercel.app,http://localhost:5173`) |
| `Gemini__ApiKey` | **Yes** | Google Gemini API key for AI agent analysis |
| `Gemini__Model` | No | Gemini model name (defaults to `gemini-3.6-flash`) |
| `Gemini__PestDiseaseApiKey` | No | Optional dedicated Gemini key for Component 3 (falls back to `Gemini__ApiKey` if unset) |
| `Gemini__PestDiseaseModel` | No | Optional dedicated Gemini model for Component 3 (falls back to `Gemini__Model` if unset) |
| `Brevo__ApiKey` | **Yes** | Brevo API key for automated email notifications |
| `Brevo__FromEmail` | **Yes** | Sender email address (registered/verified in Brevo) |
| `Brevo__FromName` | No | Sender display name (defaults to `PaddyWise AI`) |
| `Storage__ConnectionString` | **Yes** | Azure Blob Storage connection string for crop observation photos |
| `Storage__ContainerName` | **Yes** | Azure Blob Storage container name for uploaded photos |
| `PORT` | Auto | Provided automatically by Render's container runtime environment |

### 2.2 Frontend (Vercel)

| Environment Variable Name | Required | Description |
|---|---|---|
| `VITE_API_BASE_URL` | **Yes** | Base URL pointing to the deployed backend API, including `/api` (e.g. `https://paddywise-backend.onrender.com/api`) |

### 2.3 Mobile (Flutter `--dart-define`)

| Build Variable Name | Required | Description |
|---|---|---|
| `API_BASE_URL` | **Yes** (prod) | Build-time parameter defining the backend API URL (e.g. `https://paddywise-backend.onrender.com/api`) |

---

## 3. Render Configuration (Backend)

1. Sign in to [Render Dashboard](https://dashboard.render.com/) and click **New + > Web Service**.
2. Connect your Git repository.
3. Configure the service settings:
   - **Name**: `paddywise-backend` (or your choice)
   - **Language / Runtime**: `Docker`
   - **Region**: Singapore / closest region to database (Neon Southeast Asia)
   - **Root Directory**: `paddywise-backend`
   - **Dockerfile Path**: `Dockerfile`
     *(Note: If leaving Root Directory as repository root `.`, set Dockerfile Path to `paddywise-backend/Dockerfile`)*
   - **Instance Type**: Free or Starter
4. Health Check:
   - **Health Check Path**: `/health`
5. Under **Environment Variables**, add all required keys listed in section 2.1 above.
6. Click **Deploy Web Service**.

> **Note on HTTPS and Reverse Proxy**: Render terminates HTTPS at its edge proxy. The backend includes `ForwardedHeaders` middleware (`XForwardedFor | XForwardedProto`) before HTTPS redirection to ensure seamless TLS termination without redirect loops.

---

## 4. Vercel Configuration (React Frontend)

1. Sign in to [Vercel Dashboard](https://vercel.com/) and click **Add New... > Project**.
2. Import the Git repository.
3. Configure the project settings:
   - **Framework Preset**: `Vite`
   - **Root Directory**: `paddywise-web` (click Edit and select `paddywise-web`)
   - **Build Command**: `npm run build`
   - **Output Directory**: `dist`
   - **Install Command**: `npm install`
4. Under **Environment Variables**:
   - Key: `VITE_API_BASE_URL`
   - Value: `https://<your-render-backend-url>.onrender.com/api`
5. Click **Deploy**.

> **Note on SPA Routing**: `paddywise-web/vercel.json` provides an SPA rewrite rule (`{"source": "/(.*)", "destination": "/index.html"}`) ensuring all direct routes (such as `/login/admin` or `/dashboard/officer`) resolve cleanly on browser refresh without 404s.

---

## 5. Flutter Mobile App (Release APK Build)

To build a production release APK pointing to the deployed Render backend:

```bash
cd paddywise_mobile
flutter build apk --release --dart-define=API_BASE_URL="https://<your-render-backend-url>.onrender.com/api"
```

The resulting release APK will be located at:
```
paddywise_mobile/build/app/outputs/flutter-apk/app-release.apk
```

### Local Development Behavior
When `--dart-define=API_BASE_URL` is omitted, the app automatically falls back to:
- **Android Emulator**: `http://10.0.2.2:5164/api`
- **Web / iOS / Desktop**: `http://localhost:5164/api`

---

## 6. Database Migrations (Neon PostgreSQL)

Migrations are **not** auto-run on backend startup. Apply migrations manually from your local machine with `dotnet ef`:

```bash
cd paddywise-backend
dotnet ef database update
```

*(Ensure your local user-secrets or `ConnectionStrings:DefaultConnection` environment variable is configured with the target Neon connection string prior to running).*

---

## 7. Verification Checklist

- [ ] `/health` returns HTTP 200 `Healthy` without authentication.
- [ ] `/swagger` and `/swagger/v1/swagger.json` load public API documentation in production.
- [ ] CORS permits requests from the deployed Vercel domain.
- [ ] Web application routes do not 404 on hard browser refresh.
- [ ] Admin and Agricultural Officer login succeeds from the deployed Vercel web client.
- [ ] Mobile app communicates with the deployed Render backend via the built APK.
