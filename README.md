# 🌾 PaddyWise-AI ("Kumbura" · කුඹුර)

> **Agentic AI Decision-Support & Agricultural Governance Platform for Sri Lankan Paddy Farming**

[![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![React 19](https://img.shields.io/badge/React-19.2-61DAFB?logo=react&logoColor=black)](https://react.dev/)
[![TypeScript](https://img.shields.io/badge/TypeScript-5.x-3178C6?logo=typescript&logoColor=white)](https://www.typescriptlang.org/)
[![Flutter](https://img.shields.io/badge/Flutter-3.x-02569B?logo=flutter&logoColor=white)](https://flutter.dev/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-Neon-4169E1?logo=postgresql&logoColor=white)](https://neon.tech/)
[![Google Gemini](https://img.shields.io/badge/Google_Gemini-Agentic_AI-8E75C2?logo=googlegemini&logoColor=white)](https://aistudio.google.com/)

---

## 📖 Table of Contents

- [Overview](#-overview)
- [The Sri Lankan Agronomic Context](#-the-sri-lankan-agronomic-context)
- [Human-in-the-Loop AI Governance](#-human-in-the-loop-ai-governance)
- [The 4 Business Components & AI Agents](#-the-4-business-components--ai-agents)
- [System Architecture](#-system-architecture)
- [Repository Structure](#-repository-structure)
- [User Roles & Access Control](#-user-roles--access-control)
- [Prerequisites](#-prerequisites)
- [Configuration & Secrets Setup](#-configuration--secrets-setup)
- [Running the Applications](#-running-the-applications)
- [API Reference](#-api-reference)
- [Design System & Frontend Styling](#-design-system--frontend-styling)
- [Contributing & Development Guidelines](#-contributing--development-guidelines)

---

## 🌾 Overview

**PaddyWise-AI** (brand name in UI: **Kumburu**, from the Sinhala word *කුඹුරු* for paddy fields) is an agentic AI decision-support and extension platform engineered specifically for Sri Lankan rice paddy farmers, agrarian extension officers, and field supervisors.

Smallholder paddy farmers face accelerating climate unpredictability, stringent chemical regulation, pest surges, and uneven access to timely expert advice. Extension officers are often responsible for thousands of farming families across entire agrarian service divisions. 

**Kumburu bridges this gap by unifying farmers and agricultural officers on a single collaborative platform:**
- Farmers register plots, track cultivation cycles, generate tailored stage-by-stage plans, log field activities, and report pest observations.
- Multimodal and reasoning AI agents generate agronomic recommendations and analyze field anomalies.
- **Deterministic domain validators** guarantee that all AI recommendations strictly conform to Department of Agriculture (DOA) rules (durations, banned substances, dosage limits) before any output reaches human eyes.
- **Agricultural Officers** review, amend, and sign off on plans and diagnostic reports through dedicated approval queues, maintaining institutional safety and human authority.

---

## 🇱🇰 The Sri Lankan Agronomic Context

PaddyWise-AI is deeply tailored to Sri Lankan paddy cultivation standards:

- **Cultivation Seasons**: Native support for both **Maha** (major wet season driven by the Northeast monsoon) and **Yala** (dry season driven by the Southwest monsoon).
- **DOA Paddy Varieties**: Pre-seeded with Department of Agriculture certified seed varieties categorized by maturation period:
  - *3-Month Varieties* (e.g., `Bg 300`, `At 307`) — 90-day cycles.
  - *3.5-Month Varieties* (e.g., `Bg 352`, `Bg 360`, `At 362`, `Bg 94-1`, `Bg 359`, `Bw 367`) — 105-day cycles.
- **Agrarian Service Divisions**: Tailored for regional division administration (e.g., *Medirigiriya*, *Nuwaragam Palatha*, *Tambuttegama*, *Ampara Central*, *Kurunegala West*).
- **Growth Stages**: Programmatic tracking across the 6 canonical stages of rice phenology:
  `Nursery` → `Tillering` → `Panicle Initiation` → `Flowering` → `Grain Filling` → `Harvest`.

---

## 🛡️ Human-in-the-Loop AI Governance

Unlike standard chatbot wrappers, PaddyWise-AI enforces a **strict 5-step agentic governance lifecycle**:

```
[01. Farmer Input] ──────► [02. Agent Reasoning] ──────► [03. Deterministic Validation]
Field & Cycle Setup        Gemini Multimodal / Tools      Code-level safety rules & caps
                                                                     │
                                                                     ▼
[05. Actionable Plan] ◄─── [04. Officer Sign-Off] ◄────── [Passed Validation Queue]
Delivered to Farmer        Review, Amend, or Reject       Awaiting Agricultural Officer
```

1. **Farmer Input & Objective**: The farmer sets up a cycle or reports crop observations. User input is strictly treated as untrusted text, enclosed within isolated prompt blocks.
2. **AI Agent Tool Calling**: The agent gathers contextual data using strictly read-only, cycle-scoped database queries (`AsNoTracking`).
3. **Deterministic Safety Validation**: All agent outputs pass through programmatic C# validators (`CultivationPlanValidator`, `ActivitySafetyValidator`) to check date chronologies, stage order, and chemical safety rules.
4. **Institutional Officer Approval**: Validated proposals enter the Agricultural Officer's queue with approval states: `Draft`, `PendingOfficerApproval`, `RevisionRequested`, `Approved`, `Rejected`.
5. **Execution & Feedback**: Approved recommendations become actionable task schedules with audit logging in `AgentRunLog` and `DiagnosisRunLog`.

---

## 🤖 The 4 Business Components & AI Agents

The platform is architected into four distinct business components, each backed by dedicated domain entities, API endpoints, UI features, and specialized agents:

| # | Business Component | Responsible Agent | Status | Key Features |
|---|--------------------|-------------------|:------:|--------------|
| **1** | **Field & Cultivation Management** | `CultivationPlanningAgent` *(Workflow Coordinator)* | **Production Ready** | Field registration, season/variety tracking, stage timelines, AI crop calendar generation, deterministic schedule validator, officer plan sign-off. |
| **2** | **Crop Activity & Resource Management** | `ResourceAnalysisAgent` *(Crop Activity & Action)* | **Production Ready** | Daily activity logging (Irrigation, Fertilizer, Pesticide, Weed Management), activity safety validation, chemical interval checking, anomaly analysis. |
| **3** | **Pest & Disease Monitoring** | `CropAnalysisAgent` *(Visual & Symptom Diagnosis)* | **Production Ready** | Farmer symptom submissions, image upload with Gemini multimodal visual diagnosis, DOA knowledge base matching, severity triage, officer review. |
| **4** | **Reporting, Dashboards & Approval** | `SchedulingValidationAgent` *(Extension Governance)* | **Active / Extensible** | Multi-role dashboards (Farmer, Agri Officer, Field Officer, Admin), plan approval workbench, diagnosis review queue, user access control, audit logging. |

### Component Deep-Dive

#### 1. Cultivation Planning Agent (Component 1)
- Evaluates the field size, division, season, and DOA variety duration.
- Generates a stage-by-stage calendar spanning Nursery through Harvest with precise task windows (`windowStart`, `windowEnd`), tasks, and agronomic rationales.
- Delegates resource and pest monitoring responsibilities to peer agents.
- See detailed specification in [`docs/cultivation-planning-agent.md`](docs/cultivation-planning-agent.md).

#### 2. Resource Analysis Agent (Component 2)
- Analyzes logged activities (`CropActivity`) against recommended management guidelines.
- Detects premature chemical applications, nitrogen over-fertilization, or missing water regimes.
- Programmatic `ActivitySafetyValidator` ensures chemical safety intervals are maintained between sprays.

#### 3. Pest & Disease Diagnosis Agent (Component 3)
- Accepts field observations (`CropObservation`) with images and farmer-reported symptoms.
- Uses Gemini multimodal vision to cross-reference visual patterns with known Sri Lankan paddy pathologies: *Brown Plant Hopper (Nilaparvata lugens)*, *Rice Blast (Magnaporthe oryzae)*, *Bacterial Leaf Blight (Xanthomonas oryzae)*, *Sheath Blight (Rhizoctonia solani)*, *Paddy Stem Borer (Scirpophaga incertulas)*.
- Yields confidence metrics, immediate cultural safety measures, and chemical treatment options requiring officer sign-off.

#### 4. Extension Reporting & Dashboard (Component 4)
- Tailored operational views per role:
  - **Farmer**: Quick field stats, current growth stage timeline, pending tasks, report submission.
  - **Agricultural Officer**: Pending plan approvals, incoming disease reports, division oversight.
  - **Field Officer**: Active cycle monitoring, inspection logs, farmer assistance.
  - **Admin**: User account verification, system configuration, audit logs.

---

## 🏛️ System Architecture

```
                       ┌────────────────────────────────────────────────────────┐
                       │                   CLIENT INTERFACES                    │
                       │                                                        │
                       │   React 19 + TypeScript SPA       Flutter 3 Mobile App │
                       │    (Vite · Port 5173)              (Android / iOS)     │
                       └───────────────────┬───────────────────┬────────────────┘
                                           │                   │
                                           │ HTTP / JWT        │ HTTP / JWT
                                           ▼                   ▼
                       ┌────────────────────────────────────────────────────────┐
                       │              ASP.NET CORE 8 WEB API                    │
                       │              (PaddyWise.Api · Port 5164)               │
                       │                                                        │
                       │  ┌──────────────────────────────────────────────────┐  │
                       │  │ Controllers: Auth · Fields · Cycles · Activities  │  │
                       │  │              Plans · Observations · Reports       │  │
                       │  └────────────────────────┬─────────────────────────┘  │
                       │                           │                            │
                       │  ┌────────────────────────▼─────────────────────────┐  │
                       │  │ Deterministic Validators: Plan & Activity Safety │  │
                       │  └────────────────────────┬─────────────────────────┘  │
                       │                           │                            │
                       │  ┌────────────────────────▼─────────────────────────┐  │
                       │  │ Agent Orchestrator & Services (Scoped / Keyed)   │  │
                       │  └───────────────┬──────────────────┬───────────────┘  │
                       └──────────────────┼──────────────────┼──────────────────┘
                                          │                  │
                         SQL / Npgsql     │                  │  REST / Function Calling
                                          ▼                  ▼
┌───────────────────────────────────────────────┐  ┌────────────────────────────┐
│      Neon Serverless PostgreSQL Database      │  │      Google Gemini API     │
│                                               │  │                            │
│  - Users, Roles & Refresh Tokens              │  │  - Multimodal Vision       │
│  - Fields, Cycles & Growth Stage Logs         │  │  - Agronomic Reasoning     │
│  - JSONB Cultivation Plans & Activities       │  │  - JSON Schema Outputs     │
│  - Crop Observations & Disease Reports        │  │  - Dynamic Tool Calling    │
│  - AgentRunLogs & DiagnosisRunLogs (Audit)    │  │                            │
└───────────────────────────────────────────────┘  └────────────────────────────┘
```

---

## 📁 Repository Structure

```text
paddywise-ai/
├── paddywise-backend/            # ASP.NET Core 8 Web API
│   ├── Agents/                   # AI Agent orchestrators, tools & LLM clients
│   │   ├── FieldCultivation/     # CultivationPlanningAgent
│   │   ├── CropResource/         # ResourceAnalysisAgent & ActivitySafetyValidator
│   │   ├── PestDisease/          # CropAnalysisAgent (multimodal diagnosis)
│   │   └── Shared/               # IAgent, GeminiLlmClient, Tool definitions, Stubs
│   ├── Controllers/              # Role-authorized API controllers
│   ├── Data/                     # ApplicationDbContext, EF Core configuration, Seeds
│   ├── DTOs/                     # Strongly-typed request/response data contracts
│   ├── Entities/                 # Domain entities mapped to PostgreSQL
│   ├── Migrations/               # Entity Framework Core versioned database migrations
│   ├── Services/                 # Business logic services
│   ├── Program.cs                # Dependency injection, middleware & security pipeline
│   └── appsettings.json          # Public configurations (no secrets)
│
├── paddywise-web/                # Modern React 19 Single Page Application
│   ├── src/
│   │   ├── api/                  # Axios instance with 401 token refresh queue
│   │   ├── components/           # Protected routes, common navigation, layout
│   │   ├── context/              # AuthContext & session state management
│   │   ├── features/             # Feature modules (field-cultivation, crop-resource, pest-disease)
│   │   ├── pages/                # Role dashboards, login, registration, user management
│   │   ├── styles/               # Agronomic design system tokens (global.css)
│   │   └── utils/                # Role-based route redirection & helper utilities
│   ├── vite.config.ts            # Vite build configuration (Port 5173)
│   └── package.json
│
├── paddywise_mobile/             # Flutter 3 Cross-Platform Mobile Client
│   ├── lib/
│   │   ├── screens/              # Login, Registration, and Multi-Role Dashboard screens
│   │   ├── services/             # Authentication & session storage
│   │   ├── theme/                # Custom Kumburu brand theme
│   │   └── main.dart             # Application root
│   └── pubspec.yaml
│
├── database/
│   └── schema.sql                # Reference PostgreSQL database schema
├── docs/
│   └── cultivation-planning-agent.md # Specifications for Component 1 planning agent
├── CLAUDE.md                     # Engineering boundaries, coding rules & styling tokens
├── PROJECT_CONTEXT.md            # In-depth architectural context & entity definitions
└── README.md                     # This file
```

---

## 👥 User Roles & Access Control

The platform enforces 4 distinct roles, stored as an ordinal integer enum in PostgreSQL and serialized as clean strings in JWT claims:

| Role Name | Ordinal | Responsibilities & Access |
|---|:---:|---|
| **`Farmer`** | `0` | Registers fields, creates cultivation cycles, generates AI plans, logs daily activities, submits pest photos/symptoms. |
| **`AgriculturalOfficer`** | `1` | Reviews pending cultivation plans, inspects diagnostic reports, issues approved treatment plans, oversees agrarian divisions. |
| **`Admin`** | `2` | Manages user directory, assigns roles, configures system reference data, reviews audit trails. |
| **`FieldOfficer`** | `3` | On-the-ground extension worker who assists farmers with cycle verification, growth stage logging, and field visits. |

Authentication uses **JWT Bearer Access Tokens** (15-minute lifetime) paired with cryptographically secure, rotatable **Refresh Tokens** (7-day lifetime) stored in the database.

---

## ⚙️ Prerequisites

Before running the project locally, ensure you have the following installed:

1. **.NET 8 SDK** ([Download](https://dotnet.microsoft.com/download/dotnet/8.0))
   - Install EF Core CLI tool globally:
     ```bash
     dotnet tool install --global dotnet-ef
     ```
2. **Node.js (v20+) & npm** ([Download](https://nodejs.org/))
3. **Flutter SDK (v3.x / Dart 3.9+)** ([Download](https://docs.flutter.dev/get-started/install)) *(required only for mobile)*
4. **PostgreSQL Database** (e.g., [Neon Serverless Postgres](https://neon.tech/) or local PostgreSQL 14+)
5. **Google Gemini API Key** ([Google AI Studio](https://aistudio.google.com/apikey))

---

## 🔐 Configuration & Secrets Setup

> [!IMPORTANT]
> **No secrets are ever committed to this repository.** `appsettings.json` is tracked by git and contains empty placeholder strings for sensitive values.

Set your local configuration using the .NET user-secrets tool inside `paddywise-backend/`:

```bash
cd paddywise-backend

# 1. JWT Signing Key (Must be at least 32 characters, recommended 64-char hex)
dotnet user-secrets set "Jwt:Key" "YOUR_64_CHARACTER_RANDOM_SECRET_KEY_HERE_FOR_LOCAL_DEV"

# 2. PostgreSQL Connection String (Neon or local PostgreSQL)
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=your-postgres-host;Database=paddywise_db;Username=your_user;Password=your_password;SslMode=Require"

# 3. Google Gemini API Key
dotnet user-secrets set "Gemini:ApiKey" "YOUR_GOOGLE_AI_STUDIO_GEMINI_API_KEY"

# Optional: Dedicated Gemini Key for Pest & Disease Multimodal Diagnosis (falls back to Gemini:ApiKey if omitted)
dotnet user-secrets set "Gemini:PestDiseaseApiKey" "YOUR_PEST_DISEASE_GEMINI_KEY"
```

### Production / Container Deployment

In production environments (e.g., Azure App Service, Docker, Kubernetes), map nested configuration keys using double underscores (`__`):

| Configuration Key | Environment Variable |
|---|---|
| `Jwt:Key` | `Jwt__Key` |
| `ConnectionStrings:DefaultConnection` | `ConnectionStrings__DefaultConnection` |
| `Gemini:ApiKey` | `Gemini__ApiKey` |
| `Gemini:PestDiseaseApiKey` | `Gemini__PestDiseaseApiKey` |

---

## 🚀 Running the Applications

### 1. Database Migration

Apply EF Core migrations to create tables and seed default divisions and varieties:

```bash
cd paddywise-backend
dotnet ef database update
```

### 2. Start the Backend API

```bash
cd paddywise-backend
dotnet run
```
- **API Base URL**: `http://localhost:5164` (and `https://localhost:7188`)
- **Swagger Documentation**: `http://localhost:5164/swagger`
- *Note:* The API validates database connectivity on startup and verifies `Jwt:Key` length.

### 3. Start the Web Frontend (React 19)

```bash
cd paddywise-web
npm install
npm run dev
```
- **Web App URL**: `http://localhost:5173`
- *Note:* The backend CORS policy (`AllowReactApp`) and frontend Axios client are pre-configured for port **5173** and port **5164**.

### 4. Start the Mobile Client (Flutter)

```bash
cd paddywise_mobile
flutter pub get
flutter run
```

---

## 📡 API Reference

Core RESTful API routes exposed by `PaddyWise.Api`:

### Authentication & Users
- `POST /api/auth/register` — Register a new account (`Farmer`, `AgriculturalOfficer`, `FieldOfficer`, `Admin`).
- `POST /api/auth/login` — Authenticate and receive Access Token + Refresh Token.
- `POST /api/auth/refresh-token` — Exchange valid refresh token for a new access token.
- `POST /api/auth/revoke-token` — Invalidate user session and revoke refresh token.

### Field & Cultivation Management (Component 1)
- `GET /api/fields` — List registered fields for authenticated farmer.
- `POST /api/fields` — Register a new agrarian plot with acreage and division.
- `GET /api/cycles` — List cultivation cycles.
- `POST /api/cycles` — Start a new cycle (variety, season, sowing date, cultivation method).
- `POST /api/plans/generate` — Trigger `CultivationPlanningAgent` to produce a stage calendar.
- `GET /api/plans/{id}` — Fetch plan JSON, validation status, and approval audit log.
- `POST /api/plans/{id}/approve` — Officer approval endpoint.
- `POST /api/plans/{id}/reject` — Officer rejection with feedback commentary.

### Crop Activity & Resources (Component 2)
- `GET /api/cropactivities/cycle/{cycleId}` — List logged activities for a cycle.
- `POST /api/cropactivities` — Log field action (Fertilizer, Irrigation, Pesticide application).
- `POST /api/cropactivityanalysis/analyze` — Trigger `ResourceAnalysisAgent` to validate field safety.

### Pest & Disease Monitoring (Component 3)
- `GET /api/observations/cycle/{cycleId}` — Retrieve submitted crop observations.
- `POST /api/observations` — Submit symptom observation and photo for visual diagnosis.
- `POST /api/observations/{id}/diagnose` — Trigger multimodal `CropAnalysisAgent` diagnosis.
- `GET /api/pestdiseasereports` — Officer triage list for pending diagnostic reports.
- `PUT /api/pestdiseasereports/{id}/status` — Officer sign-off or status update.

### Agrarian Reference Data
- `GET /api/divisions` — Sri Lankan Agrarian Service Divisions list.
- `GET /api/varieties` — Certified DOA paddy seed varieties with durations.

---

## 🎨 Design System & Frontend Styling

The web client implements an agronomic, earth-toned design system defined in [`paddywise-web/src/styles/global.css`](file:///d:/Work/paddywise-ai/paddywise-web/src/styles/global.css):

### Color Tokens
- `--forest` (`#22392A`) & `--forest-deep` (`#182A1E`) — Primary deep evergreen tones.
- `--shoot` (`#7FA66C`) & `--shoot-light` (`#B4CB9C`) — Fresh paddy seedling accents.
- `--gold` (`#E1A63B`) & `--gold-deep` (`#C48A28`) — Ripe grain / harvest highlights.
- `--clay` (`#A6693F`) — Rich arable soil accents.
- `--cream` (`#F6F1E3`) & `--cream-deep` (`#EDE5CF`) — Warm parchment backgrounds.
- `--ink` (`#212D1E`) & `--ink-soft` (`#4B5645`) — High-contrast typography.

### Typography
- **Headings**: `Fraunces` (Editorial serif)
- **Body & Data**: `Work Sans` (Clean geometric sans-serif)

---

## 🤝 Contributing & Development Guidelines

1. **Git Flow**:
   - Create feature branches off `develop`: `git checkout -b feature/<feature-name>`.
   - Submit Pull Requests back into `develop`.
2. **Build Validation**:
   - Always verify builds before committing:
     - Backend: `cd paddywise-backend && dotnet build`
     - Frontend: `cd paddywise-web && npm run build`
     - Mobile: `cd paddywise_mobile && flutter analyze`
3. **Database Integrity**:
   - Never edit or delete an existing migration file. Always generate a new migration:
     ```bash
     dotnet ef migrations add <DescriptiveMigrationName>
     ```
4. **Secret Protection**:
   - Never commit sensitive keys, tokens, or connection strings into `appsettings.json` or source code.
5. **API Error Contract**:
   - Keep the standardized `{ message: string }` error response pattern on all 4xx/5xx API responses to ensure seamless frontend toast and error banner rendering.

---

## 📄 License & Academic Attribution

Developed as an advanced agricultural research and decision-support system for Sri Lankan smallholder paddy cultivation. All rights reserved. For architectural details and team ownership boundaries, refer to [`CLAUDE.md`](CLAUDE.md) and [`PROJECT_CONTEXT.md`](PROJECT_CONTEXT.md).
