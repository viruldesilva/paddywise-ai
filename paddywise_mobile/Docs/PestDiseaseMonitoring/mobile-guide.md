# Mobile guide — Pest & Disease Monitoring (Component 3)

Scope: `paddywise_mobile/` only, for whoever is building or maintaining Component 3's mobile
UI — the farmer-facing report/photo/diagnosis flow. Backend conventions, the agent, and the
data model are documented separately in
`paddywise-backend/Docs/PestDiseaseMonitoring/backend-guide.md`; the web equivalent is in
`paddywise-web/Docs/PestDiseaseMonitoring/frontend-guide.md`. This mobile UI calls the same
`/api/observations` and `/api/cycles` endpoints as the web app and matches its wording/status
logic, so both apps stay consistent for a farmer moving between them.

## Where the code lives

```
lib/features/pest_disease/
  models/
    observation.dart              Observation, CreateObservationRequest, UpdateObservationRequest,
                                   the type/severity label maps, symptomsMaxLength
    pest_disease_report.dart      PestDiseaseReport, reportStatusLabels/reportStatusLabel
    cultivation_cycle_summary.dart minimal cycle shape for the pickers (id, fieldName, season, year, currentStage)
  services/
    observation_api_service.dart  every HTTP call this feature makes, plus the
                                   request-analysis timeout-then-poll logic
  screens/
    observations_list_screen.dart farmer's "My Reports" list
    new_observation_screen.dart   create/edit form (both modes, same widget)
    observation_detail_screen.dart symptoms/photo/reports + "Ask the diagnosis agent"
  widgets/
    observation_status_badge.dart computeObservationListStatus() + the chip widget
```

Follows the existing mobile app's conventions: plain `StatefulWidget`/`setState` (no
Provider/Riverpod/Bloc), the `http` package (no `dio`), `go_router` for routing,
`AuthService`/`UserStorage` (`lib/services/auth_service.dart`,
`lib/core/storage/user_storage.dart`) for the existing session — nothing new was introduced
there. The one new dependency is `image_picker` (`^1.1.2`, plus `http_parser` promoted from
transitive to direct since `observation_api_service.dart` uses its `MediaType` type directly
for the multipart upload's content-type).

The folder shape (`models/`, `services/`, `screens/`, `widgets/`) deliberately mirrors
`paddywise-web/src/features/pest-disease/{types,services,pages,components}` rather than the
flatter style `lib/features/reporting_approval/` uses today (raw `Map<String, dynamic>`
inline in the screen, no service class) — this feature needed typed, independently testable
models and an API service (see Tests below), which the flatter style doesn't support well.

## Endpoints used

| Call | Endpoint | Notes |
|---|---|---|
| `getObservations({cultivationCycleId})` | `GET /api/observations` | Already newest-first server-side; `cultivationCycleId` is the list screen's filter. |
| `getObservationById(id)` | `GET /api/observations/{id}` | Detail screen load, and the request-analysis poll fallback. |
| `createObservation(request)` | `POST /api/observations` | |
| `updateObservation(id, request)` | `PUT /api/observations/{id}` | Only while `reports.isEmpty` — the backend's edit lock; a 400 here surfaces the backend's own `{message}` verbatim. |
| `uploadPhoto(...)` | `POST /api/observations/{id}/photo` | `http.MultipartRequest`, no manual `Content-Type` header (the request sets its own multipart boundary). Client-side caps mirror the server's: ≤10MB, one of `image/jpeg`/`image/png`/`image/webp`/`image/gif`. |
| `requestAnalysis(id)` | `POST /api/observations/{id}/request-analysis` | See below. |
| `getCycles()` | `GET /api/cycles` | Role-scoped server-side to the caller's own cycles; used by both the create-form picker and the list's filter. |

Every non-2xx response is parsed the same way `AuthService._extractBackendError` already does
for auth (`{message}` first, then ASP.NET's `ValidationProblemDetails.errors`, then `title`,
then a generic fallback) and thrown as `ApiException(message)` — screens catch that and show
`e.message` directly, same pattern as the web's `extractApiErrorMessage`.

## Request-analysis: timeout, then poll

`request-analysis` can take up to ~240s server-side (`Gemini` `HttpClient.Timeout`).
`requestAnalysis(id)`:
1. Calls the endpoint directly with a 270s client timeout (comfortably above the server's own
   240s, so a normal successful run returns first).
2. A real HTTP error response (e.g. 400) is thrown straight through as `ApiException` — the
   caller shows it.
3. A network-level failure only (timeout, dropped connection, no connectivity — anything that
   isn't a real HTTP response) falls back to `pollUntilAnalyzed(id)`: `GET
   /api/observations/{id}` every 10s for up to 5 minutes until `lastAnalyzedAt` is set. A
   transient GET failure during the window is swallowed and retried on the next tick, since
   the whole point of polling is resilience to the same kind of flakiness that triggered it.
   If the window elapses still unanalysed, it throws a distinct "taking longer than expected"
   message — never a hard "failed", since the run may genuinely still be in progress
   server-side.

`ObservationApiService`'s constructor accepts `requestAnalysisTimeout`/`pollInterval`/
`pollWindow` overrides (defaulting to the real values above) so tests can use millisecond
durations instead of waiting minutes — see Tests.

## Screens

- **Observations list** (`/farmer/observations`, inside the drawer `ShellRoute`) —
  pull-to-refresh (`RefreshIndicator`), a cycle-filter dropdown built from `getCycles()`,
  loading/empty/error states, and a status badge per card from
  `computeObservationListStatus()`: "Not analysed yet" / "Analysed — no likely match" / "N
  possible issue(s) — awaiting officer review" / the officer's decision once any candidate has
  been reviewed (Approved beats Revision requested beats Rejected, since a farmer-facing list
  card shows one summary line even though each candidate can be reviewed independently — the
  detail screen shows every candidate's own status).
- **New Observation** (`/farmer/observations/new`, pushed outside the shell) — same widget
  serves create and edit; pass an `Observation` via `state.extra` to edit (cycle picker hidden,
  matching the backend's `UpdateObservationRequestDto` not taking a cycle id). Photo via
  `image_picker`'s camera or gallery source (`maxWidth: 1600, imageQuality: 85` — a phone
  camera photo is often 5-10MB; this keeps the upload fast on mobile data and well under the
  backend's 10MB cap, and the backend downscales to 1024px on its own side regardless, so
  this doesn't need to match that exactly), client-validated (size, extension) before upload.
  A photo failure *after* a successful create/update pops back with `{'observation': saved,
  'warning': message}` instead of treating the whole submit as failed — the same fix already
  applied on web's `ObservationForm.tsx` for the "report was saved, but the photo wasn't"
  case.
- **Observation Detail** (`/farmer/observations/:id`, pushed outside the shell) — symptoms,
  photo, cycle/field info, and either "Ask the diagnosis agent" (never analysed) / "…again"
  (analysed, no match) or the reports list, each with confidence as a percentage always paired
  with "possible match" (never "confirmed"), its own status label, and the officer's comment
  once reviewed. The edit pencil icon only shows while `reports.isEmpty`; if the backend still
  rejects an edit (raced with an analysis that just landed), its `{message}` shows in a
  `SnackBar` rather than the client assuming its own gate was authoritative.
  While a request-analysis call (including its poll fallback) is in flight, `_isAnalyzing`
  both swaps the body for the full-screen "Analysing…" state (so the button isn't even
  rendered to re-tap — backed by an explicit `if (_isAnalyzing) return;` guard at the top of
  `_requestAnalysis()` too, for a re-tap that lands before the next frame rebuilds it away)
  and wraps the screen in a `PopScope(canPop: !_isAnalyzing, ...)` that blocks the AppBar back
  arrow, the system back gesture, and go_router's own pop, with a `SnackBar` explaining why —
  a farmer leaving and reopening this screen mid-run must not be able to start a second
  analysis on the same observation while the first is still going.

## Navigation wiring (shared files, minimal changes)

- `lib/core/router/app_router.dart` — 3 `GoRoute`s: `/farmer/observations` inside the existing
  `ShellRoute`; `/farmer/observations/new` and `/farmer/observations/:id` as sibling top-level
  routes (root navigator — a plain `AppBar` + back arrow, no drawer, matching how
  `/login`/`/register` already sit outside the shell).
- `lib/core/widgets/role_menu_config.dart` — the `// TODO: 'Report Crop Problem' - awaiting
  Pest & Disease feature screen` placeholder under `'Farmer'` is now a real
  `MenuItemConfig('Report Crop Problem', ..., '/farmer/observations')`. No other role's menu
  changed.
- `android/app/src/main/AndroidManifest.xml` — added `android.permission.CAMERA`. `INTERNET`
  was already present in the debug/profile manifests Flutter uses for local runs, so no change
  needed there.
- `ios/Runner/Info.plist` — added `NSCameraUsageDescription` and
  `NSPhotoLibraryUsageDescription`.

## Running

```bash
cd paddywise_mobile
flutter pub get
flutter run          # backend must be reachable at :5164 — 10.0.2.2 on the Android emulator,
                      # localhost elsewhere; AuthService.apiBaseUrl already handles this
```

`flutter analyze` must be clean; `flutter test` must pass.

## Tests (`flutter test`)

- `test/features/pest_disease/models_test.dart` — JSON parsing for `Observation` (never
  analysed / analysed-no-match / has-candidates fixtures), `PestDiseaseReport`,
  `CultivationCycleSummary`, and `CreateObservationRequest`/`UpdateObservationRequest`
  `toJson()` round-trips.
- `test/features/pest_disease/observation_status_test.dart` — `computeObservationListStatus()`
  for all four list-card states, including the Approved-beats-others priority rule.
- `test/features/pest_disease/new_observation_form_test.dart` — widget tests against
  `NewObservationScreen`: empty symptoms blocked, >2000 characters blocked, and a valid submit
  with defaults reaching `createObservation` with the right payload. Uses a larger test surface
  (`tester.view.physicalSize = Size(1000, 3000)`) since the form is taller than the default
  800×600 test viewport — without it, `ListView`'s build cache extent never creates the
  elements past the fold (including the Submit button) and `find` reports them as missing.
  Every field with a `validator` (currently just symptoms) also needs a stable `Key` — an
  inline conditional item earlier in the same `ListView` (e.g. the cycle-selection error text)
  changes the list's length when it appears, which shifts every unkeyed sibling's position and
  makes Flutter treat them as brand-new widgets, silently resetting their internal state
  (a validated field's `errorText` included) on the very next rebuild.
- `test/features/pest_disease/observation_api_service_test.dart` — `http.testing.MockClient`
  (from the `http` package itself, no extra dependency) covering list/get/create/update/
  photo/cycles happy paths, a 400 edit-lock response surfacing the backend's exact message, and
  the request-analysis timeout-then-poll path (a thrown `http.ClientException` on the direct
  call, several "still unanalysed" polls, then a poll that returns `lastAnalyzedAt` set) using
  millisecond `pollInterval`/`pollWindow` overrides.

## Manual end-to-end verification (2026-09-29)

Ran against the real backend (`dotnet run`, `:5164`) with a QA farmer account
(`qa-farmer-cat-test@example.com`) and cultivation cycle already seeded from earlier backend
testing this project.

**Environment note:** no Android emulator or physical device was available in this
environment (`flutter devices` only listed Windows desktop and web/Chrome), and the Windows
desktop target needs a Visual Studio C++ toolchain that isn't installed here
(`flutter run -d windows` failed with "Unable to find suitable Visual Studio toolchain"). The
run below is on `flutter run -d chrome --web-port 5173` instead — same Dart code, same HTTP
calls, but not the actual Android/iOS build target the app ships on. Flutter Web's CanvasKit
renderer also doesn't expose file-input elements to browser automation the way a native app's
camera/gallery picker would, so the photo-attach step used the **web** app instead for
`image_picker`'s file-input limitation — see below.

1. Logged in as the farmer, opened the drawer, confirmed **"Report Crop Problem"** appears
   exactly where the old TODO placeholder was and navigates to `/farmer/observations`.
2. **List screen**: loaded real data from earlier test observations, each card's status badge
   matched `computeObservationListStatus()` exactly — "Not analysed yet", "Analysed — no
   likely match", "N possible issues — awaiting officer review" all observed correctly against
   live data (not fixtures).
3. **Create flow**: filled the form (cycle picker, "Disease" type, symptoms), submitted without
   a photo (image_picker's file-input dialog isn't reachable through this environment's browser
   automation on Flutter Web — a native OS file/camera dialog opens outside the page, which the
   photo-attach code path itself is already covered by the widget tests and by the backend's
   own multipart-upload tests from the web-app work). The new observation appeared at the top
   of the list immediately, correctly showing "Not analysed yet".
4. **Request analysis**: the in-app button could not be triggered through browser automation
   (Flutter Web's CanvasKit renderer doesn't expose real, clickable DOM elements, and this
   environment's click/coordinate tooling could not reliably reach it — see the flagged
   product-feedback item on this). Triggered the identical `POST
   .../request-analysis` call directly instead — the same call `requestAnalysis()` makes — and
   confirmed via the backend log that it returned 3 Disease-category candidates: Rice Blast
   (45%), Brown Spot (55%), Bacterial Leaf Blight (65%), all `PendingOfficerReview`.
5. **Cross-platform check**: logged into the **web** app as a freshly-approved
   AgriculturalOfficer account, opened "Review Pest/Disease Reports", found the exact 3
   candidates from step 4 in the queue, and approved the Rice Blast one.
6. Re-fetched the observation via `GET /api/observations/{id}` (what the mobile detail screen
   would do on refresh) and confirmed report id 10 now shows `"status":"Approved"` with
   `reviewedAt` set — exactly the shape `PestDiseaseReport.fromJson` parses and
   `computeObservationListStatus()`/the detail screen's per-report badge already render
   correctly (covered by the "approved wins" case in `observation_status_test.dart`).

**Not verified live**: the in-app tap on "Ask the diagnosis agent" and the "Analysing your
crop…" full-screen state's actual on-screen appearance, and the camera/gallery photo picker's
real OS dialog — both blocked by this session's environment (no emulator, and Flutter Web's
canvas rendering isn't automatable the way a native app or a DOM-based web app is). The
request-analysis network call itself, the timeout→poll fallback, and the photo-upload
multipart request are all covered by the automated test suite above against the same backend
contract used here.
