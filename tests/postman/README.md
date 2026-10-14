# PaddyWise API tests (Postman / Newman)

| File | Purpose |
|---|---|
| `PaddyWise.postman_collection.json` | 99 requests in 8 folders, each with assertions |
| `PaddyWise.local.postman_environment.json` | `baseUrl`, `adminEmail`, `adminPassword` |
| `build-collection.js` | Readable source; `node build-collection.js` regenerates both JSON files |

## Before running

1. Start the backend with the http profile: `cd paddywise-backend && dotnet run --launch-profile http`
2. In the environment, set `adminEmail` and `adminPassword` to an existing Admin account.
   The run approves a newly registered officer, so it needs an Admin.

Each run registers new users with timestamped emails, so you can run it repeatedly.
It writes those users, fields and cycles to whatever database the backend uses.

## Run in Postman

Import both JSON files and select the **PaddyWise Local** environment.
Then open **Run collection** and run all folders **in order**, because later folders use ids saved by earlier ones.

## Run with Newman (for report evidence)

```bash
npm i -g newman newman-reporter-htmlextra
newman run PaddyWise.postman_collection.json -e PaddyWise.local.postman_environment.json \
  --env-var adminEmail=<admin> --env-var adminPassword=<password> \
  -r cli,htmlextra,junit \
  --reporter-htmlextra-export results/newman-report.html \
  --reporter-junit-export results/newman-junit.xml
```

## Checks on every request (collection-level)

- Responses must arrive within 2 s. Requests tagged `[LLM]` get 60 s. Change this with `responseLimitMs` / `llmResponseLimitMs`.
- No response may be a 5xx.
- 4xx bodies must use `{ message }` or validation `{ errors }`, and must not include a stack trace.

`[LLM]` requests call Gemini. Their assertions branch on whether the plan came back
`PendingOfficerApproval` or `ValidationFailed`, so the run stays green if the model output fails validation.
