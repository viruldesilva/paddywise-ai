# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project overview

PaddyWise AI ("Kumburu") is an agentic AI-powered paddy farming decision support platform for Sri Lankan farmers. It has three independent clients around one ASP.NET Core backend:

- `paddywise-backend/` — ASP.NET Core 8 Web API (C#), JWT auth, PostgreSQL via EF Core. This is the real API.
- `paddywise-web/` — React 19 + TypeScript + Vite SPA. Fully wired to the backend API.
- `paddywise_mobile/` — Flutter app. **Currently a standalone prototype**: `AuthService` (`paddywise_mobile/lib/services/auth_service.dart`) mocks auth entirely in-memory/`shared_preferences` with hardcoded seed users — it does not call `paddywise-backend`. Don't assume mobile and web share an auth backend until this is wired up.
- `database/schema.sql` — hand-written reference PostgreSQL schema (roles, divisions, users, refresh_tokens). EF Core migrations in `paddywise-backend/Migrations/` are the actual source of truth for the deployed schema; `schema.sql` is a readable snapshot/seed reference and can drift from migrations.

The intended product shape (per README) is a 4-agent AI workflow: cultivation planning, resource advisory, pest/disease diagnosis, and officer-approved recommendations. Only auth and a basic crop-activity/fertilizer-validation flow exist so far.

## Backend (paddywise-backend)

ASP.NET Core 8, Entity Framework Core with Npgsql (PostgreSQL), JWT bearer auth, BCrypt password hashing.

Common commands (run from `paddywise-backend/`):
```
dotnet restore
dotnet build
dotnet run                              # runs on http://localhost:5164, Swagger UI in Development
dotnet ef migrations add <Name>         # add a migration after changing entities/ApplicationDbContext
dotnet ef database update               # apply migrations to the configured PostgreSQL DB
```
There is no test project in this solution yet.

Architecture:
- Feature folders are grouped by domain under `Controllers/`, `Services/`, `DTOs/`, `Entities/` — currently `Shared/` (auth, users, refresh tokens) and `FieldCultivation/` (field data). Follow this `<Domain>/` subfolder convention when adding new features (e.g. a future `PestDisease/` or `ResourceAdvisory/` folder), rather than flattening everything into one folder.
- `Program.cs` is split into two clearly separated sections: service registration (`builder.Services.*`, before `builder.Build()`) and the middleware pipeline (`app.Use*`/`app.Map*`, after `builder.Build()`). Keep new registrations/middleware in the matching section.
- Auth flow: `AuthController` (`Controllers/Shared/AuthController.cs`) → `IAuthService`/`AuthService` (`Services/Shared/`). Login/register issue a short-lived JWT access token plus a long-lived opaque refresh token persisted in the `refresh_tokens` table; `POST /api/auth/refresh` rotates the refresh token (revokes old, issues new pair) rather than reusing it.
- CORS is currently locked to a single hardcoded origin, `http://localhost:5173` (the Vite dev server), via the `AllowReactApp` policy in `Program.cs` — update this if the frontend dev port or a deployed origin changes.
- `appsettings.json` contains a live Neon Postgres connection string and JWT signing key committed in the repo (this is pre-existing dev config, not something to "fix" silently — flag it to the user if asked about secrets handling, don't just rotate/remove it unprompted).

## Web frontend (paddywise-web)

React 19 + TypeScript + Vite, react-router-dom v7, axios.

Common commands (run from `paddywise-web/`):
```
npm install
npm run dev       # Vite dev server (must stay on :5173 to match backend CORS policy)
npm run build     # tsc -b && vite build
npm run lint      # eslint .
npm run preview
```
No test runner is configured yet.

Architecture:
- `API_BASE_URL` is hardcoded in `src/api/axiosInstance.ts` to `http://localhost:5164/api` — update here if the backend port/host changes.
- `axiosInstance` implements automatic access-token refresh on 401s: it queues concurrent requests during a refresh, retries once (`_retry` flag) then force-logs-out and redirects to `/login` on repeated failure. Auth endpoints (`/auth/login`, `/auth/register`, `/auth/refresh`) are exempted from this interceptor to avoid refresh loops.
- Auth state lives in `AuthContext` (`src/context/AuthContext.tsx`), backed by `services/authService.ts` (API calls) and `services/tokenStorage.ts` (persistence). On mount it re-validates the stored session against `GET /api/auth/me`.
- Routing (`src/App.tsx`) sends authenticated users to a role-specific dashboard rather than one generic dashboard: `getRoleDashboardRoute()` (`src/utils/roleRoutes.ts`) maps `UserRole` (`Farmer | AgriculturalOfficer | Admin | FieldOfficer`, defined in `src/types/auth.ts`) to `/dashboard/farmer`, `/dashboard/officer`, `/dashboard/admin`, `/dashboard/field-officer`. Add new roles in both places together.
- Feature work is organized under `src/features/<feature-name>/{components,pages,data}` (e.g. `src/features/crop-resource/`), separate from the generic `src/components/` and `src/pages/`. Prefer this structure for new domain features rather than adding more top-level pages/components.

## Mobile app (paddywise_mobile)

Flutter, Material theme in `lib/theme/app_theme.dart`.

Common commands (run from `paddywise_mobile/`):
```
flutter pub get
flutter run
flutter analyze     # uses analysis_options.yaml (flutter_lints)
flutter test        # runs test/widget_test.dart
```

Notes:
- `lib/services/auth_service.dart` is a self-contained mock: seed users are hardcoded, "login" just does an in-memory/`shared_preferences` lookup with a plaintext password match, and there is no HTTP client anywhere in the app yet. When wiring this app to the real backend, this whole service is expected to be replaced with real API calls (mirroring the web app's `authService`/`tokenStorage` pattern), not incrementally patched.
- `main.dart` decides the initial screen (`DashboardScreen` vs `LoginScreen`) synchronously from `AuthService.getCurrentUser()` after `AuthService.init()` — any real backend integration needs to preserve this "restore session before first frame" behavior.
