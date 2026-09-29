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
import '../../features/pest_disease/models/observation.dart';
import '../../features/pest_disease/screens/observations_list_screen.dart';
import '../../features/pest_disease/screens/new_observation_screen.dart';
import '../../features/pest_disease/screens/observation_detail_screen.dart';
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

    // Pushed on top of the shell (own AppBar + back arrow, no drawer) — a create/edit form
    // and a detail view aren't drawer destinations themselves; only the list screen inside
    // the ShellRoute below is.
    GoRoute(
      path: '/farmer/observations/new',
      builder: (BuildContext context, GoRouterState state) {
        return NewObservationScreen(editing: state.extra as Observation?);
      },
    ),
    GoRoute(
      path: '/farmer/observations/:id',
      builder: (BuildContext context, GoRouterState state) {
        final id = int.parse(state.pathParameters['id']!);
        return ObservationDetailScreen(observationId: id);
      },
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
          path: '/farmer/observations',
          builder: (BuildContext context, GoRouterState state) {
            return const ObservationsListScreen();
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
          path: '/profile',
          builder: (BuildContext context, GoRouterState state) {
            return const ProfileScreen();
          },
        ),
      ],
    ),
  ],
);
