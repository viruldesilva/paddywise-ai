# CLAUDE.md — PaddyWise-AI ("Kumburu")

## 1. Product & ownership

PaddyWise-AI (UI name **Kumburu**) is an agentic AI decision-support platform for Sri Lankan
paddy farmers, connecting farmers and agricultural/field officers on one system. It has four
business components, one per team member, each with its own AI agent:
1. **Field & Cultivation Management** — fields, cultivation cycles, growth stages —
   *Cultivation Planning Agent* (the workflow coordinator).
2. **Crop Activity & Resource Management** — irrigation/fertilizer/pesticide activity logging,
   fertilizer rules — *Resource Analysis & Action Agent*.
3. **Pest & Disease Monitoring** — problem reports, photo upload, diagnosis —
   *Pest & Disease Diagnosis Agent*.
4. **Reporting, Dashboards & Approval** — officer/admin dashboards, approval management,
   audit — *Scheduling, Validation & Approval Agent* (scheduling/prioritisation; validation
   of agent output is deterministic backend code, not an LLM).

**Client split:** the **web app is for Admin and AgriculturalOfficer only**. The web login
portals (`LoginForm.tsx`) reject every other role. **Farmers use only the Flutter mobile app.**
Do not add farmer-facing screens to the web. Farmer flows go in `paddywise_mobile`, and
officer review and oversight go in the web app.

**Nuran owns Component 1 only.** Do not create, edit, or delete files belonging to other
components: `paddywise-web/src/features/crop-resource/**`, and any backend folder
`Entities|DTOs|Services|Controllers|Agents/<Component>/`, web `src/features/<name>/` or mobile
`lib/features/<name>/` for components 2–4 (CropResource, PestDisease, Reporting/
reporting_approval). Component 1's folders are `FieldCultivation/` (backend),
`src/features/field-cultivation/` (web) and `lib/features/field_cultivation/` (mobile).
Shared files may be edited only for the minimal lines a task explicitly names:
- **Backend:** `Program.cs`, `ApplicationDbContext.cs`
- **Web:** `App.tsx`, `Sidebar.tsx`, `DashboardPage.tsx`
- **Mobile:** `app_router.dart`, `role_menu_config.dart`, `auth_service.dart`, `dashboard_screen.dart`

If a task seems to need more, stop and ask.

Other components import from Component 1's `fieldApi.ts`:
- crop-resource uses `getCycleById` and `fieldApi.getMyCycles`.
- pest-disease uses `getMyCycles`.

Do not remove or rename those functions.

## 2. Running the apps

The backend throws on startup unless three secrets are set (once per machine, from
`paddywise-backend/`): `Jwt:Key` (shared team secret, ≥32 chars),
`ConnectionStrings:DefaultConnection` (shared Neon Postgres string), `Gemini:ApiKey`
(your own, from [Google AI Studio](https://aistudio.google.com/apikey)):

```bash
dotnet user-secrets set "Jwt:Key" "<shared-secret>"
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<shared-connection-string>"
dotnet user-secrets set "Gemini:ApiKey" "<your-own-key>"
```

```bash
# Backend → http://localhost:5164   (Swagger at /swagger; https profile also on 7188)
cd paddywise-backend && dotnet restore && dotnet ef database update && dotnet run

# Web → http://localhost:5173
cd paddywise-web && npm install && npm run dev

# Mobile
cd paddywise_mobile && flutter pub get && flutter run
```

The web app's API base URL is hard-coded to `http://localhost:5164/api` in
`src/api/axiosInstance.ts`. The CORS policy `AllowReactApp` in `Program.cs` allows any
`http(s)://localhost:<port>` origin; `127.0.0.1` is not allowed.

The mobile app calls the real backend. Its base URL comes from `AuthService.apiBaseUrl` in
`lib/services/auth_service.dart`:
- `http://10.0.2.2:5164/api` on the Android emulator
- `http://localhost:5164/api` elsewhere
- it can be overridden at runtime with `setApiBaseUrl`

Run the backend with the **`http`** launch profile for mobile. The `https` profile redirects
the emulator to 7188 and the dev certificate.

No test project exists for the backend or the web app. `npm run lint` (eslint) is available in
`paddywise-web`. Mobile has `flutter analyze` and `flutter test`, which runs everything in
`test/`, including `role_navigation_test.dart` (the drawer menus) and
`field_cultivation_models_test.dart` (Component 1's JSON parsing).

## 3. Backend conventions

Folder layout, per business component:

- `Entities/<Component>/X.cs`
- `DTOs/<Component>/XDto.cs`
- `Services/<Component>/{IXService,XService}.cs`
- `Controllers/<Component>/XController.cs` with `[ApiController]` and `[Route("api/<plural>")]`

`Shared/` is the folder for cross-cutting types (User, UserRole, RefreshToken, auth).
Component 1 lives under `FieldCultivation/`.

**Controller error pattern in use:** try/catch `InvalidOperationException` →
`BadRequest(new { message })`; a null result → `Unauthorized(new { message })` /
`NotFound(new { message })`. Keep the `{ message }` shape — the web's `extractApiErrorMessage`
depends on it.

**Authorization:** use `[Authorize]`, and **add `Roles = "..."` for anything role-scoped**
(nothing does today). Get the caller's id from `ClaimTypes.NameIdentifier` — it is already in
the access token, alongside `Name`, `Email` and `Role`.

**Registration + migrations:** register a service in `Program.cs` as `AddScoped`, add
`DbSet<X>` to `ApplicationDbContext`, configure it in `OnModelCreating`, then:

```bash
dotnet ef migrations add <Name>
```

## 4. Web conventions

The web app is for officers and admins; see §1. Component 1's web pages are:
- **`DivisionFieldsPage`** (`/officer/fields`): pick a division and browse its fields.
- **`FieldDetailPage`** (`/fields/:id`): a field and its cycles, read-only.
- **`CycleDetailPage`** (`/cycles/:id`): a cycle's timeline and plans. Only an
  AgriculturalOfficer can log a stage here, because the backend refuses stage logs from Admin.
- **`PlanApprovalPage`** (`/plans/pending`): the officer's approval queue.

All four are guarded by `allowedRoles` (AgriculturalOfficer/Admin, or AgriculturalOfficer for
the approval queue). Creating fields, starting cycles and requesting plans happen only on
mobile.

- New feature → `src/features/<name>/{pages,components,services,types}/`.
- **All HTTP goes through `src/api/axiosInstance.ts`** (never bare `axios`) so the bearer token,
  the 401 handler and the single-flight refresh queue apply.
- Reuse `extractApiErrorMessage(err, fallback)` from `src/services/authService.ts` for every API
  error — it unwraps `{ message }`, `ValidationProblemDetails.errors`, and `title`.
- Register routes in `App.tsx` inside `<ProtectedRoute allowedRoles={[...]}>`. Always pass the
  prop. The `/dashboard/*` and `/activities` routes still omit it, which is an open
  authorization gap.
- Type your state properly; avoid the `useState<any>` pattern.
- Style with the `styles/global.css` custom properties — **never** the crop-resource variable set
  (`--primary-dark`, `--text-muted`, `glass-panel`):

  `--cream #F6F1E3`, `--cream-deep #EDE5CF`, `--ink #212D1E`, `--ink-soft #4B5645`,
  `--forest #22392A`, `--forest-deep #182A1E`, `--shoot #7FA66C`, `--shoot-light #B4CB9C`,
  `--gold #E1A63B`, `--gold-deep #C48A28`, `--clay #A6693F`, `--line rgba(33,45,30,.14)`

- Fonts: **Fraunces** (serif) for headings, **Work Sans** for body.

## 5. Mobile conventions (`paddywise_mobile`)

- **Feature folders:** a new feature goes in `lib/features/<name>/{models,services,widgets,screens}/`.
  Component 1's is `lib/features/field_cultivation/`.
- **HTTP:** all authenticated calls go through `lib/core/api/api_client.dart` (`ApiClient`), not
  bare `http`.
  - It adds the bearer token.
  - It unwraps `{ message }`, `errors` and `title` into `ApiException`.
  - On a 401 it calls `AuthService.refreshSession()` (single-flight `/auth/refresh`) and retries
    once. If the refresh fails, it logs out and returns to `/login`.
  - Show errors with `e.toString()`.
- **Models:** plain classes with `fromJson`. Enums carry the backend member name in a `wire`
  field. `DateOnly` values travel as `YYYY-MM-DD`; use `parseDateOnly`/`toDateOnly` in
  `field_cultivation/models/cycle_models.dart`.
- **Routes:**
  - Register screens in `lib/core/router/app_router.dart`.
  - List screens and detail screens go inside the `ShellRoute`, which provides the app bar and
    drawer. Drill-down uses `context.push`, and a detail screen shows an `FcBackLink`.
  - Full-screen forms are top-level `GoRoute`s declared **before** the `ShellRoute`, so
    `/fields/new` is not read as `/fields/:id`. A form pops with its result.
- **Drawer menu:** entries live in `lib/core/widgets/role_menu_config.dart`, keyed by the
  backend role string. Update `test/role_navigation_test.dart` when you change them.
- **Styling:** use `AppColors` from `lib/theme/app_theme.dart`, which mirrors the web tokens.
  Do not add colors. Reuse `field_cultivation/widgets/fc_common.dart` for headers, cards, empty
  and error states and banners.
- **Component 1 screens (Farmer only):**
  - **Fields:** My Fields `/fields`, add/edit field `/fields/new` and `/fields/:id/edit`, field
    detail `/fields/:id`.
  - **Cycles:** start a cycle `/fields/:id/start-cycle`, My Cultivations `/cycles`, cycle detail
    `/cycles/:id` (timeline, log a stage, abandon).
  - **Plans:** request a plan `/cycles/:id/plans/new`, plan detail `/plans/:id`.
  - These screens deliberately have no offline demo fallback.

## 6. Roles

The canonical role list is exactly:

```
Farmer, AgriculturalOfficer, Admin, FieldOfficer
```

`UserRole` is an enum **stored in the database as an int** (ordinal: Farmer=0,
AgriculturalOfficer=1, Admin=2, FieldOfficer=3). The API accepts the string name
(case-insensitive) or the numeric value. There is no Buyer role on the backend or the web app,
despite what the Flutter app and `database/schema.sql` contain.

## 7. Rules for every task

- **Always build before finishing.** Report the result.
  - Backend: `dotnet build` in `paddywise-backend`.
  - Web: `npm run build` in `paddywise-web`. As of 2026-09-28 it fails on errors in other
    components' files (crop-resource, pest-disease, `UserManagementPage.tsx`). Check that no
    error comes from your files, and run `npx eslint` on them.
  - Mobile: `flutter analyze` and `flutter test` in `paddywise_mobile`.
- **Never edit `appsettings.json` to add secrets.** Use `dotnet user-secrets` or environment
  variables. The file is tracked by git.
- **Never delete or rewrite an existing migration.** Always add a new one.
- **Do not create new CSS design tokens.** Use the existing `global.css` variables listed above.
- **Keep the `{ message }` error shape** on every 4xx response.
- **Ask before touching `App.tsx`, `Sidebar.tsx`, `Program.cs`, or `ApplicationDbContext.cs`**
  beyond the single minimal line needed to register a route, service, or DbSet.

## 8. Agents

Agent code lives in `Agents/<Component>/`, with cross-cutting types in
`Agents/Shared/`: `IAgent<TInput,TOutput>` (Name + RunAsync returning
`AgentResult<T>` with ToolCalls and Duration), `AgentContext`, `ToolCallRecord`,
`DelegatedTask`/`DelegatedTaskResult`, `AgentNames` (the DI keys), `ILlmClient`
+ `GeminiLlmClient`, and `LlmException`. Components 2-4 replace the stubs in
`StubAgents.cs` with real `IAgent` implementations registered under the same
`AgentNames` key.

Rules: an agent's tools are read-only and scoped to the run's own records; no
LLM output reaches a human or the database without passing a deterministic
validator (see `Services/FieldCultivation/CultivationPlanValidator.cs` for the
pattern); every run writes an `AgentRunLog` row whether it succeeds or fails;
user text goes in the user prompt inside a delimited block, never in the system
prompt. `ILlmClient` is the only place the provider is known - swapping vendors
is one class.

A plan request runs two validation passes.
1. `CultivationPlanValidator` (Component 1) runs inside `CultivationPlanService`.
2. `PlansController.RequestPlan` then calls Component 4's `IValidationAgentService`, a
   shape and dosage check. It runs only on a plan that pass 1 already passed, so it can fail
   such a plan but never promote one pass 1 failed.

The POST returns the plan as stored after both passes.

Component 1's agent in full detail (tools, validation rules, plan/approval states) is
documented in `docs/cultivation-planning-agent.md`.
