import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import '../../services/auth_service.dart';
import '../../screens/login_screen.dart';
import '../../screens/register_screen.dart';
import '../../screens/dashboard_screen.dart';
import '../../features/reporting_approval/screens/pending_reviews_screen.dart';
import '../../features/reporting_approval/screens/plan_status_screen.dart';
import '../../features/reporting_approval/screens/notifications_screen.dart';
import '../../features/profile/screens/profile_screen.dart';
import '../../features/field_cultivation/models/cycle_models.dart';
import '../../features/field_cultivation/models/field_models.dart';
import '../../features/field_cultivation/screens/cycle_detail_screen.dart';
import '../../features/field_cultivation/screens/field_detail_screen.dart';
import '../../features/field_cultivation/screens/field_form_screen.dart';
import '../../features/field_cultivation/screens/my_cycles_screen.dart';
import '../../features/field_cultivation/screens/my_fields_screen.dart';
import '../../features/field_cultivation/screens/plan_detail_screen.dart';
import '../../features/field_cultivation/screens/request_plan_screen.dart';
import '../../features/field_cultivation/screens/start_cycle_screen.dart';
import '../widgets/app_shell.dart';

final GlobalKey<NavigatorState> _rootNavigatorKey =
    GlobalKey<NavigatorState>(debugLabel: 'root');
final GlobalKey<NavigatorState> _shellNavigatorKey =
    GlobalKey<NavigatorState>(debugLabel: 'shell');

/// The centralized GoRouter configuration for Kumburu.
/// Uses ShellRoute to keep AppShell (app bar + persistent navigation drawer)
/// alive across all authenticated screens.
final GoRouter appRouter = GoRouter(
  navigatorKey: _rootNavigatorKey,
  initialLocation: '/dashboard',
  redirect: (BuildContext context, GoRouterState state) {
    final bool isAuthenticated = AuthService.isAuthenticated;
    final String path = state.uri.path;
    final bool isAuthRoute = path == '/login' || path == '/register';

    if (!isAuthenticated && !isAuthRoute) {
      return '/login';
    }

    if (isAuthenticated && isAuthRoute) {
      return '/dashboard';
    }

    return null;
  },
  routes: <RouteBase>[
    // Unauthenticated routes (outside of AppShell)
    GoRoute(
      path: '/login',
      builder: (BuildContext context, GoRouterState state) {
        return const LoginScreen();
      },
    ),
    GoRoute(
      path: '/register',
      builder: (BuildContext context, GoRouterState state) {
        return const RegisterScreen();
      },
    ),

    // Field & Cultivation forms open full-screen above the shell. Listed
    // before the shell so '/fields/new' is not read as a field id.
    GoRoute(
      path: '/fields/new',
      builder: (BuildContext context, GoRouterState state) =>
          const FieldFormScreen(),
    ),
    GoRoute(
      path: '/fields/:id/edit',
      builder: (BuildContext context, GoRouterState state) => FieldFormScreen(
        fieldId: int.parse(state.pathParameters['id']!),
        initial: state.extra as Field?,
      ),
    ),
    GoRoute(
      path: '/fields/:id/start-cycle',
      builder: (BuildContext context, GoRouterState state) => StartCycleScreen(
        fieldId: int.parse(state.pathParameters['id']!),
        field: state.extra as Field?,
      ),
    ),
    GoRoute(
      path: '/cycles/:id/plans/new',
      builder: (BuildContext context, GoRouterState state) => RequestPlanScreen(
        cycleId: int.parse(state.pathParameters['id']!),
        cycle: state.extra as CultivationCycle?,
      ),
    ),

    // Authenticated shell routes (persistent AppShell drawer & app bar)
    ShellRoute(
      navigatorKey: _shellNavigatorKey,
      builder: (BuildContext context, GoRouterState state, Widget child) {
        return AppShell(child: child);
      },
      routes: <RouteBase>[
        GoRoute(
          path: '/dashboard',
          builder: (BuildContext context, GoRouterState state) {
            return const DashboardScreen(embeddedInShell: true);
          },
        ),
        GoRoute(
          path: '/officer/pending-reviews',
          builder: (BuildContext context, GoRouterState state) {
            return const PendingReviewsScreen();
          },
        ),
        GoRoute(
          path: '/farmer/plan-status',
          builder: (BuildContext context, GoRouterState state) {
            return const PlanStatusScreen();
          },
        ),
        GoRoute(
          path: '/farmer/notifications',
          builder: (BuildContext context, GoRouterState state) {
            return const NotificationsScreen();
          },
        ),
        GoRoute(
          path: '/officer/notifications',
          builder: (BuildContext context, GoRouterState state) {
            return const NotificationsScreen();
          },
        ),
        GoRoute(
          path: '/field-officer/notifications',
          builder: (BuildContext context, GoRouterState state) {
            return const NotificationsScreen();
          },
        ),
        GoRoute(
          path: '/admin/notifications',
          builder: (BuildContext context, GoRouterState state) {
            return const NotificationsScreen();
          },
        ),
        GoRoute(
          path: '/fields',
          builder: (BuildContext context, GoRouterState state) =>
              const MyFieldsScreen(),
        ),
        GoRoute(
          path: '/fields/:id',
          builder: (BuildContext context, GoRouterState state) =>
              FieldDetailScreen(fieldId: int.parse(state.pathParameters['id']!)),
        ),
        GoRoute(
          path: '/cycles',
          builder: (BuildContext context, GoRouterState state) =>
              const MyCyclesScreen(),
        ),
        GoRoute(
          path: '/cycles/:id',
          builder: (BuildContext context, GoRouterState state) =>
              CycleDetailScreen(cycleId: int.parse(state.pathParameters['id']!)),
        ),
        GoRoute(
          path: '/plans/:id',
          builder: (BuildContext context, GoRouterState state) =>
              PlanDetailScreen(planId: int.parse(state.pathParameters['id']!)),
        ),
        GoRoute(
          path: '/profile',
          builder: (BuildContext context, GoRouterState state) {
            return const ProfileScreen();
          },
        ),
      ],
    ),
  ],
);
