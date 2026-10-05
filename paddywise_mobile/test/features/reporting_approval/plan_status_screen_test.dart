import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:paddywise_mobile/features/reporting_approval/screens/plan_status_screen.dart';
import 'package:paddywise_mobile/theme/app_theme.dart';

void main() {
  group('PlanStatusScreen Widget Tests', () {
    testWidgets('renders plan title, variety, and timeline progress', (tester) async {
      await tester.pumpWidget(
        MaterialApp(
          theme: AppTheme.theme,
          home: const PlanStatusScreen(),
        ),
      );

      await tester.pumpAndSettle();

      expect(find.text('CULTIVATION ADVISORY'), findsOneWidget);
      expect(find.text('Plan Status & AI Advice'), findsOneWidget);
      expect(find.text('Yala 2026 Season Plan'), findsOneWidget);
      expect(find.textContaining('Bg 352 (Nadu 3.5 Months)'), findsOneWidget);
      expect(find.textContaining('Tillering Phase'), findsOneWidget);
      expect(find.byType(LinearProgressIndicator), findsOneWidget);
    });

    testWidgets('displays status badge and officer feedback box', (tester) async {
      await tester.pumpWidget(
        MaterialApp(
          theme: AppTheme.theme,
          home: const PlanStatusScreen(),
        ),
      );

      await tester.pumpAndSettle();

      // Verify status badge
      expect(find.text('OFFICER APPROVED'), findsOneWidget);

      // Verify officer feedback note is displayed
      expect(find.text('Extension Officer Note:'), findsOneWidget);
      expect(find.textContaining('Recommended water level at 5cm'), findsOneWidget);
    });
  });
}
