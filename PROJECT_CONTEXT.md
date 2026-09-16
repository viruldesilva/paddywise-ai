# PaddyWise-AI ("Kumburu") — Full Project Context Document

> Generated 2026-09-16 from a full read of the repository at branch `feature/field-cultivation`.
> Purpose: paste this into an LLM chat as complete context before planning a new feature.

---

## 1. What this project is

**PaddyWise-AI** (product name in the UI: **Kumburu**) is an academic/capstone-style
**agentic AI decision-support platform for Sri Lankan paddy (rice) farmers**.

The pitch (from the landing page copy, which is the clearest statement of product intent):

> "Kumburu connects farmers, extension officers and buyers on one system — so a pest report
> gets answered in hours, not weeks, and the harvest finds a fair price."

Three problems it claims to solve:
1. Pest/disease reports travel by phone or paper with no record of who acted on them.
2. Treatment advice is not checked against safe dosage limits and has no approval step.
3. Farmers sell harvest blind, to the first mill that calls, not the best offer.

The intended **5-step core workflow** (from `WorkflowSection.tsx`):
`01 Report → 02 Analyze (AI agent) → 03 Recommend (dosage-checked) → 04 Approve (officer sign-off) → 05 Act (farmer gets plan)`

The intended **4 business components** (from `FeaturesSection.tsx`):
- **Component A — Cultivation tracking**: register fields, log sowing dates, follow growth stages.
- **Component B — Pest & disease advisory**: photo → AI diagnosis → officer-approved treatment.
- **Component C — Harvest & market**: record yield/grade, compare live buyer offers.
- **Component D — Resources & weather**: fertilizer stock/subsidy tracking, spray & irrigation timing from forecast.

**Reality check: none of components A–D are functionally implemented.** What exists today is
authentication + static/mock dashboards + one client-side fertilizer rule-validation demo.

---

## 2. Repository layout

```
paddywise-ai/
├── README.md                  # one-paragraph project pitch
├── .gitattributes
├── package-lock.json          # empty stub, no root package.json (vestigial)
├── database/
│   └── schema.sql             # HAND-WRITTEN Postgres schema — DIVERGES from the EF model (see §7)
├── paddywise-backend/         # ASP.NET Core 8 Web API  (project name: PaddyWise.Api)
├── paddywise-web/             # React 19 + TypeScript + Vite SPA
└── paddywise_mobile/          # Flutter app (Dart 3.9)
```

Three independent tech stacks, no shared contract/codegen, no monorepo tooling, no CI, no Docker,
no tests beyond one Flutter smoke test.

---

## 3. Backend — `paddywise-backend` (ASP.NET Core 8)

**Project**: `PaddyWise.Api.csproj`, `net8.0`, nullable + implicit usings enabled.

### Packages
| Package | Version | Purpose |
|---|---|---|
| `Microsoft.AspNetCore.Authentication.JwtBearer` | 8.0.10 | JWT bearer auth |
| `Npgsql.EntityFrameworkCore.PostgreSQL` | 8.0.10 | Postgres provider |
| `Microsoft.EntityFrameworkCore.Design` | 8.0.10 | migrations |
| `BCrypt.Net-Next` | 4.2.0 | password hashing |
| `Swashbuckle.AspNetCore` | 6.6.2 | Swagger UI with Bearer scheme |

### Full file inventory (only 17 .cs files)
```
Controllers/Shared/AuthController.cs
DTOs/Shared/{AuthResponseDto, LoginRequestDto, RefreshRequestDto, RegisterRequestDto}.cs
Data/ApplicationDbContext.cs
Entities/Shared/{User, UserRole, RefreshToken}.cs
Entities/FieldCultivation/Field.cs
Services/Shared/{IAuthService, AuthService}.cs
Migrations/20260909130307_InitialCreate.cs        # Fields table
Migrations/20260910105145_AddUsersAndRefreshTokens.cs
Migrations/ApplicationDbContextModelSnapshot.cs
Program.cs
```

Note the **folder convention already in place**: `Shared/` for cross-cutting, and a
per-business-component folder (`FieldCultivation/`). New features should follow
`Entities/<Component>/`, `Controllers/<Component>/`, `Services/<Component>/`, `DTOs/<Component>/`.

### `Program.cs` — what is wired
- Controllers + Swagger (with a "Bearer" security definition; token entered without the `Bearer ` prefix).
- `IAuthService → AuthService` (Scoped).
- `ApplicationDbContext` on Npgsql, connection string `ConnectionStrings:DefaultConnection`.
- JWT bearer validation: validates issuer, audience, lifetime, signing key (HMAC-SHA256).
- Authorization (no policies/roles registered — only the default).
- CORS policy `AllowReactApp` → **hard-coded to `http://localhost:5173`** only.
- Pipeline: Swagger (Dev only) → HttpsRedirection → CORS → Authentication → Authorization → MapControllers.
- **Leftover template junk**: the `/weatherforecast` minimal endpoint + `WeatherForecast` record are still present.

### Entities

```csharp
// Entities/Shared/User.cs
class User { int Id; string Name; string Email; string PasswordHash;
             UserRole Role; string? Phone; DateTime CreatedAt; DateTime UpdatedAt; }

// Entities/Shared/UserRole.cs  -> stored in DB as int
enum UserRole { Farmer=0, AgriculturalOfficer=1, Admin=2, FieldOfficer=3 }

// Entities/Shared/RefreshToken.cs
class RefreshToken { int Id; int UserId; string Token; DateTime ExpiresAt;
                     bool IsRevoked; DateTime CreatedAt; }
// NOTE: UserId is a plain int — NO EF navigation property, NO FK constraint configured.

// Entities/FieldCultivation/Field.cs   <-- table exists, but NOTHING uses it
class Field { int Id; string Name; decimal Area; string SoilType;
              string IrrigationType; double? Latitude; double? Longitude; }
```

`ApplicationDbContext` exposes `Fields`, `Users`, `RefreshTokens`; `OnModelCreating` adds a unique
index on `User.Email` and on `RefreshToken.Token`. That's all the model config there is.

### API surface — **4 endpoints total**

Base: `http://localhost:5164/api` (http profile; https profile also binds `https://localhost:7188`)

| Method | Route | Auth | Body | Returns |
|---|---|---|---|---|
| POST | `/api/auth/register` | anon | `{name,email,password,role,phone?}` | `AuthResponseDto` / 400 `{message}` |
| POST | `/api/auth/login` | anon | `{email,password}` | `AuthResponseDto` / 401 `{message}` |
| POST | `/api/auth/refresh` | anon | `{refreshToken}` | `AuthResponseDto` / 401 `{message}` |
| GET  | `/api/auth/me` | **JWT** | – | `{name, role}` |
| GET  | `/weatherforecast` | anon | – | template leftover, delete it |

`AuthResponseDto = { accessToken, refreshToken, name, email, role }` (camelCased over the wire).

### Auth behaviour (`AuthService.cs`)
- **Register**: rejects duplicate email; `Enum.TryParse<UserRole>(request.Role, ignoreCase:true)` —
  so the client must send `"Farmer"`, `"AgriculturalOfficer"`, `"Admin"`, `"FieldOfficer"`
  (or the numeric value). BCrypt hash. Immediately issues a token pair (auto-login on register).
- **Login**: email lookup (case-sensitive `==`, no `.ToLower()`), BCrypt verify, issue pair.
- **Refresh**: looks up the refresh token row; rejects if missing/revoked/expired; **rotates**
  (marks old revoked, issues a brand-new pair). Old tokens are never deleted or pruned.
- **Access token claims**: `NameIdentifier`(user id), `Name`, `Email`, `Role`.
  Expiry from `Jwt:AccessTokenExpiryMinutes` = **20 min**.
- **Refresh token**: 64 random bytes, Base64. Expiry `Jwt:RefreshTokenExpiryDays` = **7 days**.

### Configuration (`appsettings.json`) — ⚠ SECURITY ISSUES, FIX BEFORE ANYTHING ELSE
```jsonc
"Jwt": { "Key": "REPLACE_WITH_A_LONG_RANDOM_SECRET_AT_LEAST_32_CHARS",  // placeholder, unchanged
         "Issuer": "PaddyWiseApi", "Audience": "PaddyWiseClient",
         "AccessTokenExpiryMinutes": "20", "RefreshTokenExpiryDays": "7" },
"ConnectionStrings": { "DefaultConnection":
   "Host=ep-morning-silence-...neon.tech;Database=neondb;Username=neondb_owner;Password=<REAL PASSWORD>;SslMode=Require" }
```
- **A live Neon Postgres password is committed to git** in `appsettings.json` (the file is tracked;
  only `appsettings.Development.json` / `appsettings.Local.json` are gitignored).
  → Rotate the Neon credential and move it to user-secrets / env vars.
- The JWT signing key is still the literal placeholder string.

### Not present in the backend
No controllers/services for: fields, cultivation cycles, activities, fertilizer, pests/diseases,
images/uploads, AI agents, weather, harvest, buyers/offers, notifications, knowledge base,
admin user management, audit logs, divisions. No role-based `[Authorize(Roles=...)]` anywhere.
No validation attributes on DTOs, no FluentValidation, no global exception handler, no logging
beyond defaults, no rate limiting, no health checks, no tests.

---

## 4. Web frontend — `paddywise-web` (React 19 + TS + Vite)

### Stack
React `19.2`, react-dom, `react-router-dom` 7, `axios` 1.20, `lucide-react` icons,
TypeScript ~6.0, Vite 8, ESLint 10. **No UI framework, no Tailwind** — hand-written CSS with
CSS custom properties. No state library (Context only). No test runner.

Scripts: `npm run dev` (Vite, port 5173), `build` (`tsc -b && vite build`), `lint`, `preview`.

### Directory map
```
src/
├── main.tsx, App.tsx
├── api/axiosInstance.ts         # axios + JWT interceptors  ← important
├── services/authService.ts      # AuthService class + extractApiErrorMessage()
├── services/tokenStorage.ts     # localStorage wrapper behind ITokenStorage
├── context/AuthContext.tsx      # AuthProvider + useAuth
├── hooks/useAuth.ts             # re-export
├── hooks/useReveal.ts           # IntersectionObserver scroll animations
├── types/auth.ts                # UserRole union + DTO types
├── utils/roleRoutes.ts          # role → dashboard path map
├── components/                  # ProtectedRoute, Sidebar + 8 landing-page sections
├── pages/                       # Home, LoginPage, RegisterPage, DashboardPage
├── features/crop-resource/      # the ONE feature-sliced module (see below)
└── styles/                      # global.css, Auth.css, Dashboard.css, Sidebar.css
```

Note the **two competing conventions**: page-based (`pages/`, `components/`) for auth + landing,
and feature-sliced (`features/crop-resource/{components,data,pages}`) for the newest work.
New features should follow the `features/<name>/` pattern.

### Routing (`App.tsx`)
| Path | Guard | Component |
|---|---|---|
| `/` | public | `Home` (landing page) |
| `/login` | public | `LoginPage` |
| `/register` | public | `RegisterPage` |
| `/dashboard` | protected | `RoleRedirect` → role-specific path |
| `/dashboard/farmer` | protected | `DashboardPage roleView="Farmer"` |
| `/dashboard/officer` | protected | `DashboardPage roleView="AgriculturalOfficer"` |
| `/dashboard/admin` | protected | `DashboardPage roleView="Admin"` |
| `/dashboard/field-officer` | protected | `DashboardPage roleView="FieldOfficer"` |
| `/activities` | protected | `ActivityDashboard` (crop-resource feature) |

⚠ `ProtectedRoute` **supports** an `allowedRoles` prop but **no route passes it** — so any logged-in
user can open any role's dashboard by typing the URL. Also, `DashboardPage` renders whatever
`roleView` the *route* says, not what the *user's* role is. This is a real authorization gap.

### Auth plumbing (this part is genuinely well built)
- **`axiosInstance.ts`** — request interceptor attaches `Authorization: Bearer <token>`;
  response interceptor catches 401, skips auth endpoints, guards with `_retry`, holds a
  `failedQueue` so concurrent 401s wait on a single refresh, calls `/auth/refresh` with a *raw*
  axios instance (avoids recursion), replays the original request, and on failure clears storage
  and hard-redirects to `/login`.
- **`tokenStorage.ts`** — `ITokenStorage` interface + `LocalStorageTokenStorage`. Keys:
  `paddywise_access_token`, `paddywise_refresh_token`, `paddywise_user`. Constructor scrubs
  legacy `kumburu_users` / `kumburu_session` keys (migration from the old mock-auth era).
  ⚠ Tokens in localStorage = XSS-exposed; a future hardening story is httpOnly cookies.
- **`authService.ts`** — `AuthService implements IAuthService` (login/register/refreshToken/
  getCurrentUser/logout), each persisting the session. Plus `extractApiErrorMessage(err, fallback)`
  which unwraps `{message}`, ASP.NET `ValidationProblemDetails.errors`, and `title`. **Reuse this
  helper for every new API call.**
- **`AuthContext.tsx`** — holds `user`, `token`, `isAuthenticated`, `isLoading`; on mount rehydrates
  from localStorage then verifies against `GET /auth/me`; `login()` accepts either a DTO or
  `(email, password)`; `logout()` clears storage and hard-navigates to `/login`.
- ⚠ `API_BASE_URL` is a **hard-coded const** `http://localhost:5164/api` — should become
  `import.meta.env.VITE_API_BASE_URL`.

### Pages
- **`Home.tsx`** — composes 8 marketing sections (Navbar, Hero, Problem, Features, Workflow, Roles,
  CTA, Contact, Footer). Fully styled, static, no data.
- **`LoginPage.tsx` / `RegisterPage.tsx`** — real, working, wired to the API. Register offers
  Farmer / AgriculturalOfficer / FieldOfficer / Admin (**no Buyer**). Post-login it honours
  `location.state.from` else routes by role. Errors shown via `extractApiErrorMessage`.
- **`DashboardPage.tsx` (442 lines)** — **100% hard-coded mock data.** Four big conditional blocks:
  - *Farmer*: fake metrics ("Yala 2026 / Day 45", "3.5 Acres / Bg 352", "14.0 MT"), a photo-upload
    box whose button only fires `alert()`, a static schedule list, and a real button →`/activities`.
  - *AgriculturalOfficer*: fake approval queue card (Brown Plant Hopper, 96% confidence) with
    Approve/Request-Details buttons that only `alert()`.
  - *FieldOfficer*: fake visit route + telemetry blurb.
  - *Admin*: renders the backend's own config (API base URL, endpoint list) as content — a dev
    placeholder, not a real admin console.
- **`Sidebar.tsx`** — per-role nav lists. **Almost every link is a dead `#anchor`**
  (`#profile`, `#fields`, `#activities`, `#report`, `#weather`, `#ai`, `#ai-review`, `#expert`,
  `#stats`, `#users`, `#knowledge`, `#settings`, `#inspections`, `#upload`, `#feedback`).
  **This sidebar is effectively the product backlog** — each `#` is an unbuilt screen.

### The one real feature module: `features/crop-resource/`
Route `/activities` → `ActivityDashboard`:
- Tabs: **Irrigation | Fertilizer | Pesticide | Other**.
- `ActivityForm.tsx` renders a different field set per tab, all with `useState<any>` and
  **no validation, no typing, and no API call** — submit just bubbles the object up.
- Irrigation: date, waterLevel(cm), duration(h), source(Canal/Rain/Well).
  Fertilizer: date, type(Urea/TSP/MOP/Organic), quantity(kg/ha), cropStage, region, method.
  Pesticide: date, product, targetPest, quantity, method. Other: date, specificActivity, notes.
- Non-fertilizer submissions just `alert('... recorded successfully!')` — **nothing is persisted anywhere**.
- Fertilizer submissions open **`FertilizerValidationFlow.tsx`**, a fake 4-step "agent" animation
  (1 s per step: check stage → check region → fetch DOA rules → compare quantity), then verdicts:
  - `qty ≤ recommended` → **accept**
  - `qty ≤ recommended × 1.2` → **officer review**
  - otherwise → **reject**; no matching rule → review.
  It shows the `sourceReference` string as "Knowledge Source".
- **`data/fertilizerRules.ts`** — 19 hard-coded rules, a local TS array, self-described as
  *"mocked ranges for simulation"*, keyed by `region (Wet|Intermediate|Dry) × cropStage
  (Basal|Tillering|Panicle Initiation|Heading) × type (Urea|TSP|MOP|Organic)`, each with
  `recommendedAmountKgPerHa` and a DOA citation string.
  **This is the closest thing to a knowledge base in the project — and it lives in the browser.**

### Design system (`styles/global.css`)
CSS variables (shared 1:1 with the Flutter theme — keep them in sync):
`--cream #F6F1E3`, `--cream-deep #EDE5CF`, `--ink #212D1E`, `--ink-soft #4B5645`,
`--forest #22392A`, `--forest-deep #182A1E`, `--shoot #7FA66C`, `--shoot-light #B4CB9C`,
`--gold #E1A63B`, `--gold-deep #C48A28`, `--clay #A6693F`, `--line rgba(33,45,30,.14)`.
Fonts: headings `Fraunces` (serif), body `Work Sans`.
⚠ `ActivityDashboard.css` uses a *different* variable set (`--primary-dark`, `--text-muted`,
`glass-panel`) — the crop-resource feature is visually off-system from the rest of the app.

---

## 5. Mobile — `paddywise_mobile` (Flutter)

Dart SDK `^3.9.2`. Dependencies: **only** `cupertino_icons` and `shared_preferences`.
**No `http`/`dio` package — the mobile app cannot talk to the backend at all.**

```
lib/
├── main.dart                    (31)  AuthService.init() then Login or Dashboard
├── models/user.dart            (131)  UserRole enum + User + StoredUser
├── services/auth_service.dart  (204)  100% LOCAL MOCK AUTH
├── screens/login_screen.dart   (359)  + 1-click demo chips per role
├── screens/register_screen.dart(420)
├── screens/dashboard_screen.dart(844) role panels, all mock
└── theme/app_theme.dart        (122)  AppColors mirroring the web palette
test/widget_test.dart                  one smoke test (login screen renders)
```

### Critical divergence: the mobile app is on a **completely different auth system and role model**
- `AuthService` is static, backed by `shared_preferences` keys `kumburu_users` / `kumburu_session`,
  with **4 seeded demo accounts and passwords stored in plaintext** (`passwordHash: 'Password123!'`):
  `farmer@kumburu.lk`, `officer@kumburu.lk`, `buyer@kumburu.lk`, `admin@kumburu.lk`.
  Login is a list scan with `user.passwordHash != password`.
- Mobile `UserRole = { farmer, extensionOfficer, buyer, admin }`
  vs web/backend `{ Farmer, AgriculturalOfficer, Admin, FieldOfficer }`.
  **`buyer` exists only on mobile; `FieldOfficer` exists only on web/backend.**
- Mobile `User` has `id (String)`, `fullName`, `division` — the backend has `int Id`, `Name`, and
  no division at all.
- `dashboard_screen.dart` has Farmer / Officer / **Buyer** / Admin panels, all with mock metrics;
  the Buyer panel has `_ListingTile`s with "Place Offer" buttons that show a
  "procurement module" snackbar. The Admin panel lists locally-stored users and can "Reset Seeds".

**Implication for planning:** the Flutter app is a UI prototype that was *never* migrated when the
web app moved from mock auth to the real JWT API. Any mobile feature work should start with
"add `dio`/`http`, port `tokenStorage` + `authService` + the refresh-interceptor pattern from the
web app, align the role enum, delete the seed users."

---

## 6. Database

There are **two conflicting sources of truth**.

### (a) EF Core migrations — what actually exists in the Neon DB
`Fields`, `Users`, `RefreshTokens`. Int identity PKs. `Users.Role` is an **int** (enum ordinal).
Unique indexes on `Users.Email` and `RefreshTokens.Token`. **No foreign keys at all**
(`RefreshTokens.UserId` is an unconstrained int).

### (b) `database/schema.sql` — a hand-written, richer, *incompatible* design
- `roles` table (`farmer`, `extension_officer`, `buyer`, `admin`) seeded with display names.
- `divisions` table with Sri Lankan agrarian divisions seeded (Medirigiriya/Polonnaruwa,
  Nuwaragam Palatha & Tambuttegama/Anuradhapura, Ampara Central, Kurunegala West, Colombo HQ).
- `users` with **UUID** PKs, `full_name`, `role_id` FK, `division_id` FK, `division_custom`,
  `is_active`, and an `updated_at` trigger.
- `refresh_tokens` with UUID PK, real FK cascade, `revoked_at`, `replaced_by_token`.
- Seeds the same 4 demo accounts as the Flutter app (the hashes are placeholder garbage, not
  valid BCrypt for `Password123!`).

**This file is NOT applied by the backend and does not match the EF model.** Decide early:
either delete `schema.sql` and evolve via EF migrations, or re-model the entities (UUIDs, roles
table, divisions) and regenerate migrations. Divisions/`is_active`/soft-role-management are real
requirements implied by the sidebar ("Manage Users", officer "Assigned Division") — so the
schema.sql design is arguably the better target.

---

## 7. Cross-cutting inconsistencies (read this before designing anything)

| # | Issue | Where |
|---|---|---|
| 1 | **Role model differs 3 ways**: backend/web `{Farmer, AgriculturalOfficer, Admin, FieldOfficer}`; mobile `{farmer, extensionOfficer, buyer, admin}`; schema.sql `{farmer, extension_officer, buyer, admin}` | everywhere |
| 2 | **Buyer/Miller role** is in the landing page, mobile app and schema.sql, but **absent from the backend enum and the web register form** — yet "harvest & market" is Component C | product-level |
| 3 | Mobile app uses local mock auth; web uses real JWT | `auth_service.dart` |
| 4 | `schema.sql` (UUID, roles table, divisions) vs EF migrations (int, enum, no divisions) | `database/` |
| 5 | DB password + placeholder JWT key committed to git | `appsettings.json` |
| 6 | No role enforcement on any route or endpoint (`allowedRoles` unused, no `[Authorize(Roles)]`) | web + API |
| 7 | API base URL hard-coded in web; CORS hard-coded to `:5173` | `axiosInstance.ts`, `Program.cs` |
| 8 | `Field` entity + table exist but no controller, service, DTO, or UI touches them | backend |
| 9 | The activity forms collect data that is **never sent anywhere** | `ActivityForm.tsx` |
| 10 | Fertilizer "knowledge base" is a client-side TS array, not a DB table | `fertilizerRules.ts` |
| 11 | Two CSS design systems in the web app | `global.css` vs `ActivityDashboard.css` |
| 12 | `/weatherforecast` template endpoint still shipped | `Program.cs` |
| 13 | Zero backend/web tests; one Flutter smoke test | repo-wide |
| 14 | No CI, no Dockerfile, no `.env.example`, no API client codegen | repo-wide |
| 15 | `RefreshToken` rows accumulate forever (no cleanup, no FK cascade) | `AuthService` |

---

## 8. Git state

- Current branch: **`feature/field-cultivation`** (identical to `origin/develop` at `7815243`).
- Uncommitted: `paddywise_mobile/analysis_options.yaml` (adds analyzer excludes) and `pubspec.lock`.
- `main` is **far behind** develop (missing all of: tokenStorage, roleRoutes, Sidebar,
  the whole Flutter app rewrite, the crop-resource feature).
- Branches: `main`, `develop`, `feature/field-cultivation`, `CropActivity`, `feature/userValidation`,
  `login`, `mobile`, **`PestDiseaseMonitoring`** and **`Reporting`** — the last two are branched from
  the *initial commit* and contain **no work at all** (they're empty placeholders for future components).
- ⚠ **`origin/CropActivity` is one commit ahead of your branch**: `5a85e98 "admin side user management ui"`
  adds `pages/UserManagementPage.tsx` (194 lines), `styles/UserManagement.css`, a Sidebar rewrite
  and an App.tsx route. **Merge or rebase this before touching Sidebar/App.tsx or you'll conflict.**
- Team: multiple contributors (repo origin `github.com/viruldesilva/paddywise-ai`), PR-per-branch
  into `develop`. Commit messages are informal.

---

## 9. How to run it

```bash
# Backend  → http://localhost:5164  (Swagger at /swagger)
cd paddywise-backend && dotnet restore && dotnet ef database update && dotnet run

# Web      → http://localhost:5173
cd paddywise-web && npm install && npm run dev

# Mobile
cd paddywise_mobile && flutter pub get && flutter run
```
Web talks to the backend only if the backend is on port **5164** and the web dev server is on
**5173** (both hard-coded). Mobile talks to nothing.

---

## 10. Build-status scoreboard

| Capability | Backend | Web | Mobile | DB |
|---|---|---|---|---|
| Register / Login / JWT / refresh rotation | ✅ done | ✅ done | ❌ local mock | ✅ tables |
| `GET /me` session verify | ✅ | ✅ | ❌ | – |
| Role-based **routing** | – | 🟡 redirect only, no enforcement | 🟡 panel switch | – |
| Role-based **authorization** | ❌ none | ❌ none | ❌ | – |
| Landing / marketing site | – | ✅ complete | – | – |
| Dashboards | – | 🟡 static mock, 4 roles | 🟡 static mock, 4 roles | – |
| Admin user management | ❌ | 🟡 UI only, on unmerged `CropActivity` branch | 🟡 lists local users | ❌ |
| **A: Fields & cultivation cycles** | 🟡 `Field` entity + table only | ❌ | ❌ | 🟡 `Fields` table |
| **A: Activity logging** (irrigation/fertilizer/pesticide) | ❌ | 🟡 forms exist, submit to nothing | ❌ | ❌ |
| **A: Fertilizer rule validation** | ❌ | 🟡 client-side mock "agent" | ❌ | ❌ |
| **B: Pest/disease report + image upload** | ❌ | ❌ (alert stub) | ❌ | ❌ |
| **B: AI diagnosis agent** | ❌ | ❌ | ❌ | ❌ |
| **B: Officer approval workflow** | ❌ | ❌ (alert stub) | ❌ | ❌ |
| **C: Harvest recording / buyer offers** | ❌ | ❌ | 🟡 mock buyer panel | ❌ |
| **D: Weather integration** | ❌ | ❌ | ❌ | ❌ |
| **D: Resource/subsidy tracking** | ❌ | ❌ | ❌ | ❌ |
| Notifications (SMS/push) | ❌ | ❌ | ❌ | ❌ |
| Divisions / geography | ❌ | ❌ | 🟡 free-text field | 🟡 only in unused schema.sql |
| Knowledge base / pesticide allow-list | ❌ | 🟡 19 hard-coded rules | ❌ | ❌ |
| Audit logs | ❌ | ❌ | ❌ | ❌ |
| Tests / CI / Docker | ❌ | ❌ | 🟡 1 smoke test | – |

Legend: ✅ working · 🟡 partial/mock/UI-only · ❌ nothing.

**One-line summary: the authentication layer is production-shaped and the rest of the product is a
clickable mock.** There is currently **exactly one** non-auth backend endpoint (the template
weather one), and **zero** business-domain API endpoints.

---

## 11. Conventions a new feature should follow

**Backend**
- `Entities/<Component>/X.cs`, `DTOs/<Component>/XDto.cs`, `Services/<Component>/{IXService,XService}.cs`,
  `Controllers/<Component>/XController.cs` with `[ApiController] [Route("api/<plural>")]`.
- Register the service in `Program.cs` as `AddScoped`, add `DbSet<X>` to `ApplicationDbContext`,
  configure in `OnModelCreating`, then `dotnet ef migrations add <Name>`.
- Controller pattern in use: try/catch `InvalidOperationException` → `BadRequest(new { message })`,
  null result → `Unauthorized/NotFound(new { message })`. Keep `{ message }` shape — the web's
  `extractApiErrorMessage` depends on it.
- Use `[Authorize]`; **add `Roles = "..."` for anything role-scoped** (nothing does today).
- Get the caller's id from `ClaimTypes.NameIdentifier` (it's already in the token).

**Web**
- New feature → `src/features/<feature>/{pages,components,data,services}/`.
- All HTTP through `axiosInstance` (never bare axios) so the token + refresh queue apply.
- Types in `types/` or colocated; avoid the `useState<any>` pattern used in `ActivityForm`.
- Route registered in `App.tsx` inside `<ProtectedRoute allowedRoles={[...]}>` — start using that prop.
- Add the sidebar link in `Sidebar.tsx` (replace the relevant `#anchor`).
- Style with the `global.css` custom properties, not the crop-resource variable set.

**Mobile** — needs the HTTP + real-auth foundation before feature work is meaningful.

---

## 12. Highest-leverage next steps (suggested, in order)

1. **Security**: rotate the leaked Neon password, move secrets to user-secrets/env, set a real JWT key,
   stop tracking `appsettings.json`.
2. **Decide the data model**: reconcile `schema.sql` vs EF (roles table + divisions + UUIDs, or not),
   settle the canonical role list **including whether Buyer exists**, and fix `RefreshToken`'s FK.
3. **Merge `origin/CropActivity`** into your branch before touching `App.tsx`/`Sidebar.tsx`.
4. **Ship Component A end-to-end** as the first real vertical slice — it's the natural next
   feature given `Field` already exists and the branch you're on is `feature/field-cultivation`:
   `Field` CRUD + `CultivationCycle` + `Activity` entities → API with role auth → wire
   `ActivityForm` to a real `POST /api/activities` → move `fertilizerRules` into a DB table and
   make `FertilizerValidationFlow` call a real `POST /api/fertilizer/validate`. That single slice
   turns three mocks into a working feature and establishes the pattern for B, C, D.
5. **Lock down authorization** (`allowedRoles` on routes, `[Authorize(Roles=...)]` on controllers)
   before adding more screens.
6. Then: Component B (upload + diagnosis + officer approval queue) — it's the project's headline
   claim and currently 0% built.
