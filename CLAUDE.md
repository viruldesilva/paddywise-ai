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

**Nuran owns Component 1 only.** Do not create, edit, or delete files belonging to other
components: `paddywise-web/src/features/crop-resource/**`, and any backend folder
`Entities|DTOs|Services|Controllers|Agents/<Component>/` or web `src/features/<name>/` for
components 2–4 (CropResource, PestDisease, Reporting). Shared files (`App.tsx`, `Sidebar.tsx`,
`Program.cs`, `ApplicationDbContext.cs`, `DashboardPage.tsx`) may be edited only for the
minimal lines a task explicitly names. If a task seems to need more, stop and ask.

## 2. Running the apps

```bash
# Backend → http://localhost:5164   (Swagger at /swagger; https profile also on 7188)
cd paddywise-backend && dotnet restore && dotnet ef database update && dotnet run

# Web → http://localhost:5173
cd paddywise-web && npm install && npm run dev

# Mobile
cd paddywise_mobile && flutter pub get && flutter run
```

The web app reaches the API only when the backend is on **5164** and the dev server on **5173** —
both are hard-coded (API base URL in `src/api/axiosInstance.ts`, CORS policy `AllowReactApp` in
`Program.cs`). Mobile talks to no backend yet.

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

- New feature → `src/features/<name>/{pages,components,services,types}/`.
- **All HTTP goes through `src/api/axiosInstance.ts`** (never bare `axios`) so the bearer token,
  the 401 handler and the single-flight refresh queue apply.
- Reuse `extractApiErrorMessage(err, fallback)` from `src/services/authService.ts` for every API
  error — it unwraps `{ message }`, `ValidationProblemDetails.errors`, and `title`.
- Register routes in `App.tsx` inside `<ProtectedRoute allowedRoles={[...]}>` — start using that
  prop; today no route passes it, which is an open authorization gap.
- Type your state properly; avoid the `useState<any>` pattern.
- Style with the `styles/global.css` custom properties — **never** the crop-resource variable set
  (`--primary-dark`, `--text-muted`, `glass-panel`):

  `--cream #F6F1E3`, `--cream-deep #EDE5CF`, `--ink #212D1E`, `--ink-soft #4B5645`,
  `--forest #22392A`, `--forest-deep #182A1E`, `--shoot #7FA66C`, `--shoot-light #B4CB9C`,
  `--gold #E1A63B`, `--gold-deep #C48A28`, `--clay #A6693F`, `--line rgba(33,45,30,.14)`

- Fonts: **Fraunces** (serif) for headings, **Work Sans** for body.

## 5. Roles

The canonical role list is exactly:

```
Farmer, AgriculturalOfficer, Admin, FieldOfficer
```

`UserRole` is an enum **stored in the database as an int** (ordinal: Farmer=0,
AgriculturalOfficer=1, Admin=2, FieldOfficer=3). The API accepts the string name
(case-insensitive) or the numeric value. There is no Buyer role on the backend or the web app,
despite what the Flutter app and `database/schema.sql` contain.

## 6. Rules for every task

- **Always build before finishing.** `dotnet build` in `paddywise-backend` for backend changes;
  `npm run build` in `paddywise-web` for web changes. Report the result.
- **Never edit `appsettings.json` to add secrets.** Use `dotnet user-secrets` or environment
  variables. The file is tracked by git.
- **Never delete or rewrite an existing migration.** Always add a new one.
- **Do not create new CSS design tokens.** Use the existing `global.css` variables listed above.
- **Keep the `{ message }` error shape** on every 4xx response.
- **Ask before touching `App.tsx`, `Sidebar.tsx`, `Program.cs`, or `ApplicationDbContext.cs`**
  beyond the single minimal line needed to register a route, service, or DbSet.

## 7. Agents

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
