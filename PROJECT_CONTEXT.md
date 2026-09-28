# PaddyWise-AI ("Kumburu") — Full Project Context

> Regenerated **2026-09-23** from a full read of the repository at branch
> `feature/field-cultivation` (commit `251e45e`, 32 commits ahead of `origin/main`).
> Purpose: paste this into an LLM chat as complete context before planning or debugging.
> Everything below was verified against the code on that commit, not against older docs.

---

## 1. What the product is

**PaddyWise-AI** (UI name **Kumburu**) is an academic/capstone **agentic-AI decision-support
platform for Sri Lankan paddy (rice) farmers**, putting farmers and agricultural/field officers
on one system.

The intended core loop, from the landing page (`WorkflowSection.tsx`):

```
01 Report  →  02 Analyze (AI agent)  →  03 Recommend (dosage-checked)
           →  04 Approve (officer sign-off)  →  05 Act (farmer gets plan)
```

The governing design principle throughout the codebase: **no LLM output reaches a human or the
database without passing deterministic C# validation, and no plan reaches a farmer without an
officer approving it.** Every agent run is audit-logged whether it succeeds or fails.

### Four business components, one per team member, each with one agent

| # | Component | Agent | Owner area | Real code today? |
|---|---|---|---|---|
| **1** | **Field & Cultivation Management** — fields, cycles, growth stages, cultivation plans | **Cultivation Planning Agent** (workflow coordinator) | `FieldCultivation/`, `features/field-cultivation/` | **Yes — complete end to end** |
| **2** | Crop Activity & Resource Management — irrigation/fertilizer/pesticide logging, fertilizer rules | Resource Analysis & Action Agent | `CropResource/`, `features/crop-resource/` | **Yes — backend + UI, but mostly rule-based, not LLM-driven** |
| **3** | Pest & Disease Monitoring — problem reports, photo upload, diagnosis | Pest & Disease Diagnosis Agent (class: `CropAnalysisAgent`) | `PestDisease/` | **Backend yes (with vision). No web UI at all.** |
| **4** | Reporting, Dashboards & Approval — officer/admin dashboards, approval mgmt, audit | Scheduling, Validation & Approval Agent | `Reporting/` | **No — a stub class and a hardcoded dashboard** |

**Nuran owns Component 1 only.** Ownership boundaries are enforced socially via `CLAUDE.md`:
do not create/edit/delete files under `features/crop-resource/**` or any backend
`Entities|DTOs|Services|Controllers|Agents/<Component>/` for components 2–4. Shared files
(`App.tsx`, `Sidebar.tsx`, `Program.cs`, `ApplicationDbContext.cs`, `DashboardPage.tsx`) may be
touched only for the minimal lines a task explicitly names.

**Marketing copy that has no code behind it:** the landing page still pitches *harvest & market*,
*buyers/millers* and *weather*. There is no harvest-sales, buyer, market-price or weather code
anywhere in the repo, and no Buyer role on the backend. Treat those as out of scope.

---

## 2. Repository layout

```
paddywise-ai/
├── README.md                  # team setup guide
├── CLAUDE.md                  # working rules: ownership, conventions, guardrails
├── PROJECT_CONTEXT.md         # this file
├── database/schema.sql        # HAND-WRITTEN, obsolete, incompatible with EF — see §9
├── docs/cultivation-planning-agent.md   # component 1's agent, in full detail
├── paddywise-backend/         # ASP.NET Core 8 Web API (assembly: PaddyWise.Api)
│   └── Docs/PestDiseaseMonitoring/      # component 3's own guide
├── paddywise-web/             # React 19 + TypeScript + Vite SPA
└── paddywise_mobile/          # Flutter app (Dart 3.9) — fully offline, no backend
```

Three independent stacks. **No monorepo tooling, no shared contract/codegen, no CI, no Docker,
no backend or web tests.** The only automated gates are `dotnet build` and `npm run build`.

---

## 3. THE WORKFLOW — what actually happens end to end

This is the spine of the system. Follow it in order.

### 3.1 Registration and auth

1. `POST /api/auth/register` — name, email, password, **role as a string**, optional phone.
   Password is BCrypt-hashed. Returns an access token + refresh token immediately.
2. `POST /api/auth/login` → same pair. Access token ~20 min, refresh token 7 days.
3. Access token claims: `NameIdentifier` (user id), `Name`, `Email`, `Role`.
   **Every controller reads the caller's id from `ClaimTypes.NameIdentifier`** — never from the
   request body.
4. `POST /api/auth/refresh` rotates: the old refresh token is revoked, a new pair issued.
5. The web SPA stores both tokens (`tokenStorage`) and `axiosInstance` handles bearer injection,
   401 → single-flight refresh → retry, and redirect to `/login` on failure.

Roles, canonical and exact: `Farmer, AgriculturalOfficer, Admin, FieldOfficer`.
`UserRole` is an **enum stored as an int** (Farmer=0, AgriculturalOfficer=1, Admin=2,
FieldOfficer=3). The API accepts the string name (case-insensitive) or the number.

### 3.2 Component 1 — the main flow (fully working)

```
Farmer registers a Field
   ↓  POST /api/fields          (name, area in acres, soil, irrigation, divisionId, lat/lng)
Farmer starts a Cultivation Cycle on that field
   ↓  POST /api/fields/{fieldId}/start-cultivation
       (varietyId, season Yala|Maha, year, method, sowingDate, notes)
   → CycleService computes ExpectedHarvestDate = SowingDate + Variety.DurationDays and
     STORES it. Status = Planned if sowing is in the future, else Active.
   → StageTimelineCalculator derives 6 contiguous growth-stage windows from
     (sowingDate, durationDays) using fixed fractions of the season:
       Nursery 0.00 | Tillering 0.15 | PanicleInitiation 0.40 |
       Flowering 0.55 | GrainFilling 0.70 | Harvest 0.95 → 1.00
Farmer asks the agent for a plan
   ↓  POST /api/cycles/{cycleId}/plans   { objective: "free text in the farmer's own words" }
   → A CultivationPlan row is saved FIRST with Status=Draft, so the audit log can point at it
     no matter what happens next.
   → CultivationPlanningAgent runs (Gemini + 4 read-only tools, 20–40 s).
   → CultivationPlanValidator.Validate() — pure C#, no DB, no LLM — collects ALL failures.
       valid   → Status = PendingOfficerApproval, delegations dispatched to other agents
       invalid → Status = ValidationFailed, errors stored verbatim and shown to the farmer
   → An AgentRunLog row is written either way, plus one per delegation, all under one
     correlation id.
Officer reviews
   ↓  GET  /api/plans/pending?divisionId=   (AgriculturalOfficer only)
   ↓  POST /api/plans/{id}/review  { decision: Approve|Reject|RequestRevision, comment }
   → Reject / RequestRevision REQUIRE a comment.
   → Approve runs in ONE transaction: plan → Approved AND cycle Planned → Active together.
Farmer logs progress
   ↓  POST /api/cycles/{id}/stages  { stage, observedOn, notes }
   → Sets CurrentStage; stage == Harvest also sets Status=Harvested + ActualHarvestDate.
```

**Plan state machine** (`PlanStatus`):

```
Draft ──► ValidationFailed        (agent failed, or a rule broke)  → farmer may retry
      └─► PendingOfficerApproval ──► Approved          (cycle Planned → Active)
                                ├─► Rejected           (comment required)
                                └─► RevisionRequested  (comment required) → farmer may retry
```

A cycle may hold **only one** plan in `PendingOfficerApproval` or `Approved` at a time, so a
plan already sitting with an officer is never silently replaced.

### 3.3 Component 2 — crop activity logging + analysis

```
Farmer logs an activity against a cycle
   ↓  POST /api/cycles/{cycleId}/activities
       { activityType: Irrigation|Fertilizer|Pesticide|Other, date, detailsJson }
   → detailsJson is a jsonb blob whose required keys depend on activityType:
       Fertilizer : type, quantity, cropStage, region, method
       Irrigation : waterLevel, duration, source
       Pesticide  : product, targetPest, quantity, method
       Other      : specificActivity
   → Date rules: not future, not older than 7 days, not before sowing, not after harvest.
Farmer/officer asks for analysis
   ↓  POST /api/cycles/{cycleId}/analysis   (or GET .../analysis/latest — which re-runs it)
   → ResourceAnalysisAgent builds a CycleActivityBundle (parses every activity's JSON),
     computes DAS, estimates the stage, then runs four deterministic diagnostics
     (Water / Fertilizer / Pest / Other), generates rule-based recommendations WITH dosages
     and DOA citations, then runs ActivitySafetyValidator, then asks the LLM for a
     two-sentence executive summary only.
   ↓  POST /api/cycles/{cycleId}/ai-chat    { question }
   → Free-text Q&A grounded in the same bundle; falls back to a deterministic canned answer
     if the LLM call throws.
```

`ActivitySafetyValidator` is the real safety gate here, and it is the strongest piece of
component 2. It: flags 11 Sri Lanka ROP banned/restricted substances found in logged pesticide
history; enforces a 14-day pre-harvest interval by **stripping** any fertilizer/pesticide
recommendation and replacing it with a stop-order; flags >65 kg/ha urea in 10 days and
suppresses any "apply fertilizer" recommendation; and forces an irrigation recommendation on
moisture stress at Flowering. Any of these can set `RequiresOfficerReview = true` — **but
nothing in the system currently acts on that flag** (see §11).

### 3.4 Component 3 — pest & disease diagnosis (backend only)

```
Farmer files an observation
   ↓  POST /api/observations
       { cultivationCycleId, observationType: Pest|Disease|Unknown, symptoms,
         severity: Low|Moderate|Severe, imageUrl? }
   → CropStage is SNAPSHOTTED from the cycle at report time (the cycle moves on; the stage
     relevant to the diagnosis must not).
Farmer requests analysis
   ↓  POST /api/observations/{id}/request-analysis   (once only — refuses if reports exist)
   → CropAnalysisAgent downloads the image (if any), base64-encodes it, and calls Gemini
     with vision + one tool: get_pest_knowledge.
   → The agent MUST look up every candidate name against the PestDiseaseKnowledge table.
   → CropAnalysisValidator rejects any candidate that was never confirmed by a successful
     get_pest_knowledge lookup, any confidence outside 0.00–1.00, any missing source, and a
     missing recommendedNextStep. One retry prompt is allowed.
   → Each surviving candidate becomes a PestDiseaseReport row, Status = PendingOfficerReview.
   → A DiagnosisRunLog row is written either way.
Officer reviews
   ↓  GET  /api/pest-disease-reports?status=&observationId=&cultivationCycleId=
   ↓  POST /api/pest-disease-reports/{id}/review  (AgriculturalOfficer only)
   → PestDiseaseReportStatus: PendingOfficerReview | Approved | Rejected | RevisionRequested
```

Seed knowledge base: 7 entries (Thrips, Brown Planthopper, Yellow Stem Borer, Rice Leaf Folder,
Rice Sheath Mite, Rice Gall Midge, Sheath Rot), each with symptoms, favourable conditions, crop
stages, management guidance and a DOA source string.

### 3.5 Component 4 — does not exist

`SchedulingValidationAgentStub` returns `"stub — to be implemented by the Reporting,
Dashboards & Approval owner"`. There is no `Reporting/` folder in the backend and no
`features/reporting/` in the web app. The dashboards that exist today are hardcoded HTML.

### 3.6 The delegation mechanism (how the four agents connect)

This is the only cross-component coupling, and it is deliberately one-way and opaque.

- The Cultivation Planning Agent's output includes `delegations[]`, each with
  `targetAgent` (one of three `AgentNames` constants), `instruction`, and a free-form `payload`.
- After validation passes, `CultivationPlanService.DispatchDelegationsAsync` maps each one onto
  the shared `DelegatedTask` (`TaskType` = the instruction, `PayloadJson` = serialised payload)
  and resolves the receiving agent **by DI key** (`_services.GetKeyedService<...>(targetAgent)`).
- `PayloadJson` is intentionally opaque — the receiving component owns the shape it expects.
- A delegation that fails is logged as its own failed `AgentRunLog` row and **does not** change
  the plan's status: the plan itself passed validation, so it still belongs in the queue.

```
CultivationPlanningAgent
   ├─ delegation → ResourceAnalysisAgent        (real: runs a full activity analysis)
   ├─ delegation → PestDiseaseDiagnosisAgent    (real: CropAnalysisAgent, but expects a
   │                                             diagnosis payload, not a plan delegation)
   └─ delegation → SchedulingValidationAgent    (stub: returns a note)
```

---

## 4. Backend — `paddywise-backend` (ASP.NET Core 8)

`net8.0`, nullable + implicit usings enabled, `UserSecretsId` set.

| Package | Version | Purpose |
|---|---|---|
| `Microsoft.AspNetCore.Authentication.JwtBearer` | 8.0.10 | JWT bearer auth |
| `Npgsql.EntityFrameworkCore.PostgreSQL` | 8.0.10 | Postgres provider |
| `Microsoft.EntityFrameworkCore.Design` | 8.0.10 | migrations |
| `Microsoft.AspNetCore.OpenApi` | 8.0.22 | OpenAPI metadata |
| `BCrypt.Net-Next` | 4.2.0 | password hashing |
| `Swashbuckle.AspNetCore` | 6.6.2 | Swagger UI with a Bearer scheme |

**No LLM SDK.** `GeminiLlmClient` is hand-written over `HttpClient`.

### Folder layout (every new feature follows this)

```
Agents/Shared/            IAgent, AgentResult, AgentContext, ToolCallRecord, DelegatedTask,
                          DelegatedTaskResult, AgentNames, ILlmClient, GeminiLlmClient,
                          LlmToolDefinition, LlmImagePart, LlmException, StubAgents
Agents/<Component>/       the component's agent + its models/tools
Controllers/<Component>/  [ApiController] [Route("api/<plural>")]
DTOs/<Component>/         XDto.cs with DataAnnotations
Entities/<Component>/     X.cs
Services/<Component>/     IXService.cs + XService.cs
Shared/                   cross-cutting: User, UserRole, RefreshToken, auth
Data/ApplicationDbContext.cs
Migrations/
```

### Entities (14 DbSets)

**Shared:** `User`, `RefreshToken`.
**FieldCultivation:** `Division`, `Field`, `Variety`, `CultivationCycle`, `GrowthStageLog`,
`CultivationPlan`, `AgentRunLog`.
**CropResource:** `CropActivity` (+ `CropActivityType` enum).
**PestDisease:** `CropObservation`, `PestDiseaseReport`, `PestDiseaseKnowledge`, `DiagnosisRunLog`.

Enums: `Season {Yala, Maha}`, `CultivationMethod {Broadcasting, Transplanting, DirectSeeding}`,
`GrowthStage {Nursery, Tillering, PanicleInitiation, Flowering, GrainFilling, Harvest}`,
`CycleStatus {Planned, Active, Harvested, Abandoned}`,
`PlanStatus {Draft, ValidationFailed, PendingOfficerApproval, Approved, Rejected, RevisionRequested}`,
`CropActivityType {Irrigation, Fertilizer, Pesticide, Other}`,
`ObservationType {Pest, Disease, Unknown}`, `ObservationSeverity {Low, Moderate, Severe}`,
`PestDiseaseReportStatus {PendingOfficerReview, Approved, Rejected, RevisionRequested}`.

Notable model configuration in `ApplicationDbContext.OnModelCreating`:

- All agent-output columns are **`jsonb`**, not text: `PlanJson`, `ValidationErrorsJson`,
  `InputJson`, `ToolCallsJson`, `DetailsJson`.
- Unique indexes: `User.Email`, `RefreshToken.Token`, `Variety.Name`,
  `PestDiseaseKnowledge.Name`, and `(FieldId, Season, Year)` on `CultivationCycle` —
  one cycle per field per season per year.
- Check constraint `CK_Fields_Area_Positive` on `"Area" > 0` (quoted so Postgres keeps casing).
- Delete behaviour is deliberate: `Restrict` on user FKs so deleting a user cannot erase an
  audit trail; `Cascade` from cycle → logs/plans/activities/observations; `SetNull` from plan →
  `AgentRunLog` and observation → `DiagnosisRunLog`, so deleting the subject never erases the
  record that an agent ran.
- Seed data: 5 divisions, 8 varieties, 7 pest/disease knowledge entries. **Both the variety
  durations and the knowledge text carry in-code comments saying they are working values that
  MUST be verified against current DOA publications before being relied on.**

### API surface — 40 endpoint attributes across 10 controllers

| Controller | Endpoints |
|---|---|
| `AuthController` | `POST /api/auth/register`, `/login`, `/refresh`, `GET /api/auth/me` |
| `FieldsController` | `GET /api/fields` (Farmer), `GET /api/fields/division/{id}` (Officer/Admin), `GET/POST/PUT/DELETE /api/fields/{id}` (Farmer; delete is soft) |
| `DivisionsController` | `GET /api/divisions` |
| `VarietiesController` | `GET /api/varieties` |
| `CyclesController` | `GET /api/cycles?fieldId=`, `GET /api/cycles/{id}`, `POST /api/fields/{fieldId}/start-cultivation` (Farmer), `POST /api/cycles/{id}/stages`, `PATCH /api/cycles/{id}/status` (Farmer) |
| `PlansController` | `POST /api/cycles/{cycleId}/plans` (Farmer), `GET /api/cycles/{cycleId}/plans`, `GET /api/plans/{id}`, `GET /api/plans/pending?divisionId=` (Officer), `POST /api/plans/{id}/review` (Officer) |
| `CropActivitiesController` | `GET/POST /api/cycles/{cycleId}/activities`, `GET /api/activities?farmerId=&cycleId=&activityType=`, `PUT/DELETE /api/activities/{id}` |
| `CropActivityAnalysisController` | `POST /api/cycles/{cycleId}/analysis`, `GET /api/cycles/{cycleId}/analysis/latest`, `POST /api/cycles/{cycleId}/ai-chat` |
| `ObservationsController` | `GET /api/observations?cultivationCycleId=&fieldId=`, `GET /api/observations/{id}`, `POST /api/observations` (Farmer), `PUT /api/observations/{id}` (Farmer), `POST /api/observations/{id}/request-analysis` (Farmer) |
| `PestDiseaseReportsController` | `GET /api/pest-disease-reports?status=&observationId=&cultivationCycleId=`, `GET /api/pest-disease-reports/{id}`, `POST /api/pest-disease-reports/{id}/review` (Officer) |

**Controller error pattern in use everywhere:**
`InvalidOperationException` → `BadRequest(new { message })`;
`UnauthorizedAccessException` → `403 + { message }`;
null result → `NotFound(new { message })` / `Unauthorized(new { message })`.
Keep the `{ message }` shape — the web's `extractApiErrorMessage` depends on it.

### Business rules worth knowing

- `CycleService`: sowing date no more than **30 days back** or **365 days forward**; a field may
  hold only one `Planned`/`Active` cycle; one cycle per field per season per year; a
  soft-deleted field is a 404.
- Existing cycles read their **stored** `SowingDate`/`ExpectedHarvestDate`, never the variety's
  current duration — editing a variety must not move a running cycle's timeline.
- `CropActivityService`: the 7-day back-dating window (above) plus per-type `detailsJson` key
  validation.

### Configuration

Three secrets, none in the repo *by design* (see §12 for the current violation):
`Jwt:Key` (shared, ≥32 chars — the app throws at startup otherwise),
`ConnectionStrings:DefaultConnection` (shared Neon Postgres),
`Gemini:ApiKey` (per-developer), plus optional `Gemini:PestDiseaseApiKey` for component 3 and
`Gemini:Model` to override the model. Deployment uses `Jwt__Key`,
`ConnectionStrings__DefaultConnection`, `Gemini__ApiKey`.

CORS policy `AllowReactApp` allows exactly `http://localhost:5173`. The API listens on **5164**
(https profile 7188). Both ports are hard-coded on both sides.

---

## 5. The agent framework (`Agents/Shared/`)

```csharp
interface IAgent<TInput, TOutput> {
    string Name { get; }
    Task<AgentResult<TOutput>> RunAsync(TInput input, AgentContext ctx, CancellationToken ct);
}
class AgentResult<T> { bool Success; T? Output; string? Error;
                       List<ToolCallRecord> ToolCalls; TimeSpan Duration; }
class AgentContext   { int RequestedByUserId; string CorrelationId; }
class ToolCallRecord { string Tool, ArgsJson, ResultJson; DateTime At; }
static class AgentNames {
    CultivationPlanning = "CultivationPlanningAgent";
    ResourceAnalysis    = "ResourceAnalysisAgent";
    PestDiseaseDiagnosis= "PestDiseaseDiagnosisAgent";
    SchedulingValidation= "SchedulingValidationAgent";
}
```

`ILlmClient` is the **only** place the provider is known — swapping vendors is one class. It
exposes `CompleteJsonAsync` and `CompleteJsonWithImagesAsync`; both take a tool list and a
`toolExecutor` callback and run the function-calling loop internally.

`GeminiLlmClient` details that matter:

- Endpoint `https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent`.
- Default model `gemini-3.6-flash`, overridable via `Gemini:Model`. `NormalizeModel` strips a
  leading `models/` so pasting a `models.list` name verbatim doesn't 404.
- API key travels in the **`x-goog-api-key` header**, never the URL, and is never logged.
- Tool loop capped at `MaxToolIterations = 8`, then it takes whatever text came back.
- It round-trips Gemini 3.x's `thoughtSignature` on function-call parts — without it the next
  call fails with *"Function call is missing a thought_signature"*.
- Non-2xx (including **429**) throws `LlmException`; **never silently retried** — the caller
  decides.
- Per-instance API key config: keyed DI gives component 3 its own `Gemini:PestDiseaseApiKey`,
  falling back to `Gemini:ApiKey` when unset.

**The four house rules for agents** (from `CLAUDE.md`, and actually honoured in components 1
and 3):

1. An agent's tools are **read-only** and **scoped to the run's own records**.
2. No LLM output reaches a human or the DB without a **deterministic validator**.
3. Every run writes an audit-log row — success or failure.
4. User text goes in the **user** prompt inside a delimited block, never the system prompt.

### 5.1 Cultivation Planning Agent (component 1) — the reference implementation

**Input** `PlanAgentInput { cycleId, objective }`.
**Output** `CultivationPlanOutput`, stored verbatim in `PlanJson`, so its property names *are*
the API contract:

| Field | Shape |
|---|---|
| `summary` | two or three sentences for the farmer |
| `steps[]` | `stage`, `windowStart`, `windowEnd` (`YYYY-MM-DD`), `task`, `rationale`, `category` |
| `delegations[]` | `targetAgent`, `instruction`, `payload` (opaque object) |
| `assumptions[]` | everything filled in because the data didn't say |

`category` ∈ `LandPrep, Water, Nutrient, Protection, Monitoring, Harvest`.
The JSON Schema for this shape is embedded in the system prompt (`CultivationPlanJson.JsonSchema`).

**Tools — all read-only `AsNoTracking`, all scoped to the run's cycle.** A `cycleId` that isn't
the run's cycle is refused with `"Only cycle N can be read during this run."`, so an objective
crafted to steer the model at another farmer's data gets nothing back.

| Tool | Returns | Scoping |
|---|---|---|
| `get_cycle` | cycle + field + division + variety, `today`, `expectedStageToday` | run's cycle only |
| `get_stage_timeline` | the six stage windows from the cycle's **stored** dates | run's cycle only |
| `get_variety` | name, duration, age group, notes | unscoped (public reference data) |
| `get_previous_cycles` | last 3 earlier cycles on this field + their stage logs | run cycle's own field only |

`get_cycle` and `get_stage_timeline` are mandatory before the model may answer.

**Prompt-injection handling:** the objective is inserted into a `<farmer_objective>` block in
the *user* prompt, and any literal `</farmer_objective>` in the farmer's text is rewritten to
`[/farmer_objective]` so the block cannot be closed early. The system prompt explicitly says the
objective is data, not instructions.

**Validation rules** (`CultivationPlanValidator.Validate` — plain code, no DB, no LLM, collects
*all* failures rather than stopping at the first):

1. At least one step, and a non-empty summary.
2. Every `step.stage` parses to a `GrowthStage`.
3. Every `step.category` is one of the six categories.
4. `windowStart <= windowEnd` on every step.
5. Every step's window lies inside its own stage's timeline window, **±3 days** (stage
   boundaries come from fractions of the season, so a couple of days either side is an agronomic
   judgement call, not a mistake).
6. Every window lies inside `[sowingDate, expectedHarvestDate]`.
7. No step's `windowEnd` is before `requestedOn` — "step is in the past".
8. Steps are ordered non-decreasing by `windowStart`.
9. Every delegation targets one of the three `AgentNames` constants and carries a non-empty
   instruction.
10. **No step's `task` matches** `\d+(\.\d+)?\s?(kg|g|ml|l|litre|liter|bag|bags)\b|\d+(\.\d+)?\s?%`
    — a quantity in a task text means the planning agent decided a dosage it has no authority
    over. That authority belongs to the Resource Analysis agent.

Error handling: provider failure (incl. 429) → run logged failed, plan → `ValidationFailed` with
a farmer-readable message; unparseable reply → the raw text is kept in `AgentRunLog.RawOutput`
and `Error`; unknown tool / bad args → `{"error": "..."}` returned to the model so it can
recover within the same run.

Full detail lives in **`docs/cultivation-planning-agent.md`**.

### 5.2 Resource Analysis Agent (component 2)

Implements **both** `IAgent<DelegatedTask, DelegatedTaskResult>` (for delegation) and
`ICropActivityAnalysisService` (for the direct API). Architecturally it is *not* an LLM agent:
diagnostics and recommendations are hand-written C# rules; the LLM is used only for (a) a
two-sentence executive summary and (b) free-text chat, both with deterministic fallbacks. It
has no `LlmToolDefinition` tools and no tool loop.

It **is** the component that legitimately states dosages ("Apply 1st Top Dressing of Urea at
50 kg/ha") — that is exactly the authority component 1's validator refuses to exercise.

### 5.3 Crop Analysis Agent (component 3)

The closest sibling to component 1's agent: one read-only tool (`get_pest_knowledge`), vision
support via `CompleteJsonWithImagesAsync`, a deterministic validator
(`CropAnalysisValidator`) that **rejects any candidate the model did not confirm against the
knowledge base**, one retry prompt on a bad parse, and a `DiagnosisRunLog` row every run.

### 5.4 Scheduling & Validation Agent (component 4)

`SchedulingValidationAgentStub` — returns a note, does nothing.

---

## 6. Web frontend — `paddywise-web` (React 19 + TS + Vite)

React 19.2, react-router-dom 7.18, axios 1.20, lucide-react icons, TypeScript ~6.0, Vite 8.
**No state library, no data-fetching library, no component library, no tests.**

```
src/
├── api/axiosInstance.ts        ALL HTTP goes through this — bearer, 401 handler, refresh queue
├── components/                 landing-page sections + Navbar, Sidebar, ProtectedRoute
├── context/AuthContext.tsx     + hooks/useAuth.ts
├── features/
│   ├── field-cultivation/      component 1 — the real module (~3,270 lines)
│   │   ├── pages/              FieldsPage, FieldDetailPage, CycleDetailPage, PlanApprovalPage
│   │   ├── components/         FieldForm, CycleForm, StageTimeline, PlanRequestPanel,
│   │   │                       PlanView, PlanReviewForm, AgentActivity, PendingPlansCard,
│   │   │                       CycleStatusBadge
│   │   ├── services/fieldApi.ts   every /fields /divisions /cycles /varieties /plans route, typed
│   │   ├── types.ts            TS mirrors of the backend DTOs + validation constants + labels
│   │   ├── utils/dates.ts
│   │   └── styles/fieldCultivation.css
│   ├── crop-resource/          component 2 (~3,520 lines) — ActivityDashboard, ActivityForm,
│   │                           ActivityHistory, ActivityAnalytics, EditActivityModal,
│   │                           AiAdvisorPanel, FertilizerValidationFlow, fertilizerRules.ts
│   └── (no pest-disease, no reporting)
├── pages/                      Home, LoginPage, RegisterPage, DashboardPage, UserManagementPage
├── services/                   authService.ts (extractApiErrorMessage), tokenStorage.ts
├── styles/                     global.css (the design tokens), Auth, Dashboard, Sidebar, UserManagement
└── utils/roleRoutes.ts
```

### Routing (`App.tsx`)

| Path | Guard |
|---|---|
| `/`, `/login`, `/register` | public |
| `/dashboard` | authed → redirects by role |
| `/dashboard/farmer`, `/dashboard/officer`, `/dashboard/admin`, `/dashboard/field-officer` | **authed only — NO role check** |
| `/admin/users` | **authed only — NO role check** |
| `/fields` | `allowedRoles={['Farmer']}` |
| `/fields/:id`, `/cycles/:id` | Farmer, AgriculturalOfficer, FieldOfficer |
| `/cycles/:id/activities/new` | Farmer |
| `/plans/pending` | AgriculturalOfficer |
| `/activities` | **authed only — NO role check** |

### Design system (`styles/global.css`) — the only tokens that exist

```
--cream #F6F1E3   --cream-deep #EDE5CF   --ink #212D1E    --ink-soft #4B5645
--forest #22392A  --forest-deep #182A1E  --shoot #7FA66C  --shoot-light #B4CB9C
--gold #E1A63B    --gold-deep #C48A28    --clay #A6693F
--line rgba(33,45,30,.14)   --line-dark rgba(246,241,227,.16)
--font-heading 'Fraunces' (serif)   --font-body 'Work Sans'
```

`CLAUDE.md` forbids inventing new tokens and forbids the crop-resource variable set
(`--primary-dark`, `--text-muted`, `glass-panel`). Those names no longer appear in `src/` — that
divergence has been cleaned up.

---

## 7. Mobile — `paddywise_mobile` (Flutter, Dart 3.9)

`lib/{main,models/user,screens/{login,register,dashboard},services/auth_service,theme/app_theme}.dart`.

**It talks to no backend at all.** `AuthService` is a `shared_preferences`-backed fake with four
hard-coded demo users and **plaintext passwords** (`'Password123!'` stored as `passwordHash`).
Its role enum includes `buyer` and `extensionOfficer` — names that do not exist on the backend.
`flutter analyze` and `flutter test` (one widget smoke test) are available. It is a UI
prototype, unrelated to Component 1 work.

---

## 8. Database

### (a) EF Core migrations — what actually exists in Postgres (Neon)

| Migration | Adds |
|---|---|
| `20260909130307_InitialCreate` | Fields |
| `20260910105145_AddUsersAndRefreshTokens` | Users, RefreshTokens |
| `20260916115824_AddDivisionsAndFieldOwnership` | Divisions, Field.FarmerId/DivisionId |
| `20260916123714_AddCultivationCycles` | Varieties, CultivationCycles, GrowthStageLogs |
| `20260916141243_AddCultivationPlansAndAgentLogs` | CultivationPlans, AgentRunLogs |
| `20260917163455_AddPestDiseaseMonitoring` | CropObservations, PestDiseaseReports, PestDiseaseKnowledge |
| `20260919121027_AddCropActivities` | CropActivities |
| `20260920094801_AddDiagnosisRunLog` | DiagnosisRunLogs |

Apply with `dotnet ef database update`. **Never edit or delete an existing migration — always
add a new one.**

### (b) `database/schema.sql` — obsolete, do not use

154 lines of hand-written SQL that **contradicts the EF model**: `snake_case` table names, a
separate `roles` table with `'farmer' | 'extension_officer' | 'buyer' | 'admin'` (there is no
Buyer role and no roles table on the backend), a `Colombo HQ` division that isn't seeded, and
nothing at all for cycles, plans, activities, observations or agent logs. It is dead weight kept
in the repo. The EF migrations are the single source of truth.

---

## 9. Build status scoreboard (verified 2026-09-23)

| Check | Result |
|---|---|
| `dotnet build` in `paddywise-backend` | ✅ **Build succeeded. 0 warnings, 0 errors.** |
| `npm run build` in `paddywise-web` | ❌ **FAILS — 10 TypeScript errors** (all in `features/crop-resource/`) |
| `npm run lint` in `paddywise-web` | eslint available, not run here |
| Backend tests | none exist |
| Web tests | none exist |
| Mobile | `flutter analyze` / `flutter test` (one smoke test) |

The 10 build errors, all component 2:

```
ActivityForm.tsx(96,119,129,162)  TS2367  comparing number to '' — 'number' and 'string'
                                          have no overlap (the `=== ''` guards are dead code)
AiAdvisorPanel.tsx(10)            TS6133  'HelpCircle' declared but never read
AiAdvisorPanel.tsx(21)            TS6133  'ActivityRecommendation' declared but never read
AiAdvisorPanel.tsx(35)            TS6133  'onCycleSelect' declared but never read
AiAdvisorPanel.tsx(344,108)       TS2551  'TotalUreaKgPerHa' does not exist — meant
                                          'totalUreaKgPerHa'  ← real runtime bug, PascalCase
                                          fallback never matches the camelCase wire format
ActivityDashboard.tsx(21)         TS6133  'setRefreshTrigger' declared but never read
NewActivityPage.tsx(16)           TS6133  'navigate' declared but never read
```

---

## 10. WHAT NEEDS TO BE BUILT

Ordered by how much the product depends on it.

### A. Component 4 — Reporting, Dashboards & Approval (nothing exists)

1. **A real dashboard.** `DashboardPage.tsx` is ~600 lines of hardcoded demo content: "Yala 2026,
   Day 45", "3.5 Acres", "14.0 MT", "42 approved treatments", "186 registered farmers", a fake
   pending-approval card, and buttons wired to `alert()`. The *only* live element is
   `<PendingPlansCard />`. Every role's dashboard needs real aggregate endpoints.
2. **`UserManagementPage.tsx` is 100% mock** — five hardcoded users, no API. There is **no
   users/admin controller on the backend at all**. Needs `GET/PUT/DELETE /api/users` with
   `[Authorize(Roles="Admin")]`.
3. **The Scheduling, Validation & Approval Agent** — currently a stub that every cultivation
   plan delegates to and gets a placeholder back from.
4. **An audit/reporting view** over `AgentRunLog` and `DiagnosisRunLog`. The data is all there
   and nothing reads it.

### B. Component 3 — the entire web UI

The backend is done (observations, vision diagnosis, knowledge base, officer review queue), and
**not one line of React consumes it.** There is no `features/pest-disease/` directory. The
farmer dashboard's "Upload Leaf or Pest Photo" box calls
`alert('Photo diagnosis is connected to the backend API & AI agent pipeline.')` — it is not.
Needed: a report form, photo upload, an observation list, a diagnosis view showing candidates
with confidence and source, and an officer review queue.

### C. Image storage

`CropObservation.ImageUrl` is a bare string and `CropAnalysisAgent` **downloads it over HTTP**.
There is no upload endpoint, no blob/file storage, no validation of where the URL points.
A real upload path is a prerequisite for B.

### D. Cross-cutting gaps

5. **Nothing consumes `RequiresOfficerReview`.** Component 2's safety validator can flag a banned
   pesticide or a PHI breach, sets the flag, and then nothing routes it to an officer. It needs
   to become a queue row like `CultivationPlan` and `PestDiseaseReport` already are.
6. **Delegation results are logged and discarded.** `DispatchDelegationsAsync` writes the
   receiving agent's output into `AgentRunLog.RawOutput` and never surfaces it. A farmer who gets
   a plan saying "see the Resource Analysis agent's recommendation for quantities" has no way to
   see that recommendation next to the plan.
7. **The delegation payload contract is unenforced.** `PayloadJson` is opaque by design, but
   `CropAnalysisAgent` expects a `CropAnalysisAgentInput` and the planning agent will hand it a
   free-form payload. A delegation to `PestDiseaseDiagnosisAgent` will parse to nothing useful.
8. **No notifications.** Officer approval, rejection and safety alerts all happen silently.
9. **Officer division scoping.** `GET /api/plans/pending` takes an optional `divisionId` query
   param and returns *every* division's plans when it's omitted. Officers have no division
   assignment on `User` at all.
10. **No seed/demo data script.** Onboarding a teammate means hand-creating a farmer, a field, a
    cycle and a plan through Swagger.

---

## 11. WHAT NEEDS TO BE FIXED

Ordered by severity. Items 1–2 are the ones to do today.

### 🔴 1. A live database credential is committed to git

`paddywise-backend/appsettings.json` contains the **full Neon Postgres connection string
including the password**, in the tracked file, on `HEAD`.

The history shows this was already fixed once and then regressed:

```
88584a3  Move secrets to user-secrets, add JWT key startup check   ← removed it
4828fc3  crop activity form backend and finalize ui                ← put it back
```

Both `CLAUDE.md` and `Docs/PestDiseaseMonitoring/backend-guide.md` state that `appsettings.json`
holds no secrets "by design". It currently does.

**Fix:** rotate the Neon password, blank the value in `appsettings.json`, move it to
`dotnet user-secrets`, and tell the team to re-set their local secret. The credential is in git
history, so rotation is not optional — removing the line is not enough.

### 🔴 2. Privilege escalation via self-registration

`POST /api/auth/register` is anonymous and takes the role as a free string:

```csharp
if (!Enum.TryParse<UserRole>(request.Role, true, out var role))
    throw new InvalidOperationException("Invalid role specified.");
```

Anyone can register as `Admin` or `AgriculturalOfficer` and immediately receive a token with
that role claim — which grants the plan approval queue, `POST /api/plans/{id}/review`,
`POST /api/pest-disease-reports/{id}/review`, and every officer read path.

**Fix:** public registration should hard-code `Farmer`. Officer/Admin accounts get created by an
Admin through an authenticated endpoint (which doesn't exist yet — see §10 A2).

### 🟠 3. The web app does not build

10 TypeScript errors, all in `features/crop-resource/` — listed in §9. Nine are dead
code/unused-import noise; one (`TotalUreaKgPerHa`) is a real display bug. **This is component 2's
code, so it is not Nuran's to fix under the ownership rules** — it needs raising with that
owner, or an explicit hand-off.

### 🟠 4. IDOR on the crop-activity analysis endpoints

`CropActivityAnalysisController` is `[Authorize]` with **no role restriction and no ownership
check**, and `ResourceAnalysisAgent.AnalyzeActivitiesAsync` takes a `userId` and never uses it
for authorization. Any authenticated user — including a self-registered Farmer — can call
`POST /api/cycles/{anyCycleId}/analysis` and `POST /api/cycles/{anyCycleId}/ai-chat` and read
another farmer's full activity history, diagnostics and field details. Every other controller in
the codebase does check ownership; these three endpoints are the exception.

### 🟠 5. Unrestricted cross-farmer reads on `GET /api/activities`

`CropActivitiesController.GetAllActivities` pins `farmerId` to the caller **only when the role
is exactly `"Farmer"`**. Any other role — `FieldOfficer`, `Admin`, or a forged-role token —
gets every activity in the database with no division scoping.

### 🟠 6. SSRF via `CropObservation.ImageUrl`

`CropAnalysisAgent.LoadImageAsync` does `httpClient.GetAsync(imageUrl)` on a farmer-supplied
URL with no scheme/host/IP validation. It checks the content type and a size cap *after* the
request, so an attacker can still make the server issue arbitrary outbound GETs — including to
cloud metadata endpoints and internal addresses. Response bodies aren't returned to the caller
unless they're a valid image, which limits it, but the request itself is the problem.

**Fix:** allow-list the scheme (https only) and the storage host, and block private/link-local
ranges. Properly solved by §10 C — take an uploaded file instead of a URL.

### 🟡 7. Frontend role guards are missing on four routes

`/dashboard/admin`, `/dashboard/officer`, `/dashboard/field-officer`, `/admin/users` and
`/activities` are wrapped in a bare `<ProtectedRoute>` with **no `allowedRoles`**, and
`DashboardPage` renders whichever view its `roleView` *prop* names — not the user's actual role.
A logged-in Farmer can navigate straight to `/dashboard/admin` and see the admin console, and to
`/admin/users` and see the user-management screen. Both are currently mock UI so nothing real
leaks, but the guard must exist before they're wired to real endpoints. `ProtectedRoute` already
supports `allowedRoles` — the routes just don't pass it. (Client-side guards are UX, not
security; the server-side `[Authorize(Roles=...)]` is what actually protects data, and it is
mostly correct.)

### 🟡 8. `GET /analysis/latest` re-runs the analysis

It is a `GET` that executes the full agent pipeline and **writes an `AgentRunLog` row** — a GET
with side effects, extra LLM cost, and no caching. There is no stored analysis to fetch; the
name is a lie. Either persist analyses and read them back, or make it a POST.

### 🟡 9. Component 2's `AgentRunLog` rows are dishonest

`ResourceAnalysisAgent` writes `DurationMs = 150` hard-coded, `Success = true` unconditionally,
and a synthetic `ToolCallsJson` describing tools that don't exist. The audit trail for component
2 is decorative. Components 1 and 3 do this correctly — copy their pattern.

### 🟡 10. Two divergent growth-stage models

Component 1 uses the `GrowthStage` enum and `StageTimelineCalculator` fractions
(0.15 / 0.40 / 0.55 / 0.70 / 0.95). Component 2's `CropActivityTools.CalculateCurrentStage`
returns **strings** from **different** thresholds (0.45 / 0.65 / 0.80) with different names
(`"Nursery / Establishment"`, `"Grain Filling / Ripening"`, `"Harvest Ready"`). The same cycle on
the same day can be reported as two different stages by two parts of the same product.

### 🟡 11. Agronomic constants are unverified

Both `Variety.DurationDays` and the `PestDiseaseKnowledge` seed text carry in-code comments
saying they are working values that must be checked against current Department of Agriculture
publications. `fertilizerRules.ts` says "mocked ranges for simulation". Component 2's
recommendations state kg/ha figures with DOA citations attached. **Nothing in the product has
been agronomically verified**, and the citations imply an authority the data doesn't have.

### 🟡 12. Mobile app stores plaintext passwords

`paddywise_mobile/lib/services/auth_service.dart` keeps `passwordHash: 'Password123!'` in
`shared_preferences`. It's a demo with no backend, but it should not ship in that shape, and its
role enum (`buyer`, `extensionOfficer`) doesn't match the backend's.

### 🟢 13. Smaller items

- `database/schema.sql` is obsolete and contradicts the EF model — delete it or mark it dead.
- `package-lock.json` at the repo root is an empty stub with no `package.json` — vestigial.
- `PATCH /api/cycles/{id}/status` accepts any `CycleStatus` with no transition rules: a
  `Harvested` cycle can be moved back to `Planned`.
- `Sidebar.tsx` has 12 links pointing at `#profile`, `#report`, `#weather`, `#fields`, `#expert`,
  `#stats`, `#knowledge`, `#settings`, `#inspections`, `#upload`, `#feedback` — dead anchors.
- `CropActivitiesController.UpdateActivity`/`DeleteActivity` carry **two** `[HttpPut]`/
  `[HttpDelete]` attributes each (`/api/activities/{id}` and `{id}` relative to the cycle route),
  which registers each action at two URLs.
- Component 2 checks roles by comparing **magic strings** (`userRole == "Farmer"`) rather than
  parsing `UserRole`, unlike components 1 and 3.

---

## 12. Conventions any new work must follow

**Backend**

- Folder layout per component: `Entities/<C>/`, `DTOs/<C>/`, `Services/<C>/{IX,X}Service.cs`,
  `Controllers/<C>/XController.cs` with `[ApiController]` + `[Route("api/<plural>")]`.
- Keep the `{ message }` shape on **every** 4xx — the web's `extractApiErrorMessage` unwraps
  `{ message }`, `ValidationProblemDetails.errors` and `title`.
- `[Authorize]` always, and `Roles = "..."` for anything role-scoped.
- Caller id from `ClaimTypes.NameIdentifier`, never the request body.
- Register services as `AddScoped` in `Program.cs`, add the `DbSet<X>`, configure it in
  `OnModelCreating`, then `dotnet ef migrations add <Name>`.
- **Never** put a secret in `appsettings.json`. **Never** edit or delete an existing migration.
- `dotnet build` before finishing. Report the result.

**Web**

- New feature → `src/features/<name>/{pages,components,services,types}/`.
- **All HTTP through `src/api/axiosInstance.ts`** — never bare `axios`.
- Reuse `extractApiErrorMessage(err, fallback)` for every API error.
- Register routes in `App.tsx` inside `<ProtectedRoute allowedRoles={[...]}>`.
- Type state properly; avoid `useState<any>`.
- Use only the `global.css` tokens listed in §6. Do not create new design tokens.
- Fonts: Fraunces (headings), Work Sans (body).
- `npm run build` before finishing. Report the result.

**Agents**

- Tools read-only and scoped to the run's own records.
- A deterministic validator between every LLM output and any human or database.
- One audit-log row per run, success or failure, under one correlation id.
- User text in the user prompt inside a delimited block, never the system prompt.
- `ILlmClient` stays the only place the provider is named.

**Ask before touching** `App.tsx`, `Sidebar.tsx`, `Program.cs` or `ApplicationDbContext.cs`
beyond the single minimal line needed to register a route, service or DbSet.

---

## 13. How to run it

```bash
# one-time, per machine, from paddywise-backend/
dotnet user-secrets set "Jwt:Key" "<shared-team-secret, >=32 chars>"
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<shared Neon string>"
dotnet user-secrets set "Gemini:ApiKey" "<your own key from aistudio.google.com/apikey>"

cd paddywise-backend && dotnet restore && dotnet ef database update && dotnet run
#   → http://localhost:5164   (Swagger at /swagger; https profile also on 7188)

cd paddywise-web && npm install && npm run dev
#   → http://localhost:5173

cd paddywise_mobile && flutter pub get && flutter run
```

The web app reaches the API **only** on 5164 ↔ 5173 — both hard-coded (`axiosInstance.ts`
`API_BASE_URL`, and the `AllowReactApp` CORS policy in `Program.cs`). The backend throws at
startup if `Jwt:Key` is missing or under 32 characters. Mobile talks to no backend.

**Branching:** feature branch off `develop`, PR back into `develop`. Current branch
`feature/field-cultivation` is 32 commits ahead of `origin/main`. Active remote branches:
`CropActivity`, `PestDiseaseMonitoring`, `Reporting`, `feature/dashboard`,
`feature/userValidation`, `login`, `mobile`, `develop`, `main`.
