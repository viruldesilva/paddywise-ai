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
    KnowledgeEntryForm.tsx       admin's add/edit form for one knowledge base entry
  pages/
    ObservationsPage.tsx         farmer, routed at /observations
    PestDiseaseReportsPage.tsx   officer, routed at /pest-disease-reports
    KnowledgeBasePage.tsx        admin, routed at /pest-disease-knowledge
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
| `/pest-disease-knowledge` | `Admin` | `KnowledgeBasePage` |

Linked from `src/components/Sidebar.tsx`: the farmer's existing "Report Pests/Diseases" entry
points at `/observations`; a new "Review Pest/Disease Reports" entry under the officer's links
points at `/pest-disease-reports`; the admin's pre-existing "Manage Knowledge Base" placeholder
(`path: '#knowledge'`) now points at `/pest-disease-knowledge`. `Sidebar.tsx` and `App.tsx` are
shared files — only the minimal lines to add a route/link were touched, per the root
`CLAUDE.md`'s shared-file rule.

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
(the UI copy says "possible match", not "diagnosis"). `PestDiseaseCategory = 'Pest' | 'Disease'`
mirrors `Entities/PestDisease/PestDiseaseCategory.cs`, with `PEST_DISEASE_CATEGORIES` /
`PEST_DISEASE_CATEGORY_LABELS` following the same const-array-plus-label-map shape as
`OBSERVATION_TYPES`/`OBSERVATION_TYPE_LABELS`.

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

- Fixed: no-match visibility. `Observation.lastAnalyzedAt` (from the backend's new
  `CropObservation.LastAnalyzedAt`) is now on the DTO. `ObservationsPage.tsx` renders three
  states off `reports.length` and `lastAnalyzedAt` together: never analyzed → "Ask the
  diagnosis agent"; analyzed with matches → the results list; analyzed with none → a `.pd-no-match`
  note ("found no likely match... as of `<date>`") plus "Ask the diagnosis agent again", instead
  of the two states silently colliding.
- Fixed: admin UI for the knowledge base. `KnowledgeBasePage.tsx` (`/pest-disease-knowledge`,
  `Admin` only) lists every entry in a table (name, symptoms preview, source, updated date),
  with Add/Edit via `KnowledgeEntryForm.tsx` (same validation-mirrors-DataAnnotations pattern
  as `ObservationForm.tsx`) and Delete behind a `window.confirm`. Verified end to end against
  the live API: create, edit (pre-fills correctly), and the list refreshing after each action —
  delete was verified via the API directly rather than clicking through the native confirm
  dialog during automated testing, since that would've frozen the browser automation session,
  not because the delete path itself is untested.
- Fixed: image upload. `ObservationForm.tsx` now has a real `<input type="file" accept="image/*"
  capture="environment">` (opens the phone's rear camera directly on mobile) above the existing
  "Photo URL" field, with a local preview via `URL.createObjectURL` (revoked on change/unmount
  to avoid leaking blob URLs) and a "Remove photo" button. The URL field stays as a fallback —
  disabled and re-labeled once a file is chosen, so it's clear the file wins. Submit flow:
  create/update the observation first (unchanged), then — only if a file was chosen — call the
  new `uploadObservationPhoto(id, file)` (`services/pestDiseaseApi.ts`, posts `FormData` to
  `POST /api/observations/{id}/photo`) and use *its* returned `Observation` for `onSaved`. Known
  limitation, not fixed here: if the create/update call succeeds but the photo upload fails
  afterward, the shared `submitError` message ("Could not save this report") is misleading — the
  report *was* saved, just without a photo. Not handled specially; a retry from the form would
  create a second observation rather than resuming the first.
- **Fixed: real-world 415 on upload, found by the user testing through the actual UI (not
  caught by this session's own testing, which had used curl's multipart — curl sets its own
  correct `Content-Type` automatically, unaffected by this bug).** `axiosInstance` sets a
  default `Content-Type: application/json` header on the whole instance
  (`src/api/axiosInstance.ts`). Unlike an unset header, axios does not override an *explicitly
  set* one just because the request body is `FormData` — so the multipart boundary axios would
  normally generate never got added, and the backend received a request declaring JSON with an
  actual multipart body, which `[ApiController]` rejects with 415. Fixed in
  `uploadObservationPhoto` by passing `headers: { 'Content-Type': undefined }` per-request,
  which lets the browser set the real multipart header (with boundary) itself. Verified two
  ways: through the real form (login as the actual `farmer1` account, opened "Report a
  problem" — modal, cultivation-cycle dropdown, and file input all render and populate
  correctly) and, since this browser session's file-attach tool can't select arbitrary local
  files, by reproducing the exact bug and fix with a Node script using the project's real
  `axios` package against the live backend: identical setup without the header override → 415
  (reproduces the reported bug exactly); with it → 200 and a real blob URL back.
- Not yet tested against a real, running backend + live Gemini call from the browser (only
  route-level smoke-tested: pages load, redirect correctly when unauthenticated, no console
  errors) — see backend-guide.md's "real-Gemini smoke test" item.
- **Fixed: knowledge base `Category` field (2026-09-25)**, matching the backend's `Category`
  addition (see backend-guide.md). `KnowledgeEntryForm.tsx` gained a `Category` select
  (Pest/Disease, defaulting to `Pest` for new entries) placed right after the Name field,
  following the exact same `<select>` + const-array-map pattern as `ObservationForm.tsx`'s
  `observationType`/`severity` selects — no new UI pattern invented.
  `KnowledgeBasePage.tsx` gained a client-side category filter (`pd-queue-controls` /
  `pd-form-group pd-queue-filter`, the same classes and shape `PestDiseaseReportsPage.tsx`
  already uses for its status filter — filtering happens in-memory over the already-fetched
  list rather than adding a server round trip, since `GET /api/pest-disease-knowledge` has no
  category query param) and a Category column in the table.

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
