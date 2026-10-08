# Postman API Test Suite — Component 4: Reporting, Dashboards & AI Approval Management

**Course:** SE3090 — Software Testing and Quality Evaluation  
**Project:** PaddyWise AI (Smart Paddy Farming Advisory & Decision Support System)  
**Component:** Component 4 — Reporting, Dashboards & AI Approval Management  
**Artifact Version:** Postman Collection v2.1  

---

## 1. Overview

This test suite provides automated API and contract testing for **Component 4 (Reporting, Dashboards & AI Approval Management)** in `paddywise-backend`. It is structured for sequential execution in Postman Collection Runner and headless CI/CD execution via **Newman**, with automated JWT token chaining, administrative approval gating, role-based access control (RBAC), AI revision draft suggestions, notifications data isolation, and health monitoring.

### Test Files in this Directory

| File | Purpose | Base URL |
|---|---|---|
| [`PaddyWise_Component4.postman_collection.json`](./PaddyWise_Component4.postman_collection.json) | Complete 35-request Postman v2.1 collection | Dynamically uses `{{baseUrl}}` |
| [`PaddyWise_Local.postman_environment.json`](./PaddyWise_Local.postman_environment.json) | Local development environment config | `http://localhost:5164/api` |
| [`PaddyWise_Azure.postman_environment.json`](./PaddyWise_Azure.postman_environment.json) | Azure cloud staging/production environment | `https://<placeholder-azure-domain>/api` |
| [`README.md`](./README.md) | Setup, configuration, and execution guide | — |

---

## 2. Environment Variables & Credentials Setup

Before running tests, configure the environment variables in Postman or update your local environment file with credentials for your test database.

> [!WARNING]
> **Security Warning:** Never commit real passwords, production secrets, or live JWT tokens to GitHub. The committed environment files contain only placeholder credentials.

### Environment Variable Reference

| Variable | Description | Initial Value |
|---|---|---|
| `baseUrl` | ASP.NET Core API root URL | `http://localhost:5164/api` (Local) / `https://<placeholder-azure-domain>/api` (Azure) |
| `adminEmail` | Test System Administrator account email | `admin@paddywise.lk` |
| `adminPassword` | Test System Administrator password | `AdminPassword123!` *(update if your seed differs)* |
| `officerEmail` | Pre-existing approved Agricultural Officer email | `officer@paddywise.lk` |
| `officerPassword` | Pre-existing approved Agricultural Officer password | `OfficerPassword123!` *(update if your seed differs)* |
| `farmerEmail` | Pre-existing Farmer account email | `farmer@paddywise.lk` |
| `farmerPassword` | Pre-existing Farmer account password | `FarmerPassword123!` *(update if your seed differs)* |
| `adminToken` | Stored JWT Bearer token for Administrator | *(Auto-populated by PM-04)* |
| `officerToken` | Stored JWT Bearer token for Agricultural Officer | *(Auto-populated by PM-05 / PM-21)* |
| `farmerToken` | Stored JWT Bearer token for Farmer | *(Auto-populated by PM-01 / PM-06)* |
| `refreshToken` | Active refresh token for rotation tests | *(Auto-populated and rotated by PM-01/07)* |
| `newOfficerId` | Database ID of newly registered pending officer | *(Auto-captured by PM-19)* |
| `notificationId` | ID of notification for mark-as-read test | *(Auto-captured by PM-29)* |

---

## 3. How to Import into Postman

1. Open **Postman Desktop** (or Postman Web).
2. Click **Import** (top left).
3. Select or drag-and-drop the following files from `docs/testing/postman/`:
   - `PaddyWise_Component4.postman_collection.json`
   - `PaddyWise_Local.postman_environment.json`
   - `PaddyWise_Azure.postman_environment.json`
4. In the top right environment dropdown, select **PaddyWise Local** (or **PaddyWise Azure**).
5. Verify that your backend server is running:
   ```bash
   cd paddywise-backend
   dotnet run
   ```

---

## 4. Running with the Postman Collection Runner

1. Click on the collection **PaddyWise Component 4 - Reporting, Dashboards & AI Approval Management**.
2. Click the **Run** button (top right).
3. Ensure **all 7 folders** are checked in their defined order:
   - `01 Auth`
   - `02 Validation`
   - `03 Authorization`
   - `04 Admin officer approval`
   - `05 Reviews`
   - `06 Notifications`
   - `07 Health`
4. Set **Iterations = 1** and **Delay = 50ms** (to prevent rate limits).
5. Click **Run PaddyWise Component 4**.
6. All 35 tests will execute sequentially with 100% automated token propagation.

---

## 5. Running via Newman CLI (Automated HTML Reports)

You can run the full collection from the terminal and generate an HTML report using Newman.

### Step 1: Install Newman and the HTML Extra Reporter
```bash
npm install -g newman newman-reporter-htmlextra
```

### Step 2: Execute against Local Backend
```bash
newman run docs/testing/postman/PaddyWise_Component4.postman_collection.json \
  -e docs/testing/postman/PaddyWise_Local.postman_environment.json \
  -r cli,htmlextra \
  --reporter-htmlextra-export docs/testing/postman/postman-report.html
```

### Step 3: Execute against Azure Cloud Backend
```bash
newman run docs/testing/postman/PaddyWise_Component4.postman_collection.json \
  -e docs/testing/postman/PaddyWise_Azure.postman_environment.json \
  -r cli,htmlextra \
  --reporter-htmlextra-export docs/testing/postman/postman-azure-report.html
```

The generated HTML report includes interactive pass/fail visualizations, request/response payloads, latency breakdowns, and individual assertion statuses.

---

## 6. Detailed Test Case Specification

The collection covers 35 test cases matching the project's test documentation convention (`PM-01` through `PM-35`):

| Test ID | Request Name | HTTP Method & Route | Auth / Role | Expected Status | Key Assertions & Behavior |
|---|---|---|---|---|---|
| **PM-01** | Register farmer | `POST /api/auth/register` | Anonymous | `200 OK` | Unique timestamped email; `requiresApproval=false`; saves `farmerToken` & `refreshToken`. |
| **PM-02** | Register officer (pending approval) | `POST /api/auth/register` | Anonymous | `200 OK` | Unique timestamped email; `requiresApproval=true`; null tokens; pending approval message. |
| **PM-03** | Login as officer that is still pending | `POST /api/auth/login` | Anonymous | `403 Forbidden` | Verifies gating: pending officer blocked before admin approval; message contains pending verification note. |
| **PM-04** | Login admin | `POST /api/auth/login` | Anonymous | `200 OK` | Authenticates admin; saves `adminToken` & `refreshToken`; role claim is `Admin`. |
| **PM-05** | Login officer | `POST /api/auth/login` | Anonymous | `200 OK` | Authenticates approved officer; saves `officerToken`; role claim is `AgriculturalOfficer`. |
| **PM-06** | Login farmer | `POST /api/auth/login` | Anonymous | `200 OK` | Authenticates farmer; saves `farmerToken`; role claim is `Farmer`. |
| **PM-07** | Refresh token | `POST /api/auth/refresh` | Anonymous | `200 OK` | Rotates refresh token; verifies new token differs from old token; updates environment. |
| **PM-08** | Login with wrong password | `POST /api/auth/login` | Anonymous | `401 Unauthorized` | Rejects bad credentials with `"Invalid email or password."`. |
| **PM-09** | GET /auth/me with farmer token | `GET /api/auth/me` | Bearer (Farmer) | `200 OK` | Verifies token claims: user name present and `role="Farmer"`. |
| **PM-10** | Register with empty email | `POST /api/auth/register` | Anonymous | `400 Bad Request` | Missing/null email handled cleanly; never returns 500. |
| **PM-11** | Register with invalid email | `POST /api/auth/register` | Anonymous | `400 Bad Request` | Malformed email handled cleanly; never returns 500. |
| **PM-12** | Register with invalid role | `POST /api/auth/register` | Anonymous | `400 Bad Request` | Invalid role (`SuperAdministrator`) rejected with `"Invalid role specified."`. |
| **PM-13** | Register with empty password | `POST /api/auth/register` | Anonymous | `400 Bad Request` | Null/empty password handled without unhandled 500 server crash. |
| **PM-14** | No token on /admin/officer-requests | `GET /api/admin/officer-requests` | None (Unauthenticated) | `401 Unauthorized` | Enforces authentication on admin endpoints. |
| **PM-15** | No token on /notifications | `GET /api/notifications` | None (Unauthenticated) | `401 Unauthorized` | Enforces authentication on user notifications feed. |
| **PM-16** | Farmer token on /admin/officer-requests | `GET /api/admin/officer-requests` | Bearer (Farmer) | `403 Forbidden` | Non-admin caller blocked by `[Authorize(Roles = "Admin")]`. |
| **PM-17** | Farmer token on draft-revision-comment | `GET /api/reviews/plans/1/draft-revision-comment` | Bearer (Farmer) | `403 Forbidden` | Farmer role blocked from officer review drafts by `[Authorize(Roles = "AgriculturalOfficer,Admin")]`. |
| **PM-18** | Tampered token on /notifications | `GET /api/notifications` | Bearer (Tampered) | `401 Unauthorized` | Pre-request script alters last byte of JWT signature; ASP.NET Core rejects tampered signature. |
| **PM-19** | GET pending officer requests (admin) | `GET /api/admin/officer-requests` | Bearer (Admin) | `200 OK` | Returns JSON array; locates the pending officer from PM-02 and sets `newOfficerId`. |
| **PM-20** | Approve the new officer (admin) | `POST /api/admin/officer-requests/{{newOfficerId}}/approve` | Bearer (Admin) | `200 OK` | Updates officer status to `Approved`; dispatches email notification. |
| **PM-21** | Login as the approved officer | `POST /api/auth/login` | Anonymous | `200 OK` | Officer can now log in successfully post-approval; saves active `officerToken`. |
| **PM-22** | Approve non-existent user 99999 | `POST /api/admin/officer-requests/99999/approve` | Bearer (Admin) | `404 Not Found` | Non-existent user ID returns `"Agricultural Officer account not found."`. |
| **PM-23** | Register second officer for rejection | `POST /api/auth/register` | Anonymous | `200 OK` | Registers a second officer candidate pending approval. |
| **PM-24** | GET pending requests to capture reject ID | `GET /api/admin/officer-requests` | Bearer (Admin) | `200 OK` | Finds second officer in pending queue and saves `secondOfficerId`. |
| **PM-25** | Reject second officer with reason | `POST /api/admin/officer-requests/{{secondOfficerId}}/reject` | Bearer (Admin) | `200 OK` | Updates status to `Rejected` with administrative reason text. |
| **PM-26** | GET draft revision comment for plan | `GET /api/reviews/plans/1/draft-revision-comment` | Bearer (Officer) | `200 OK` | AI drafts structured agronomic feedback (pros, risks, recommendations) for plan review. |
| **PM-27** | GET draft revision comment for report | `GET /api/reviews/reports/1/draft-revision-comment` | Bearer (Officer) | `200 OK` | AI drafts structured diagnostic feedback for pest/disease report review. |
| **PM-28** | GET draft revision comment with id -1 | `GET /api/reviews/plans/-1/draft-revision-comment` | Bearer (Officer) | `404 / 200 OK` | Graceful fault tolerance: missing plan returns safe fallback without unhandled 500 crash. |
| **PM-29** | GET notifications (farmer) | `GET /api/notifications` | Bearer (Farmer) | `200 OK` | Returns list of notifications for authenticated caller; saves first `notificationId`. |
| **PM-30** | GET unread-count | `GET /api/notifications/unread-count` | Bearer (Farmer) | `200 OK` | Returns `{ unreadCount: N }` scoped strictly to caller. |
| **PM-31** | Mark one notification as read | `PUT /api/notifications/{{notificationId}}/read` | Bearer (Farmer) | `200 OK` | Updates targeted notification's `isRead` flag to true. |
| **PM-32** | Mark another user's notification as read | `PUT /api/notifications/999999/read` | Bearer (Farmer) | `404 Not Found` | Multi-tenant isolation: caller cannot access or modify notifications belonging to others. |
| **PM-33** | Mark all as read | `PUT /api/notifications/read-all` | Bearer (Farmer) | `200 OK` | Bulk updates caller's unread notifications to `isRead = true`. |
| **PM-34** | Verify unread-count becomes 0 | `GET /api/notifications/unread-count` | Bearer (Farmer) | `200 OK` | Verifies `unreadCount` equals 0 after bulk mark-all-read. |
| **PM-35** | GET /health (outside /api) | `GET /health` | Anonymous | `200 OK` | Dynamic URL derivation (`baseUrl.replace(/\/api$/, '') + '/health'`); returns plain-text `"Healthy"`. |

---

## 7. Real Code Architecture Notes

1. **Health Check Routing:**  
   In `Program.cs`, the root health probe is mapped at `/health` outside the `/api` prefix:
   ```csharp
   app.MapHealthChecks("/health").AllowAnonymous();
   app.MapHealthChecks("/api/health", healthCheckOptions).AllowAnonymous();
   ```
   PM-35 uses a pre-request script to strip `/api` from `{{baseUrl}}` and call `http://localhost:5164/health` dynamically, guaranteeing portability between local and Azure environments.

2. **Fault-Tolerant Revision Comments:**  
   In `RevisionDraftService.cs`, if a plan or report is not found in the database, the service falls back to safe static text (`"Please review this plan and provide feedback."`) and returns HTTP 200 OK rather than throwing an unhandled exception, ensuring officers are never blocked in the field. Non-integer routes (e.g. `/plans/invalid-id/...`) fail ASP.NET Core's `{id:int}` route constraint and return HTTP 404.

3. **Multi-Tenant Notification Isolation:**  
   In `NotificationsController.cs`, `MarkAsRead(id)` checks `n.Id == id && n.UserId == callerId.Value`. If the notification belongs to another farmer or does not exist, it returns HTTP 404 Not Found, preventing user ID enumeration or unauthorized state modification.
