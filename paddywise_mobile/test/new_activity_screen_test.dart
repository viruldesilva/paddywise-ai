import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:paddywise_mobile/features/crop-resource/screens/new_activity_screen.dart';
import 'package:paddywise_mobile/theme/app_theme.dart';

void main() {
  group('NewActivityScreen & Crop Activities Tests', () {
    testWidgets('renders NewActivityScreen with header, tabs, and Fertilizer form by default', (tester) async {
      await tester.pumpWidget(
        MaterialApp(
          theme: AppTheme.theme,
          home: const NewActivityScreen(),
        ),
      );

      await tester.pumpAndSettle();

      // Check header elements
      expect(find.text('Dashboard'), findsOneWidget);
      expect(find.text('RECORD ACTIVITY'), findsOneWidget);
      expect(find.text('Back to Cultivation Cycle'), findsOneWidget);

      // Check activity type tabs
      expect(find.text('Fertilizer'), findsWidgets);
      expect(find.text('Irrigation'), findsOneWidget);
      expect(find.text('Pesticide'), findsOneWidget);
      expect(find.text('Other'), findsOneWidget);

      // Check Fertilizer fields
      expect(find.text('Fertilizer Activity Form'), findsOneWidget);
      expect(find.text('Select fertilizer type'), findsOneWidget);
      expect(find.text('Save Fertilizer Activity'), findsOneWidget);
    });

    testWidgets('switches to Irrigation form when Irrigation tab is tapped', (tester) async {
      await tester.pumpWidget(
        MaterialApp(
          theme: AppTheme.theme,
          home: const NewActivityScreen(),
        ),
      );

      await tester.pumpAndSettle();

      // Tap on Irrigation tab
      await tester.tap(find.text('Irrigation'));
      await tester.pumpAndSettle();

      // Verify Irrigation fields are present
      expect(find.text('Irrigation Activity Form'), findsOneWidget);
      expect(find.text('e.g. 5'), findsOneWidget); // Water level hint
      expect(find.text('e.g. 2'), findsOneWidget); // Duration hint
      expect(find.text('Select water source'), findsOneWidget);
      expect(find.text('Save Irrigation Activity'), findsOneWidget);
    });

    testWidgets('switches to Pesticide and Other tabs', (tester) async {
      await tester.pumpWidget(
        MaterialApp(
          theme: AppTheme.theme,
          home: const NewActivityScreen(),
        ),
      );

      await tester.pumpAndSettle();

      // Tap on Pesticide
      await tester.tap(find.text('Pesticide'));
      await tester.pumpAndSettle();

      expect(find.text('Pesticide Activity Form'), findsOneWidget);
      expect(find.text('e.g. Chlorantraniliprole'), findsOneWidget);
      expect(find.text('e.g. Stem Borer'), findsOneWidget);
      expect(find.text('Select application method'), findsOneWidget);

      // Tap on Other
      await tester.tap(find.text('Other'));
      await tester.pumpAndSettle();

      expect(find.text('Other Activity Form'), findsOneWidget);
      expect(find.text('Select activity type'), findsOneWidget);
      expect(find.text('Add optional activity notes...'), findsOneWidget);
    });

    testWidgets('shows validation errors when submitting empty required fields', (tester) async {
      await tester.pumpWidget(
        MaterialApp(
          theme: AppTheme.theme,
          home: const NewActivityScreen(),
        ),
      );

      await tester.pumpAndSettle();

      // Tap Save Fertilizer Activity without filling required fields
      final saveBtn = find.text('Save Fertilizer Activity');
      await tester.ensureVisible(saveBtn);
      await tester.tap(saveBtn);
      await tester.pumpAndSettle();

      // Verify validation warning banner is displayed
      expect(
        find.text('Please correct the highlighted fields before saving the activity.'),
        findsOneWidget,
      );
      expect(find.text('Please select a fertilizer type.'), findsOneWidget);
    });
  });
}
