# Component 2 — Crop Activity & Resource: live-backend tests

Files for the tests that need the real backend (`dotnet run --launch-profile http` in `paddywise-backend`).

| File | Purpose |
|---|---|
| `PaddyWise_Component2.postman_collection.json` | 34 requests: activity create / list / filter / update / delete with validation and role checks, the Resource Analysis Agent, the officer recommendation queue and review, agent audit trail, security checks, cleanup |
| `PaddyWise_Component2_Local.postman_environment.json` | Base URL and the farmer / officer logins (replace the placeholders; never commit real passwords) |
| `load-test-cropresource.js` | k6 load test: 50 virtual users for 5 minutes on activity history and the officer review queue |

```bash
# Postman, headless
npx newman run docs/testing/crop-resource/PaddyWise_Component2.postman_collection.json \
  -e docs/testing/crop-resource/PaddyWise_Component2_Local.postman_environment.json

# k6
cd docs/testing/crop-resource
k6 run -e FARMER_EMAIL=<farmer> -e FARMER_PASSWORD=<pw> -e OFFICER_EMAIL=<officer> -e OFFICER_PASSWORD=<pw> \
  --summary-export=perf-summary.json load-test-cropresource.js
```

The automated tests (xUnit, Flutter, Vitest) need no secrets; see `paddywise-backend/Docs/CropResource/test-cases.md`.
