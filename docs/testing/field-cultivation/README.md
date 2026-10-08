# Component 1 — Field & Cultivation: live-backend tests

Files for the tests that need the real backend (`dotnet run --launch-profile http` in `paddywise-backend`).

| File | Purpose |
|---|---|
| `PaddyWise_Component1.postman_collection.json` | 32 requests: auth, fields, cycles, stage logs, plan request → background generation (polled) → officer review, security checks, cleanup |
| `PaddyWise_Component1_Local.postman_environment.json` | Base URL and the farmer / officer logins (replace the placeholders; never commit real passwords) |
| `field-cultivation-load-test.js` | k6 load test: 25 virtual users, 2m45s, farmer and officer read endpoints |

```bash
# Postman, headless
npx newman run docs/testing/field-cultivation/PaddyWise_Component1.postman_collection.json \
  -e docs/testing/field-cultivation/PaddyWise_Component1_Local.postman_environment.json

# k6
cd docs/testing/field-cultivation
k6 run -e FARMER_EMAIL=<farmer> -e FARMER_PASSWORD=<pw> -e OFFICER_EMAIL=<officer> -e OFFICER_PASSWORD=<pw> \
  --summary-export=perf-summary.json field-cultivation-load-test.js
```

The automated tests (xUnit, Flutter, Vitest) need no secrets; see `paddywise-backend/Docs/FieldCultivation/test-cases.md`.
