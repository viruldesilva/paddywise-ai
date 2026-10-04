# Defect Report — Reporting, Dashboards & AI Approval Management

**Course:** SE3090 — Software Testing and Quality Evaluation  
**Assignment:** Assignment 2 — Software Testing and Quality Evaluation  
**Project:** PaddyWise AI (Smart Paddy Farming Advisory & Decision Support System)  
**Component:** Component 4 — Reporting, Dashboards & AI Approval Management  
**Component Owner:** Virul Akbo De Silva  
**Date:** October 4, 2026  

---

## 1. Automated Test Execution Defect Status
In the automated test run of all executable test suites (Backend .NET, Database InMemory, and Flutter Mobile), **0 tests failed** (`Failed: 0`, 100% pass rate).

However, during widget and static analysis of the existing frontend and mobile screens, **4 real code defects / design gaps** were identified. Per project guidelines, these defects are documented below with their root causes, evidence, and proposed remediation steps awaiting user confirmation before applying code changes.

---

## 2. Defect Log

| Defect ID | Description | Severity / Priority | Steps to Reproduce | Evidence | Status | Retest Result |
|---|---|---|---|---|---|---|
| **DEF-01** | **Hardcoded Static Notification List in Widget Build Method**<br>The Flutter `NotificationsScreen` defines a static list of 3 items directly within `build()`, preventing dynamic updates from `NotificationService` and making it impossible to render an empty state view when a farmer has zero notifications. | Medium / P2 | 1. Navigate to Notifications screen in mobile app.<br>2. Clear all notifications in storage or log in as a new farmer with 0 notifications.<br>3. Inspect screen. | `lib/features/reporting_approval/screens/notifications_screen.dart:L9-L34`<br>`final notifications = [ ... hardcoded items ... ];` | Open (Proposed fix documented) | Pending Confirmation |
| **DEF-02** | **Static "OFFICER APPROVED" Badge & Note in Plan Status Screen**<br>The Flutter `PlanStatusScreen` hardcodes the badge text `'OFFICER APPROVED'` with static green styling and a static feedback quote. When a plan is in `Rejected` or `Pending` status, the screen still misinforms the farmer that it is approved. | Medium / P2 | 1. Log in as farmer with a `Rejected` or `Pending` cultivation plan.<br>2. Open Plan Status screen (`/farmer/plan-status`).<br>3. Observe status header. | `lib/features/reporting_approval/screens/plan_status_screen.dart:L76-L82`<br>`child: const Text('OFFICER APPROVED')` | Open (Proposed fix documented) | Pending Confirmation |
| **DEF-03** | **Unrecognized Role Fallback Defaults to Full Farmer Permissions**<br>In `role_menu_config.dart`, when `getMenuItemsForRole` receives an unknown or null role, it returns `roleMenus['Farmer'] ?? []`, giving invalid or guest accounts access to farmer operational routes. | Low / P3 | 1. Call `getMenuItemsForRole('UnknownRole')`.<br>2. Inspect returned route list. | `lib/core/widgets/role_menu_config.dart:L150`<br>`// Fallback default`<br>`return roleMenus['Farmer'] ?? [];` | Open (Proposed fix documented) | Pending Confirmation |
| **DEF-04** | **Syntax Compilation Error in Teammate Shared Dashboard Screen**<br>`dashboard_screen.dart` contains an extraneous argument in `Column(...)` on line 614 and a bracket syntax error on line 1093, preventing compilation when imported via `app_router.dart`. | High / P1 | 1. Import `package:paddywise_mobile/screens/dashboard_screen.dart`.<br>2. Run `flutter test`. | Compiler error: `Too many positional arguments: 0 allowed, but 1 found` on line 614. | Open (Isolated in tests) | Pending Teammate Patch |

---

## 3. Proposed Fixes (Awaiting Confirmation)

### Proposed Fix for DEF-01 (`notifications_screen.dart`)
- **Root Cause:** Static mock data hardcoded in `build()`.
- **Proposed Solution:** Refactor `NotificationsScreen` to read from `NotificationService.getNotificationsForUser()`:
  ```dart
  final notifications = NotificationService.getNotificationsForUser(currentUser.id);
  if (notifications.isEmpty) {
    return const Center(child: Text('No notifications yet. You will receive updates once your plans or diagnoses are reviewed.'));
  }
  ```

### Proposed Fix for DEF-02 (`plan_status_screen.dart`)
- **Root Cause:** Hardcoded strings in container children.
- **Proposed Solution:** Accept an optional `PlanStatus` parameter or retrieve active plan from storage/API, binding badge color dynamically:
  - If `Approved`: `AppColors.successBg` / `'OFFICER APPROVED'`
  - If `Rejected`: `AppColors.errorBg` / `'REJECTED'`
  - If `Pending`: `AppColors.gold.withAlpha(40)` / `'PENDING REVIEW'`
  - Display actual `officerComment` instead of hardcoded text.

### Proposed Fix for DEF-03 (`role_menu_config.dart`)
- **Root Cause:** Line 150 defaults to `roleMenus['Farmer']`.
- **Proposed Solution:** Return a minimal guest menu (`[Dashboard, Profile]`) or empty list:
  ```dart
  // Fallback default: minimal navigation
  return [
    const MenuItemConfig(label: 'Home / Dashboard', icon: Icons.dashboard_outlined, route: '/dashboard'),
    const MenuItemConfig(label: 'Profile', icon: Icons.person_outline, route: '/profile'),
  ];
  ```
