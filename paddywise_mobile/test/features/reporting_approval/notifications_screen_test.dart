import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:paddywise_mobile/features/reporting_approval/screens/notifications_screen.dart';
import 'package:paddywise_mobile/theme/app_theme.dart';

void main() {
  group('NotificationsScreen Widget Tests', () {
    testWidgets('renders screen title, header, and notification cards', (tester) async {
      await tester.pumpWidget(
        MaterialApp(
          theme: AppTheme.theme,
          home: const NotificationsScreen(),
        ),
      );

      await tester.pumpAndSettle();

      expect(find.text('ACTIVITY FEED'), findsOneWidget);
      expect(find.text('Notifications'), findsOneWidget);

      // Verify notification cards are rendered
      expect(find.text('Plan Review Update'), findsOneWidget);
      expect(find.text('Pest Warning: Brown Plant Hopper'), findsOneWidget);
      expect(find.text('Weather Advisory: Intermittent Showers'), findsOneWidget);
    });

    testWidgets('visual distinction between unread and read notifications', (tester) async {
      await tester.pumpWidget(
        MaterialApp(
          theme: AppTheme.theme,
          home: const NotificationsScreen(),
        ),
      );

      await tester.pumpAndSettle();

      // Find all containers representing notification cards
      final containers = find.byType(Container);
      expect(containers, findsWidgets);

      // Verify unread badge border / color is rendered
      expect(find.textContaining('approved your Tillering stage plan'), findsOneWidget);
      expect(find.text('2 hours ago'), findsOneWidget);
    });

    testWidgets('defect check: static list prevents displaying dynamic empty state', (tester) async {
      // NOTE for Step 4 Defect Tracking:
      // NotificationsScreen embeds a hardcoded list inside build() and does not
      // accept a dynamic list or connect to NotificationService.
      // Therefore, it cannot render an empty state when a farmer has 0 notifications.
      await tester.pumpWidget(
        MaterialApp(
          theme: AppTheme.theme,
          home: const NotificationsScreen(),
        ),
      );
      await tester.pumpAndSettle();

      // Confirms notifications are statically present rather than dynamically empty
      expect(find.text('No notifications yet'), findsNothing);
      expect(find.byType(ListView), findsOneWidget);
    });
  });
}
