# PaddyWise-AI ("Kumburu")

An agentic AI decision-support platform for Sri Lankan paddy farmers, connecting farmers and
agricultural/field officers on one system: a farmer registers a field, starts a cultivation
cycle and asks an AI agent for a stage-by-stage plan, which passes a deterministic validator
and then goes to an agricultural officer for approval. Four business components, one per team
member, each with its own agent — **1. Field & Cultivation Management** (*Cultivation Planning
Agent*, the workflow coordinator), **2. Crop Activity & Resource Management** (*Resource
Analysis & Action Agent*), **3. Pest & Disease Monitoring** (*Pest & Disease Diagnosis Agent*)
and **4. Reporting, Dashboards & Approval** (*Scheduling, Validation & Approval Agent*).
ASP.NET Core 8 + PostgreSQL, React 19 + TypeScript + Vite, Flutter. Component 1 is built end to
end; the other three agents are registered stubs today.

## Prerequisites

- .NET 8 SDK, plus `dotnet tool install --global dotnet-ef`
- Node.js 20+ and npm
- Flutter 3 (Dart SDK 3.9+) — only for the mobile app
- A Gemini API key from [Google AI Studio](https://aistudio.google.com/apikey)

## Setup

No secrets live in the repo. Set all three in the per-user secret store, from
`paddywise-backend/`. `Jwt:Key` and the connection string are **shared by the team** — ask for
the current values. `Gemini:ApiKey` is **your own**.

```bash
cd paddywise-backend
dotnet user-secrets set "Jwt:Key" "<64-char-random-secret-shared-by-the-team>"
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=<host>;Database=<db>;Username=<user>;Password=<password>;SslMode=Require"
dotnet user-secrets set "Gemini:ApiKey" "<your-own-ai-studio-key>"
dotnet ef database update
```

The API refuses to start if `Jwt:Key` is missing or under 32 characters. Deployment uses the
`Jwt__Key`, `ConnectionStrings__DefaultConnection` and `Gemini__ApiKey` environment variables
instead — see `paddywise-backend/README.md`.

## Running

```bash
cd paddywise-backend && dotnet run                # http://localhost:5164 (Swagger at /swagger)
cd paddywise-web && npm install && npm run dev    # http://localhost:5173
cd paddywise_mobile && flutter pub get && flutter run
```

The web app reaches the API only when the backend is on **5164** and Vite on **5173** — both
ports are hard-coded. The mobile app talks to no backend yet.

## Contributing

Branch per feature off `develop` (`git checkout -b feature/<name>`), then PR back into
`develop`. Build before you push: `dotnet build` in `paddywise-backend`, `npm run build` in
`paddywise-web`. Never add a secret to `appsettings.json` (it is tracked), and never edit an
existing migration — add a new one.

Conventions and ownership boundaries: [CLAUDE.md](CLAUDE.md). Component 1's agent — its tools,
validation rules and approval states: [docs/cultivation-planning-agent.md](docs/cultivation-planning-agent.md).
