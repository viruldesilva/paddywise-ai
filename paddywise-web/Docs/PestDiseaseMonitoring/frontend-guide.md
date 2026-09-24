# Frontend guide — Pest & Disease Monitoring (Component 3)

Scope: `paddywise-web/` only, for whoever is building or maintaining Component 3's UI —
**Pest & Disease Monitoring** (farmer problem reports, photo submission, the diagnosis result,
officer review). Backend conventions, the agent, and the data model are documented separately
in `paddywise-backend/Docs/PestDiseaseMonitoring/backend-guide.md`; see the root `CLAUDE.md`
for cross-component ownership rules.

## Where the code lives

```
src/features/pest-disease/
  types.ts                       TS mirrors of DTOs/PestDisease/*.cs
  services/pestDiseaseApi.ts     every /api/observations + /api/pest-disease-reports call
  utils/dates.ts                 formatDateTime — local to this feature, not shared
  styles/pestDisease.css         pd-* classes, styles/global.css tokens only
  components/
    ObservationForm.tsx          farmer's create/edit form
    ReportReviewForm.tsx         officer's Approve/Reject/RequestRevision form
  pages/
    ObservationsPage.tsx         farmer, routed at /observations
    PestDiseaseReportsPage.tsx   officer, routed at /pest-disease-reports
```

Follow the existing `field-cultivation/` feature folder as the template for this shape
(`components/`, `pages/`, `services/`, `types.ts`) — same convention the root `CLAUDE.md`
describes for all `src/features/<name>/` work.

Do not add PestDisease UI code outside this folder, and do not edit
`src/features/crop-resource/` or `src/features/field-cultivation/` beyond a read-only import
(see below) — those belong to Components 2 and 1.

## Routes and access

Registered in `src/App.tsx`, each behind `ProtectedRoute allowedRoles`:

| Route | Role | Page |
|---|---|---|
| `/observations` | `Farmer` | `ObservationsPage` |
| `/pest-disease-reports` | `AgriculturalOfficer` | `PestDiseaseReportsPage` |

Linked from `src/components/Sidebar.tsx`: the farmer's existing "Report Pests/Diseases" entry
points at `/observations`; a new "Review Pest/Disease Reports" entry under the officer's links
points at `/pest-disease-reports`. `Sidebar.tsx` and `App.tsx` are shared files — only the
minimal lines to add a route/link were touched, per the root `CLAUDE.md`'s shared-file rule.

## Cross-feature reads

`ObservationForm.tsx` needs the farmer's cultivation cycles to populate the "which cycle is
this about" picker. Rather than duplicating that fetch, it imports the existing, already-built
call directly:

```ts
import { getMyCycles } from '../../field-cultivation/services/fieldApi';
import type { CultivationCycle } from '../../field-cultivation/types';
```

This is a read-only cross-feature import, not an edit — the same pattern
`crop-resource/pages/NewActivityPage.tsx` already uses (`getCycleById` from the same
`fieldApi`). It mirrors the backend rule that `CultivationCycle` is Component 1's data, read
but never written from here.

## API surface

Everything goes through `src/api/axiosInstance.ts` (bearer token, 401 refresh queue) via
`services/pestDiseaseApi.ts`:

- `getObservations`, `getObservationById`, `createObservation`, `updateObservation`,
  `requestAnalysis` — wrap `/api/observations`
- `getPestDiseaseReports`, `getPestDiseaseReportById`, `reviewPestDiseaseReport` — wrap
  `/api/pest-disease-reports`

`requestAnalysis` has no client-side timeout (same reasoning as `field-cultivation`'s
`requestPlan`): the agent's Gemini call plus the `get_pest_knowledge` tool loop plus the
deterministic validator can take a while, and `axiosInstance` sets no timeout, which is what
lets the request stand.

Every error is surfaced through `extractApiErrorMessage(err, fallback)` from
`src/services/authService.ts`, matching the backend's `{ message }` error shape.

## Types

`types.ts` mirrors `DTOs/PestDisease/*.cs` exactly: camelCase properties, enum-typed fields as
their C# member name string (e.g. `PestDiseaseReportStatus = 'PendingOfficerReview' | ...`),
not the ordinal. `formatConfidence(confidence)` renders `0.82` as `"82%"` — used everywhere a
confidence is shown, since the agent's confidence is a possible-match score, never certainty
(the UI copy says "possible match", not "diagnosis").

If a backend DTO in `DTOs/PestDisease/` changes shape, update the matching interface here in
the same change — there is no shared schema generation between the two.

## Styling

`styles/pestDisease.css` declares its own `pd-*` class names (page head, cards, buttons, form,
modal, badges, table, drawer, review form, toast) built only on `styles/global.css` custom
properties (`--cream`, `--forest`, `--gold`, `--shoot`, `--clay`, etc.). It does not import or
depend on `field-cultivation/styles/fieldCultivation.css` or the crop-resource variable set
(`--primary-dark`, `--text-muted`, `glass-panel`) — per the root `CLAUDE.md`'s styling rule,
those are a different component's tokens. The `fc-*` class structure was used as a visual
reference when writing `pd-*` (same page-head/card/badge/drawer shapes), but the two files are
independent — editing one never requires touching the other.

## Known gaps (see backend-guide.md's "Current implementation status" for the backend side)

- **No-match visibility.** `ObservationsPage.tsx` decides whether to show "Ask the diagnosis
  agent" vs. the results list purely from `observation.reports.length === 0`. Since the backend
  now treats an empty `possibleIssues` list as a valid "no likely match" outcome rather than a
  failure, a no-match run still produces zero `PestDiseaseReport` rows — so the page can't tell
  "not yet analyzed" apart from "analyzed, nothing matched." A farmer can re-click indefinitely.
  Fixing this needs a backend signal first (something persisted for a no-match run), then a
  frontend branch to render it distinctly.
- **No admin UI** for the `PestDiseaseKnowledge` CRUD endpoints
  (`Controllers/PestDisease/PestDiseaseKnowledgeController.cs`) — the API exists
  (`GET/POST/PUT/DELETE /api/pest-disease-knowledge`), nothing in `paddywise-web` calls it yet.
- **No image upload.** `ObservationForm.tsx`'s photo field is a plain URL text input — the
  farmer must already have the photo hosted somewhere. There is no file-upload endpoint on the
  backend to wire up yet.
- Not yet tested against a real, running backend + live Gemini call from the browser (only
  route-level smoke-tested: pages load, redirect correctly when unauthenticated, no console
  errors) — see backend-guide.md's "real-Gemini smoke test" item.

## Build / lint

```bash
npm install
npm run dev       # :5173, must match backend CORS policy
npm run build     # tsc -b && vite build
npm run lint       # eslint .
```

As of this writing, `npm run build` still fails on 10 pre-existing TypeScript errors in
`src/features/crop-resource/` (Component 2) unrelated to this feature — confirmed via
`git diff` that no pest-disease work touches those files. Scope any build check to this
feature's files if the crop-resource errors haven't been fixed yet:

```bash
npx eslint src/features/pest-disease src/App.tsx src/components/Sidebar.tsx
npx tsc --noEmit -p tsconfig.app.json   # then grep the output for "pest-disease"
```
