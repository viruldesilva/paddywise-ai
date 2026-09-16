# PaddyWise-AI ("Kumburu") — Full Project Context Document

> Generated 2026-09-16 from a full read of the repository at branch `feature/field-cultivation`
> (16 commits ahead of `origin/develop`).
> Purpose: paste this into an LLM chat as complete context before planning a new feature.

---

## 1. What this project is

**PaddyWise-AI** (product name in the UI: **Kumburu**) is an academic/capstone-style
**agentic AI decision-support platform for Sri Lankan paddy (rice) farmers**, connecting
farmers and agricultural/field officers on one system.

The pitch (from the landing page copy, which is still the clearest statement of product intent):

> "Kumburu connects farmers, extension officers and buyers on one system — so a pest report
> gets answered in hours, not weeks, and the harvest finds a fair price."

The intended **5-step core workflow** (from `WorkflowSection.tsx`):
`01 Report → 02 Analyze (AI agent) → 03 Recommend (dosage-checked) → 04 Approve (officer sign-off) → 05 Act (farmer gets plan)`

### The four business components, one per team member

Each component owns one AI agent. This is the structure the backend folders, the DI keys in
`AgentNames` and the web `features/` directories are all organised around.

| # | Component | Agent | Built? |
|---|---|---|---|
| **1** | **Field & Cultivation Management** — fields, cultivation cycles, growth stages, cultivation plans | **Cultivation Planning Agent** (the workflow coordinator) | **Yes — end to end** |
| **2** | Crop Activity & Resource Management — irrigation/fertilizer/pesticide logging, fertilizer rules | Resource Analysis & Action Agent | No — stub agent, client-side mock UI |
| **3** | Pest & Disease Monitoring — problem reports, photo upload, diagnosis | Pest & Disease Diagnosis Agent | No — stub agent, no UI beyond `alert()` |
| **4** | Reporting, Dashboards & Approval — officer/admin dashboards, approval management, audit | Scheduling, Validation & Approval Agent | No — stub agent; the *plan* approval queue in §4 belongs to component 1 |

**Reality check: component 1 is the only one that exists in code.** Components 2–4 exist as
three `IAgent` stubs in `Agents/Shared/StubAgents.cs` that return a "to be implemented" note,
plus one client-side mock feature module (`features/crop-resource/`). The landing page's older
four-feature pitch (cultivation / pest advisory / **harvest & market** / **weather**) is
marketing copy — harvest, market, buyers and weather have no code anywhere in the repo.

---

## 2. Repository layout

```
paddywise-ai/
├── README.md                  # team setup guide (prerequisites, secrets, run commands)
├── CLAUDE.md                  # working rules: ownership boundaries, conventions, guardrails
├── PROJECT_CONTEXT.md         # this file
├── .gitattributes
├── package-lock.json          # empty stub, no root package.json (vestigial)
├── database/
│   └── schema.sql             # HAND-WRITTEN Postgres schema — DIVERGES from the EF model (see §6)
├── docs/
│   └── cultivation-planning-agent.md   # component 1's agent: tools, validation rules, states
├── paddywise-backend/         # ASP.NET Core 8 Web API  (project name: PaddyWise.Api)
├── paddywise-web/             # React 19 + TypeScript + Vite SPA
└── paddywise_mobile/          # Flutter app (Dart 3.9)
```

Three independent tech stacks, no shared contract/codegen, no monorepo tooling, no CI, no Docker,
no tests beyond one Flutter smoke test.

---

## 3. Backend — `paddywise-backend` (ASP.NET Core 8)

**Project**: `PaddyWise.Api.csproj`, `net8.0`, nullable + implicit usings enabled,
`UserSecretsId` set.

### Packages
| Package | Version | Purpose |
|---|---|---|
| `Microsoft.AspNetCore.Authentication.JwtBearer` | 8.0.10 | JWT bearer auth |
| `Npgsql.EntityFrameworkCore.PostgreSQL` | 8.0.10 | Postgres provider |
| `Microsoft.EntityFrameworkCore.Design` | 8.0.10 | migrations |
| `Microsoft.AspNetCore.OpenApi` | 8.0.22 | OpenAPI metadata |
| `BCrypt.Net-Next` | 4.2.0 | password hashing |
| `Swashbuckle.AspNetCore` | 6.6.2 | Swagger UI with Bearer scheme |

No LLM SDK: the Gemini client is hand-written over `HttpClient` (§7).

### Folder layout (the convention every new feature follows)

```
Agents/Shared/            IAgent, AgentContext, ToolCallRecord, DelegatedTask, AgentNames,
                          ILlmClient, GeminiLlmClient, LlmToolDefinition, LlmException, StubAgents
Agents/FieldCultivation/  CultivationPlanningAgent, CultivationPlanModels
Controllers/<Component>/  [ApiController] [Route("api/<plural>")]
DTOs/<Component>/         XDto.cs, with DataAnnotations
Entities/<Component>/     X.cs
Services/<Component>/     IXService.cs + XService.cs
Data/ApplicationDbContext.cs
Migrations/
```

`Shared/` holds cross-cutting types (User, UserRole, RefreshToken, auth). Component 1 lives
under `FieldCultivation/`. Components 2–4 create `CropResource/`, `PestDisease/`, `Reporting/`.

### `Program.cs` — what is wired
- Controllers + Swagger (Bearer security definition; token entered without the `Bearer ` prefix).
- **Startup guard**: throws if `Jwt:Key` is missing or under 32 characters, naming the
  `dotnet user-secrets` command to fix it.
- Scoped services: `IAuthService`, `IFieldService`, `ICycleService`, `ICultivationPlanService`.
- `IHttpClientFactory` client named `"Gemini"` with a 60-second timeout; `ILlmClient → GeminiLlmClient`.
- `IAgent<PlanAgentInput, CultivationPlanOutput> → CultivationPlanningAgent` — a unique closed
  generic, so it needs no DI key.
- Three **keyed** registrations of `IAgent<DelegatedTask, DelegatedTaskResult>` under the
  `AgentNames` constants, all pointing at stubs. Components 2–4 replace the implementation and
  keep the key.
- `ApplicationDbContext` on Npgsql, connection string `ConnectionStrings:DefaultConnection`.
- JWT bearer validation: issuer, audience, lifetime, signing key (HMAC-SHA256).
- `AddAuthorization()` with no custom policies — role checks are `[Authorize(Roles = "...")]`
  attributes on the controllers.
- CORS policy `AllowReactApp` → **hard-coded to `http://localhost:5173`** only.
- Pipeline: Swagger (Dev only) → HttpsRedirection → CORS → Authentication → Authorization → MapControllers.
- The `/weatherforecast` template endpoint is **gone** from `Program.cs`. `PaddyWise.Api.http`
  still calls it and is now dead scratch.

### Entities

**`Entities/Shared/`**
```csharp
class User         { int Id; string Name, Email, PasswordHash; UserRole Role; string? Phone;
                     DateTime CreatedAt, UpdatedAt; }
enum UserRole      { Farmer=0, AgriculturalOfficer=1, Admin=2, FieldOfficer=3 }   // int in DB
class RefreshToken { int Id; int UserId; string Token; DateTime ExpiresAt; bool IsRevoked;
                     DateTime CreatedAt; }
// RefreshToken.UserId is still a plain int — NO navigation property, NO FK constraint.
```

**`Entities/FieldCultivation/` — component 1**
```csharp
class Division        { int Id; string Name, District, Province; }
class Field           { int Id; string Name; decimal Area /*acres*/; string SoilType, IrrigationType;
                        double? Latitude, Longitude; int FarmerId, DivisionId; bool IsActive;
                        DateTime CreatedAt, UpdatedAt; User Farmer; Division Division; }
class Variety         { int Id; string Name; int DurationDays; string AgeGroup; string? Notes; }
class CultivationCycle{ int Id, FieldId, VarietyId; Season Season; int Year; CultivationMethod Method;
                        DateOnly SowingDate, ExpectedHarvestDate; DateOnly? ActualHarvestDate;
                        GrowthStage CurrentStage; CycleStatus Status; string? Notes;
                        DateTime CreatedAt, UpdatedAt; Field Field; Variety Variety; }
class GrowthStageLog  { int Id, CultivationCycleId; GrowthStage Stage; DateOnly ObservedOn;
                        string? Notes; int LoggedByUserId; DateTime CreatedAt; }
class CultivationPlan { int Id, CultivationCycleId, RequestedByUserId; string Objective;
                        string PlanJson /*jsonb*/; PlanStatus Status;
                        string? ValidationErrorsJson /*jsonb*/; int? OfficerId;
                        string? OfficerComment; DateTime? ReviewedAt, CreatedAt, UpdatedAt; }
class AgentRunLog     { int Id; int? CultivationPlanId; string AgentName, CorrelationId;
                        string InputJson /*jsonb*/, ToolCallsJson /*jsonb*/, RawOutput;
                        bool Success; string? Error; int DurationMs; DateTime CreatedAt; }

enum Season            { Yala=0, Maha=1 }
enum CultivationMethod { Broadcasting=0, Transplanting=1, DirectSeeding=2 }
enum CycleStatus       { Planned=0, Active=1, Harvested=2, Abandoned=3 }
enum GrowthStage       { Nursery=0, Tillering=1, PanicleInitiation=2, Flowering=3,
                         GrainFilling=4, Harvest=5 }
enum PlanStatus        { Draft=0, ValidationFailed=1, PendingOfficerApproval=2, Approved=3,
                         Rejected=4, RevisionRequested=5 }
```

`ExpectedHarvestDate` is **stored, not computed**: `CycleService` sets it to
`SowingDate + Variety.DurationDays` at creation, so editing a variety's duration later cannot
silently move an existing cycle's plan.

### `ApplicationDbContext` — model configuration

DbSets: `Fields`, `Divisions`, `Users`, `RefreshTokens`, `Varieties`, `CultivationCycles`,
`GrowthStageLogs`, `CultivationPlans`, `AgentRunLogs`.

`OnModelCreating` configures:
- Unique indexes on `User.Email` and `RefreshToken.Token`.
- `Field`: `Area` precision (10,2), `IsActive` default true, index on `FarmerId`, required FKs to
  `User` and `Division` with `Restrict`, and a check constraint `CK_Fields_Area_Positive` (`"Area" > 0`).
- `Variety`: unique index on `Name`.
- `CultivationCycle`: **unique index on `(FieldId, Season, Year)`** — one cycle per field per
  season per year — plus an index on `Status`, and `Restrict` FKs to Field and Variety.
- `GrowthStageLog`: Cascade from the cycle, `Restrict` on the user so deleting a user cannot
  erase the observation trail.
- `CultivationPlan`: `PlanJson` / `ValidationErrorsJson` as **jsonb**, indexes on
  `CultivationCycleId` and `Status`, Cascade from the cycle, `Restrict` on both user FKs.
- `AgentRunLog`: `InputJson` / `ToolCallsJson` as jsonb, indexes on `CultivationPlanId` and
  `CorrelationId`, and **`SetNull`** on the plan FK so deleting a plan cannot erase the record
  that an agent ran.
- **Seed data**: 5 `Division` rows (Medirigiriya/Polonnaruwa, Nuwaragam Palatha & Tambuttegama/
  Anuradhapura, Ampara Central, Kurunegala West) and 8 `Variety` rows (Bg 300, Bg 352, Bg 360,
  At 362, Bg 94-1, Bg 359, At 307, Bw 367). ⚠ The seeded `DurationDays` / `AgeGroup` values are
  **working values, not verified DOA data** — the entity's own comment says so, and harvest-date
  planning currently rests on them.

### Migrations — five, all applied via `dotnet ef database update`

| Migration | Adds |
|---|---|
| `20260909130307_InitialCreate` | `Fields` |
| `20260910105145_AddUsersAndRefreshTokens` | `Users`, `RefreshTokens` + unique indexes |
| `20260916115824_AddDivisionsAndFieldOwnership` | `Divisions` (+5 seed rows), `Field.FarmerId/DivisionId/IsActive/CreatedAt/UpdatedAt`, the area check constraint |
| `20260916123714_AddCultivationCycles` | `Varieties` (+8 seed rows), `CultivationCycles`, `GrowthStageLogs` |
| `20260916141243_AddCultivationPlansAndAgentLogs` | `CultivationPlans`, `AgentRunLogs` |

Rule in force: **never edit or delete an existing migration — always add a new one.**

### API surface — 22 endpoints

Base: `http://localhost:5164/api` (http profile; the https profile also binds `https://localhost:7188`).
Every 4xx keeps the `{ message }` shape the web's `extractApiErrorMessage` depends on.

| Method | Route | Roles | Notes |
|---|---|---|---|
| POST | `/api/auth/register` | anon | `{name,email,password,role,phone?}` → `AuthResponseDto`; 400 on duplicate email |
| POST | `/api/auth/login` | anon | 401 `{message}` on bad credentials |
| POST | `/api/auth/refresh` | anon | rotates the pair; 401 if missing/revoked/expired |
| GET | `/api/auth/me` | any authenticated | `{name, role}` |
| GET | `/api/divisions` | any authenticated | component 1 |
| GET | `/api/varieties` | any authenticated | component 1 |
| GET | `/api/fields` | **Farmer** | the caller's own active fields |
| GET | `/api/fields/division/{divisionId}` | **AgriculturalOfficer, FieldOfficer, Admin** | every active field in a division |
| GET | `/api/fields/{id}` | any authenticated | a Farmer who is not the owner gets 403 |
| POST | `/api/fields` | **Farmer** | owner comes from the token, not the body |
| PUT | `/api/fields/{id}` | **Farmer** (owner) | full replace of every editable value |
| DELETE | `/api/fields/{id}` | **Farmer** (owner) | **soft** delete → `IsActive = false`, 204 |
| POST | `/api/fields/{fieldId}/start-cultivation` | **Farmer** (owner) | "Start Cultivation Cycle"; declared on `CyclesController` because everything it creates is a cycle |
| GET | `/api/cycles?fieldId=` | any authenticated | a Farmer gets their own; an officer/admin **must** pass `fieldId` or gets 400 |
| GET | `/api/cycles/{id}` | any authenticated | Farmer scoped to their own, else 403 |
| POST | `/api/cycles/{id}/stages` | **Farmer, AgriculturalOfficer, FieldOfficer** | logging `Harvest` also sets the cycle `Harvested` + `ActualHarvestDate` |
| PATCH | `/api/cycles/{id}/status` | **Farmer** (owner) | Planned / Active / Harvested / Abandoned |
| POST | `/api/cycles/{cycleId}/plans` | **Farmer** (owner) | runs the planning agent; **20–40 s**; declared on `PlansController` |
| GET | `/api/cycles/{cycleId}/plans` | any authenticated | every plan for a cycle, newest first; Farmer scoped |
| GET | `/api/plans/{id}` | any authenticated | plan + its agent runs; Farmer scoped |
| GET | `/api/plans/pending?divisionId=` | **AgriculturalOfficer** | the approval queue, newest first |
| POST | `/api/plans/{id}/review` | **AgriculturalOfficer** | Approve / Reject / RequestRevision |

Authorization pattern: `[Authorize(Roles = "...")]` on the action, caller id from
`ClaimTypes.NameIdentifier`, caller role from `ClaimTypes.Role`, then a second **ownership**
check inside the service that throws `UnauthorizedAccessException` → 403. Errors:
`InvalidOperationException` → 400, null → 404, `UnauthorizedAccessException` → 403.

### Business rules enforced in `CycleService`
- Field must exist **and** be active, and belong to the caller.
- A field may hold only one `Planned` or `Active` cycle at a time.
- Sowing date: no more than **30 days back**, no more than **365 days forward**.
- No duplicate `(field, season, year)`.
- `ExpectedHarvestDate = SowingDate + Variety.DurationDays`, frozen at creation.
- `CycleResponseDto` carries both `currentStage` (what the farmer reported) and
  `expectedStageToday` (what the timeline says) — the two differing is the signal the agent acts on.

### `StageTimelineCalculator` (component 1, pure arithmetic, no DB)
Turns sowing date + duration into six contiguous inclusive stage windows at cumulative
fractions of the season: Nursery 0.00, Tillering 0.15, PanicleInitiation 0.40, Flowering 0.55,
GrainFilling 0.70, Harvest 0.95. Also `ExpectedStageOn(date, …)`. Shared by `CycleService`
and the agent's `get_stage_timeline` tool, so the API and the model see the same calendar.

### Auth behaviour (`Services/Shared/AuthService.cs`) — unchanged, still the most finished layer
- **Register**: rejects duplicate email; `Enum.TryParse<UserRole>(role, ignoreCase: true)`, so
  the client sends the member name or the numeric value. BCrypt hash. Auto-login on register.
- **Login**: email lookup with a case-sensitive `==` (no `.ToLower()`), BCrypt verify.
- **Refresh**: rejects missing/revoked/expired, then **rotates** (old marked revoked, new pair
  issued). Old rows are never deleted or pruned.
- **Access token claims**: `NameIdentifier`, `Name`, `Email`, `Role`. 20 minutes.
- **Refresh token**: 64 random bytes, Base64, 7 days.

### Configuration
`appsettings.json` is tracked and holds **no secrets** — `Jwt:Key` and
`ConnectionStrings:DefaultConnection` are deliberately empty strings that document the shape.
Supply them from `dotnet user-secrets` locally or `Jwt__Key` / `ConnectionStrings__DefaultConnection`
in deployment. `Gemini:ApiKey` is per-developer and follows the same route
(`Gemini__ApiKey`). `paddywise-backend/README.md` documents all of it.
Non-secret JWT settings (`Issuer`, `Audience`, `AccessTokenExpiryMinutes`,
`RefreshTokenExpiryDays`) stay in `appsettings.json`.

### Not present in the backend
No controllers/services for: activities, fertilizer rules, pests/diseases, image upload,
weather, harvest, buyers/offers, notifications, knowledge base, admin user management,
audit/reporting endpoints. No global exception handler, no rate limiting, no health checks,
no request logging beyond defaults, **no tests**.

---

## 4. Web frontend — `paddywise-web` (React 19 + TS + Vite)

### Stack
React `19.2`, react-dom, `react-router-dom` 7, `axios` 1.20, `lucide-react`, TypeScript ~6.0,
Vite 8, ESLint 10. **No UI framework, no Tailwind** — hand-written CSS with custom properties.
No state library (Context only). No test runner.

Scripts: `npm run dev` (Vite, 5173), `build` (`tsc -b && vite build`), `lint`, `preview`.

### Directory map
```
src/
├── main.tsx, App.tsx
├── api/axiosInstance.ts          # axios + JWT interceptors  ← all HTTP goes through this
├── services/authService.ts       # AuthService + extractApiErrorMessage()
├── services/tokenStorage.ts      # localStorage wrapper behind ITokenStorage
├── context/AuthContext.tsx       # AuthProvider + useAuth
├── hooks/{useAuth,useReveal}.ts
├── types/auth.ts                 # UserRole union + DTO types
├── utils/roleRoutes.ts           # role → dashboard path map
├── components/                   # ProtectedRoute, Sidebar + 8 landing-page sections
├── pages/                        # Home, Login, Register, DashboardPage, UserManagementPage
├── features/field-cultivation/   # COMPONENT 1 — real, wired to the API
├── features/crop-resource/       # COMPONENT 2 — client-side mock
└── styles/                       # global.css, Auth.css, Dashboard.css, Sidebar.css, UserManagement.css
```

Two conventions coexist: page-based for auth + landing, feature-sliced for component work.
**New features follow `features/<name>/{pages,components,services,types,utils,styles}/`.**

### Routing (`App.tsx`)
| Path | Guard | Component |
|---|---|---|
| `/` | public | `Home` (landing page) |
| `/login`, `/register` | public | `LoginPage`, `RegisterPage` |
| `/dashboard` | `ProtectedRoute` | `RoleRedirect` → role-specific path |
| `/dashboard/farmer` \| `/officer` \| `/admin` \| `/field-officer` | `ProtectedRoute` (no roles) | `DashboardPage roleView=…` |
| `/admin/users` | `ProtectedRoute` (no roles) | `UserManagementPage` |
| `/activities` | `ProtectedRoute` (no roles) | `ActivityDashboard` (component 2) |
| **`/fields`** | `allowedRoles={['Farmer']}` | `FieldsPage` |
| **`/fields/:id`** | `allowedRoles={['Farmer','AgriculturalOfficer','FieldOfficer']}` | `FieldDetailPage` |
| **`/cycles/:id`** | `allowedRoles={['Farmer','AgriculturalOfficer','FieldOfficer']}` | `CycleDetailPage` |
| **`/plans/pending`** | `allowedRoles={['AgriculturalOfficer']}` | `PlanApprovalPage` |

✅ Component 1's four routes **do** pass `allowedRoles`. ⚠ The dashboard, admin and activities
routes still do not, so any logged-in user can open any role's dashboard by typing the URL, and
`DashboardPage` renders whatever `roleView` the *route* names rather than the user's own role.
That remains an open authorization gap for components 2–4 to close on their own routes.

### Auth plumbing (genuinely well built, unchanged)
- **`axiosInstance.ts`** — request interceptor attaches the bearer token; response interceptor
  catches 401, skips auth endpoints, guards with `_retry`, queues concurrent 401s behind a
  single refresh, calls `/auth/refresh` with a *raw* axios instance, replays the original
  request, and on failure clears storage and hard-redirects to `/login`. No timeout is set,
  which is what lets the 20–40 s plan request stand.
- **`tokenStorage.ts`** — `ITokenStorage` + `LocalStorageTokenStorage`, keys
  `paddywise_access_token` / `paddywise_refresh_token` / `paddywise_user`; the constructor
  scrubs legacy `kumburu_*` keys. ⚠ localStorage tokens are XSS-exposed; httpOnly cookies are
  a future hardening story.
- **`authService.ts`** — `extractApiErrorMessage(err, fallback)` unwraps `{message}`,
  `ValidationProblemDetails.errors` and `title`. **Every API call in the app reuses it.**
- **`AuthContext.tsx`** — rehydrates from localStorage on mount, then verifies against
  `GET /auth/me`.
- ⚠ `API_BASE_URL` is still a hard-coded const `http://localhost:5164/api` — it should become
  `import.meta.env.VITE_API_BASE_URL`.

### `features/field-cultivation/` — component 1, the real module

**`types.ts` (434 lines)** — hand-maintained TS mirrors of every component-1 DTO, each with the
C# file it mirrors named in a comment: the enum unions (`Season`, `CultivationMethod`,
`CycleStatus`, `GrowthStage`, `PlanStatus`, `PlanStepCategory`, `DelegationTargetAgent`,
`PlanReviewDecision`), the entity shapes (`Field`, `Division`, `Variety`, `CultivationCycle`,
`StageWindow`, `StageLog`, `CultivationPlan`, `CultivationPlanOutput`, `PlanStep`,
`PlanDelegation`, `AgentRunSummary`, `ToolCallRecord`, `PendingPlanSummary`), the validation
constants that mirror the server's DataAnnotations (`FIELD_RULES`, `CYCLE_RULES`,
`STAGE_LOG_RULES`, `PLAN_RULES`, `PLAN_REVIEW_RULES`), and label maps + safe label functions
(`agentLabel`, `growthStageLabel`, `planStepCategoryLabel`) that tolerate the out-of-set values
a `ValidationFailed` plan can contain, because such a plan is stored verbatim.

**`services/fieldApi.ts`** — one typed function per endpoint, all through `axiosInstance`,
no React: `getMyFields`, `getFieldsByDivision`, `getFieldById`, `createField`, `updateField`,
`deleteField`, `getDivisions`, `getVarieties`, `getMyCycles`, `getCycleById`,
`startCultivation`, `logStage`, `updateCycleStatus`, `requestPlan`, `getPlan`,
`getPlansForCycle`, `getPendingPlans`, `reviewPlan`.

**Pages**
- `FieldsPage` (`/fields`) — the farmer's field list + create form.
- `FieldDetailPage` (`/fields/:id`) — field detail, edit, the field's cycles, "start cultivation".
- `CycleDetailPage` (`/cycles/:id`) — cycle detail, stage timeline, plan request, plan view,
  agent activity. Cycle and plan reads are tracked separately so a failed plan read costs the
  page its plan section, not the cycle.
- `PlanApprovalPage` (`/plans/pending`) — the officer's queue with a division filter, sort,
  the full plan, the agent trail and the review form.

**Components**
- `FieldForm`, `CycleForm` — typed string-draft forms that re-check the server's own rules
  client-side (`FIELD_RULES`, `CYCLE_RULES`) before posting.
- `StageTimeline` — draws the six inclusive windows as a proportional bar with a "today"
  marker, and logs a stage observation.
- `CycleStatusBadge`, `PlanStatusBadge` — one CSS class per status.
- `PlanRequestPanel` — the objective box (two example objectives, 1000-char limit) and the
  in-flight experience. ⚠ Its progress list is **a description, not progress**: nothing in it
  is reported by the server; it advances on a 9-second timer and holds on the last message
  until the real request settles, so it can never claim the plan is ready before it is.
- `PlanView` — summary, steps grouped by stage, delegations, assumptions, validation errors,
  officer comment.
- `AgentActivity` — every agent run behind a plan: name, success, duration, and each tool call's
  args and result pretty-printed.
- `PlanReviewForm` — Approve / Reject / RequestRevision, enforcing the server's rule that
  everything but Approve needs a comment.
- `PendingPlansCard` — the officer dashboard tile with the **live** pending count from
  `GET /api/plans/pending`; on a 403 (any non-officer previewing the officer dashboard) it shows
  "—" rather than a wrong number.

`styles/fieldCultivation.css` uses only the `global.css` tokens.

### `features/crop-resource/` — component 2, still a client-side mock
Route `/activities` → `ActivityDashboard`, tabs Irrigation | Fertilizer | Pesticide | Other.
`ActivityForm.tsx` still uses `useState<any>({})`, has no validation and **makes no API call**;
non-fertilizer submissions just `alert('… recorded successfully!')` and nothing is persisted.
Fertilizer submissions open `FertilizerValidationFlow.tsx`, a fake 4-step "agent" animation
(1 s per step) that then compares against `data/fertilizerRules.ts` — **19 hard-coded rules in a
browser TS array**, self-described as "mocked ranges for simulation", keyed by
region × cropStage × type — and returns accept / officer-review / reject. This is still the
closest thing to a fertilizer knowledge base in the project, and it lives in the browser.
`ActivityDashboard.css` uses a **different variable set** (`--primary-dark`, `--text-muted`,
`glass-panel`), so the module is visually off-system. **Not Nuran's to edit.**

### Pages outside the feature modules
- **`Home.tsx`** — 8 marketing sections. Fully styled, static, no data.
- **`LoginPage` / `RegisterPage`** — real and wired. Register offers Farmer /
  AgriculturalOfficer / FieldOfficer / Admin (**no Buyer**).
- **`DashboardPage.tsx` (439 lines)** — still **almost entirely hard-coded mock data**: the
  farmer's "Yala 2026 / Day 45", "3.5 Acres / Bg 352", "14.0 MT", the photo-upload box whose
  button only fires `alert()`, the static schedule, the officer's fake approval card with
  `alert()` buttons, the field-officer visit route, and the admin block that renders the API's
  own config as content. **The one live thing on it is `PendingPlansCard`** in the officer view.
- **`UserManagementPage.tsx`** — UI only, over a hard-coded `mockUsers` array; there is no
  admin user API.
- **`Sidebar.tsx`** — per-role nav. Real links now: `/fields` (farmer),
  `/plans/pending` (officer, "Review AI Recommendations"), `/admin/users` and
  `/dashboard/admin` (admin). Everything else is still a dead `#anchor` — `#profile`,
  `#activities`, `#report`, `#weather`, `#ai`, `#fields` (officer), `#expert`, `#stats`,
  `#knowledge`, `#settings`, `#inspections`, `#upload`, `#feedback`. **The sidebar is still the
  product backlog**; note the farmer's "Record Activities" points at `#activities` even though
  `/activities` exists.

### Design system (`styles/global.css`)
`--cream #F6F1E3`, `--cream-deep #EDE5CF`, `--ink #212D1E`, `--ink-soft #4B5645`,
`--forest #22392A`, `--forest-deep #182A1E`, `--shoot #7FA66C`, `--shoot-light #B4CB9C`,
`--gold #E1A63B`, `--gold-deep #C48A28`, `--clay #A6693F`, `--line rgba(33,45,30,.14)`,
plus `--line-dark`, `--font-heading: 'Fraunces', serif`, `--font-body: 'Work Sans', sans-serif`.
Shared 1:1 with the Flutter theme — keep them in sync. **Do not create new tokens.**

---

## 5. Mobile — `paddywise_mobile` (Flutter)

Unchanged since the last pass. Dart SDK `^3.9.2`. Dependencies: **only** `cupertino_icons` and
`shared_preferences`. **No `http`/`dio` — the mobile app cannot talk to the backend at all.**

```
lib/
├── main.dart                    AuthService.init() then Login or Dashboard
├── models/user.dart             UserRole enum + User + StoredUser
├── services/auth_service.dart   100% LOCAL MOCK AUTH
├── screens/{login,register,dashboard}_screen.dart
└── theme/app_theme.dart         AppColors mirroring the web palette
test/widget_test.dart            one smoke test
```

Still on a **different auth system and role model**: static `AuthService` over
`shared_preferences` (`kumburu_users` / `kumburu_session`) with 4 seeded demo accounts whose
passwords are stored in plaintext (`passwordHash: 'Password123!'`), and
`UserRole = {farmer, extensionOfficer, buyer, admin}` against the backend's
`{Farmer, AgriculturalOfficer, Admin, FieldOfficer}`. The dashboard has a **Buyer** panel that
exists nowhere else in the system.

**Implication for planning:** the Flutter app is a UI prototype that was never migrated when the
web app moved to the real JWT API. Mobile feature work should start with "add `dio`/`http`, port
`tokenStorage` + `authService` + the refresh-interceptor pattern from the web app, align the
role enum, delete the seed users."

---

## 6. Database

Still **two conflicting sources of truth**, and the gap is now much wider.

### (a) EF Core migrations — what actually exists in the Postgres database
`Fields`, `Divisions`, `Users`, `RefreshTokens`, `Varieties`, `CultivationCycles`,
`GrowthStageLogs`, `CultivationPlans`, `AgentRunLogs`. Int identity PKs. `Users.Role` is an
**int** (enum ordinal). Real FKs with explicit delete behaviour everywhere in component 1;
jsonb columns on the plan and the run log; seeded divisions and varieties.
**Exception: `RefreshTokens.UserId` is still an unconstrained int with no FK.**

### (b) `database/schema.sql` — hand-written, incompatible, and now badly out of date
154 lines covering only `roles`, `divisions`, `users`, `refresh_tokens`, with **UUID** PKs, a
`roles` table (`farmer`, `extension_officer`, `buyer`, `admin`), `full_name`, `role_id` /
`division_id` FKs, `is_active`, an `updated_at` trigger, and the same 4 demo accounts as the
Flutter app (with placeholder hashes that are not valid BCrypt). It knows nothing about fields,
cycles, stage logs, plans or agent logs.

**It is not applied by the backend and does not match the EF model.** It should be deleted, or
demoted to a documentation artefact — evolving the schema through EF migrations is the working
practice and there is no path back.

---

## 7. Agents

The agent layer is component 1's work and the contract the other three components implement
against. Full detail lives in `docs/cultivation-planning-agent.md`.

### Shared contracts — `Agents/Shared/`
- **`IAgent<TInput,TOutput>`** — `Name` + `RunAsync(input, ctx, ct)` returning
  `AgentResult<T> { Success, Output, Error, List<ToolCallRecord> ToolCalls, TimeSpan Duration }`.
- **`AgentContext`** — `RequestedByUserId` + `CorrelationId` (ties every log line of one request).
- **`ToolCallRecord`** — `Tool`, `ArgsJson`, `ResultJson`, `At`.
- **`DelegatedTask` / `DelegatedTaskResult`** — the one cross-component contract.
  `PayloadJson` is deliberately opaque; the receiving component owns the shape.
- **`AgentNames`** — the DI keys: `CultivationPlanningAgent`, `ResourceAnalysisAgent`,
  `PestDiseaseDiagnosisAgent`, `SchedulingValidationAgent`.
- **`ILlmClient`** — one method, `CompleteJsonAsync(systemPrompt, userPrompt, tools, toolExecutor, ct)`.
  **The only place the provider is known — swapping vendors is one class.**
- **`GeminiLlmClient`** (345 lines) — the Gemini REST `generateContent` call and the
  function-calling loop: `functionCall {id,name,args}` in, `functionResponse {id,name,response}`
  back, at most **8 tool rounds** before it takes whatever text came back. Default model
  `gemini-3.6-flash`, overridable with `Gemini:Model`; API key from `Gemini:ApiKey`, with a
  clear throw naming the user-secrets command when it is missing.
- **`LlmException`** — a non-2xx from the provider, carrying status and raw body. **429 is never
  retried silently** — the caller decides.
- **`StubAgents.cs`** — `ResourceAnalysisAgentStub`, `PestDiseaseDiagnosisAgentStub`,
  `SchedulingValidationAgentStub`. Each returns `Success = true` with a "stub — to be
  implemented by the … owner" note. **Components 2–4 replace these by registering a real
  implementation under the same `AgentNames` key.**

### `CultivationPlanningAgent` (component 1, 525 lines)
Turns a farmer's objective into a stage-by-stage plan for one cycle and delegates everything
outside its authority. Its four tools are **all read-only** (`AsNoTracking`, no writes ever) and
**scoped to the run's own cycle** — a `cycleId` that is not the run's cycle is refused with
`"Only cycle N can be read during this run."`:

| Tool | Returns | Scoping |
|---|---|---|
| `get_cycle` | the cycle with field, division and variety; plus `today` and `expectedStageToday` | run's cycle only |
| `get_stage_timeline` | the six stage windows, from the cycle's **stored** dates | run's cycle only |
| `get_variety` | name, duration, age group, notes | unscoped — public reference data |
| `get_previous_cycles` | the last three earlier cycles on the field, with their stage logs | the run cycle's own field only |

`get_cycle` and `get_stage_timeline` are mandatory before the model answers.

**Prompt safety**: the farmer's objective is data, not instructions. It goes in the **user**
prompt inside a `<farmer_objective>` block, never the system prompt, and any attempt to close
that block early is neutralised. The system prompt tells the model to plan the cycle anyway and
note it in `assumptions` if the objective asks for something else. The system prompt also
forbids the agent from deciding dosages: Nutrient steps must refer the farmer to the
ResourceAnalysisAgent and contain no numbers or units at all.

**Output** — `CultivationPlanOutput { summary, steps[], delegations[], assumptions[] }`, whose
JSON Schema is embedded in the system prompt and whose property names are also the API's
contract, since it is stored verbatim in `CultivationPlan.PlanJson`. A step carries
`stage`, `windowStart`, `windowEnd`, `task`, `rationale`, `category`
(LandPrep | Water | Nutrient | Protection | Monitoring | Harvest). A parse failure is a **failed
run, not a crashed request**: the raw reply is kept in `AgentRunLog.RawOutput` and `Error`.

### `CultivationPlanValidator` — the deterministic gate
Plain code: no database, no LLM. It collects **all** failures rather than stopping at the first,
so the reasons can be shown to the farmer verbatim:
1. at least one step and a non-empty summary; 2. every `stage` parses to a `GrowthStage`;
3. every `category` is one of the six; 4. `windowStart <= windowEnd`;
5. each window sits inside its own stage's timeline window **±3 days** (the boundaries come from
fractions of the season, so a couple of days either side is judgement, not error);
6. every window sits inside `[sowingDate, expectedHarvestDate]`; 7. no step's `windowEnd` is
before `requestedOn`; 8. steps are non-decreasing by `windowStart`; 9. every delegation targets
an `AgentNames` constant and carries an instruction; 10. **no step's `task` text matches
`\d+(\.\d+)?\s?(kg|g|ml|l|litre|liter|bag|bags)\b|\d+(\.\d+)?\s?%`** — a quantity in a task is a
dosage decision out of scope.

This is the pattern components 2–4 must follow: **no LLM output reaches a human or the database
without passing a deterministic validator.**

### Plan lifecycle and officer approval
`CultivationPlanService.RequestPlanAsync` writes the `Draft` row **before** the agent runs, so
the run log can point at a plan whatever happens. Then:

```
Draft ──valid──> PendingOfficerApproval ──officer──> Approved | Rejected | RevisionRequested
  └──agent failed / rule broken──> ValidationFailed
```

- A cycle may hold only **one** plan that is `PendingOfficerApproval` or `Approved` at a time, so
  a plan already with an officer is never replaced behind their back (400 otherwise).
- An `LlmException` (including 429) is caught and recorded as a failed run with a
  farmer-readable message; the plan lands in `ValidationFailed`.
- Only a plan that **passed** the validator dispatches its delegations, each mapped onto the
  shared `DelegatedTask` and sent to the keyed agent it names. A delegation that fails or throws
  gets its own failed `AgentRunLog` row and **does not** change the plan's status.
- **Every run writes an `AgentRunLog` row, successful or not**, under one correlation id.
- Reject and RequestRevision **require a comment**; Approve does not.
- Approval is one database transaction: the plan becomes `Approved` and a cycle still `Planned`
  becomes `Active` together, or neither does.

**Honest limits of the agent layer today:** components 2–4's agents are stubs, so every
delegation currently returns a placeholder note; the officer approval that exists is for
*cultivation plans* only (component 1) and is not the general approval module component 4 owns;
there is **no validator test suite** — the rules above are enforced but unverified by any
automated test; and the seeded variety durations the whole timeline rests on are still
unverified against DOA data.

---

## 8. Cross-cutting inconsistencies (read this before designing anything)

| # | Issue | Where |
|---|---|---|
| 1 | **Role model differs 3 ways**: backend/web `{Farmer, AgriculturalOfficer, Admin, FieldOfficer}`; mobile `{farmer, extensionOfficer, buyer, admin}`; schema.sql `{farmer, extension_officer, buyer, admin}` | everywhere |
| 2 | **Buyer/Miller role** exists on the landing page, in the mobile app and in schema.sql, but not in the backend enum or the web register form — and nothing in the codebase serves it | product-level |
| 3 | Mobile uses local mock auth and cannot reach the API at all (no HTTP package) | `auth_service.dart` |
| 4 | `schema.sql` (UUID, roles table, 4 tables) vs EF migrations (int, enum, 9 tables) — the gap is now unbridgeable | `database/` |
| 5 | Role enforcement is complete on component 1's endpoints and routes, absent on every dashboard route and on components 2–4 | web + API |
| 6 | API base URL hard-coded in the web app; CORS hard-coded to `:5173` | `axiosInstance.ts`, `Program.cs` |
| 7 | The activity forms still collect data that is never sent anywhere | `ActivityForm.tsx` |
| 8 | The fertilizer "knowledge base" is still 19 rules in a browser TS array, not a DB table | `fertilizerRules.ts` |
| 9 | Two CSS design systems in the web app | `global.css` vs `ActivityDashboard.css` |
| 10 | `DashboardPage` and `UserManagementPage` are mock data with no backing API | web |
| 11 | `RefreshToken.UserId` has no FK and rows accumulate forever (no cleanup) | `AuthService`, DbContext |
| 12 | Seeded `Variety.DurationDays` / `AgeGroup` are unverified working values that the whole stage timeline and every plan rest on | `ApplicationDbContext` |
| 13 | Zero backend and web tests — including none for `CultivationPlanValidator`; one Flutter smoke test | repo-wide |
| 14 | No CI, no Dockerfile, no `.env.example`, no API client codegen (`types.ts` is mirrored by hand) | repo-wide |
| 15 | `PaddyWise.Api.http` still calls the deleted `/weatherforecast` endpoint | backend |
| 16 | The farmer sidebar's "Record Activities" is a dead `#activities` anchor although `/activities` exists | `Sidebar.tsx` |

---

## 9. Git state

- Current branch: **`feature/field-cultivation`**, **16 commits ahead of `origin/develop`** and
  nothing behind it. The last five commits are component 1's agent work:
  `8407043` agent contracts → `b18112a` planning agent + tools + plan API →
  `fa16470` deterministic validation, officer review, pending queue →
  `a77855f` plan request/view/agent activity on the cycle page →
  `540235e` officer approval queue + live pending count.
- Uncommitted: `CLAUDE.md` (modified).
- `origin/develop` and `origin/CropActivity` are both at `7815243`/`5a85e98` (27 commits) — the
  CropActivity work, including `UserManagementPage`, is already **merged** into this branch.
- `origin/main` is at 9 commits, far behind develop.
- **`origin/PestDiseaseMonitoring` and `origin/Reporting` are still at the initial commit
  (1 commit each) — empty placeholders for components 3 and 4.**
- Convention: branch per feature, PR into **`develop`**. Repo origin
  `github.com/viruldesilva/paddywise-ai`; commit messages are informal.

---

## 10. How to run it

```bash
# Backend  → http://localhost:5164  (Swagger at /swagger; https profile also on 7188)
cd paddywise-backend && dotnet restore && dotnet ef database update && dotnet run

# Web      → http://localhost:5173
cd paddywise-web && npm install && npm run dev

# Mobile
cd paddywise_mobile && flutter pub get && flutter run
```

The backend needs three secrets set first — `Jwt:Key`, `ConnectionStrings:DefaultConnection`
and `Gemini:ApiKey` — via `dotnet user-secrets`. See `README.md` and
`paddywise-backend/README.md`. The web app reaches the API only when the backend is on **5164**
and the dev server on **5173** (both hard-coded). Mobile talks to no backend.

**Always build before finishing a task**: `dotnet build` in `paddywise-backend`,
`npm run build` in `paddywise-web`.

---

## 11. Build-status scoreboard

| Capability | Component | Backend | Web | Mobile | DB |
|---|---|---|---|---|---|
| Register / Login / JWT / refresh rotation | shared | ✅ | ✅ | ❌ local mock | ✅ |
| `GET /me` session verify | shared | ✅ | ✅ | ❌ | – |
| Role-based **routing** | shared | – | 🟡 enforced on component-1 routes only | 🟡 panel switch | – |
| Role-based **authorization** | shared | ✅ on every component-1 endpoint | 🟡 `allowedRoles` on 4 routes | ❌ | – |
| Landing / marketing site | – | – | ✅ complete | – | – |
| Dashboards | 4 | ❌ | 🟡 static mock (one live tile) | 🟡 static mock | – |
| Admin user management | 4 | ❌ | 🟡 UI over a mock array | 🟡 lists local users | ❌ |
| **Divisions** (seeded reference data) | 1 | ✅ | ✅ used by the field form | ❌ | ✅ 5 rows |
| **Varieties** (seeded reference data) | 1 | ✅ | ✅ used by the cycle form | ❌ | 🟡 8 rows, durations unverified |
| **Fields CRUD + ownership + soft delete** | 1 | ✅ | ✅ | ❌ | ✅ |
| **Cultivation cycles** (start, list, detail, status) | 1 | ✅ | ✅ | ❌ | ✅ |
| **Growth stage logging + stage timeline** | 1 | ✅ | ✅ proportional timeline UI | ❌ | ✅ |
| **Cultivation Planning Agent** (Gemini, 4 read-only tools) | 1 | ✅ | ✅ request + view + agent trail | ❌ | ✅ `AgentRunLogs` |
| **Deterministic plan validation** | 1 | ✅ 10 rules, no tests | ✅ errors shown verbatim | ❌ | ✅ `ValidationErrorsJson` |
| **Officer plan approval queue + review** | 1 | ✅ | ✅ `/plans/pending` + live count | ❌ | ✅ |
| **Agent run audit log** | 1 | ✅ every run, success or fail | ✅ `AgentActivity` | ❌ | ✅ |
| Activity logging (irrigation/fertilizer/pesticide) | 2 | ❌ | 🟡 forms submit to nothing | ❌ | ❌ |
| Fertilizer rule validation | 2 | ❌ | 🟡 client-side mock "agent" | ❌ | ❌ |
| Resource Analysis & Action Agent | 2 | 🟡 stub returns a note | ❌ | ❌ | – |
| Pest/disease report + image upload | 3 | ❌ | ❌ (`alert()` stub) | ❌ | ❌ |
| Pest & Disease Diagnosis Agent | 3 | 🟡 stub returns a note | ❌ | ❌ | – |
| Reporting / dashboards over real data | 4 | ❌ | ❌ | ❌ | ❌ |
| Scheduling, Validation & Approval Agent | 4 | 🟡 stub returns a note | ❌ | ❌ | – |
| Weather, harvest, buyers/offers, notifications | – | ❌ | ❌ | 🟡 mock buyer panel | ❌ |
| Knowledge base / pesticide allow-list | 2/4 | ❌ | 🟡 19 hard-coded rules | ❌ | ❌ |
| Tests / CI / Docker | – | ❌ | ❌ | 🟡 1 smoke test | – |

Legend: ✅ working · 🟡 partial/mock/stub · ❌ nothing.

**One-line summary: component 1 is a complete vertical slice — entities, migrations, role-scoped
API, a real LLM agent with read-only scoped tools, a deterministic validator, an officer
approval workflow and the UI for all of it — and components 2, 3 and 4 are three registered
stubs and one clickable mock.**

---

## 12. Conventions a new feature must follow

**Ownership** — Nuran owns component 1 only. Do not create, edit or delete files under
`paddywise-web/src/features/crop-resource/**`, or any backend
`Entities|DTOs|Services|Controllers|Agents/<Component>/` folder or web `src/features/<name>/`
belonging to components 2–4. Shared files (`App.tsx`, `Sidebar.tsx`, `Program.cs`,
`ApplicationDbContext.cs`, `DashboardPage.tsx`) may be touched only for the minimal lines a
task explicitly names — **ask first if it needs more.**

**Backend**
- `Entities/<Component>/X.cs`, `DTOs/<Component>/XDto.cs`,
  `Services/<Component>/{IXService,XService}.cs`,
  `Controllers/<Component>/XController.cs` with `[ApiController] [Route("api/<plural>")]`.
- Register the service in `Program.cs` as `AddScoped`, add `DbSet<X>` to
  `ApplicationDbContext`, configure it in `OnModelCreating`, then `dotnet ef migrations add <Name>`.
  **Never edit or delete an existing migration.**
- Controller error pattern: try/catch `InvalidOperationException` → `BadRequest(new { message })`;
  null → `NotFound(new { message })` / `Unauthorized(new { message })`;
  `UnauthorizedAccessException` → 403 with the same shape. **Keep `{ message }` on every 4xx.**
- `[Authorize]` always, with `Roles = "..."` for anything role-scoped, plus an ownership check
  in the service. Caller id from `ClaimTypes.NameIdentifier`, role from `ClaimTypes.Role`.
- **Never put secrets in `appsettings.json`** — it is tracked. Use `dotnet user-secrets` or
  environment variables.

**Agents**
- Live in `Agents/<Component>/`, implement `IAgent<TInput,TOutput>`, register under the existing
  `AgentNames` key (replacing the stub).
- Tools are **read-only and scoped to the run's own records**.
- **No LLM output reaches a human or the database without a deterministic validator** — follow
  `Services/FieldCultivation/CultivationPlanValidator.cs`.
- **Every run writes an `AgentRunLog` row**, success or failure, under one correlation id.
- **User text goes in the user prompt inside a delimited block, never the system prompt.**
- `ILlmClient` is the only place the provider is known.

**Web**
- New feature → `src/features/<name>/{pages,components,services,types}/`.
- **All HTTP through `src/api/axiosInstance.ts`** (never bare axios), so the bearer token, the
  401 handler and the single-flight refresh queue apply.
- Reuse `extractApiErrorMessage(err, fallback)` from `src/services/authService.ts` for every
  API error.
- Register routes in `App.tsx` inside `<ProtectedRoute allowedRoles={[...]}>`.
- Type state properly — avoid the `useState<any>` pattern in `ActivityForm`.
- Style with the `global.css` custom properties; **never** the crop-resource variable set
  (`--primary-dark`, `--text-muted`, `glass-panel`). **Do not create new design tokens.**
- Fonts: **Fraunces** for headings, **Work Sans** for body.

**Mobile** — needs the HTTP + real-auth foundation before feature work is meaningful.

**Every task** — build before finishing (`dotnet build` / `npm run build`) and report the result.
