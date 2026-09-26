import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:paddywise_mobile/features/crop-resource/screens/past_activities_screen.dart';
import 'package:paddywise_mobile/features/crop-resource/widgets/activity_card.dart';
import 'package:paddywise_mobile/features/crop-resource/widgets/activity_detail_sheet.dart';
import 'package:paddywise_mobile/theme/app_theme.dart';

void main() {
  group('PastActivitiesScreen & Crop Activities History Tests', () {
    testWidgets('renders PastActivitiesScreen with app bar, category pills, and activities', (tester) async {
      await tester.pumpWidget(
        MaterialApp(
          theme: AppTheme.theme,
          home: const PastActivitiesScreen(),
        ),
      );

      await tester.pumpAndSettle();

      // Check App Bar & Header
      expect(find.text('Crop Activities'), findsOneWidget);

      // Check Category Filter Pills
      expect(find.text('All'), findsOneWidget);
      expect(find.text('Fertilizer'), findsWidgets);
      expect(find.text('Irrigation'), findsWidgets);
      expect(find.text('Pesticide'), findsWidgets);
      expect(find.text('Other'), findsWidgets);

      // Check Floating Action Button / Record button
      expect(find.text('Record Activity'), findsWidgets);

      // Check seeded demo activity cards
      expect(find.byType(ActivityCard), findsWidgets);
      expect(find.textContaining('Urea'), findsOneWidget);
      expect(find.textContaining('Canal'), findsOneWidget);
    });

    testWidgets('filters activities when category pill is tapped', (tester) async {
      await tester.pumpWidget(
        MaterialApp(
          theme: AppTheme.theme,
          home: const PastActivitiesScreen(),
        ),
      );

      await tester.pumpAndSettle();

      // Initially all types are present
      expect(find.textContaining('Urea'), findsOneWidget);
      expect(find.textContaining('Canal'), findsOneWidget);

      // Tap on Fertilizer pill (first widget with 'Fertilizer' text in the pill list)
      final fertilizerPill = find.widgetWithText(InkWell, 'Fertilizer');
      if (fertilizerPill.evaluate().isNotEmpty) {
        await tester.tap(fertilizerPill.first);
      } else {
        await tester.tap(find.text('Fertilizer').first);
      }
      await tester.pumpAndSettle();

      // Fertilizer is present, but Irrigation is filtered out
      expect(find.textContaining('Urea'), findsOneWidget);
      expect(find.textContaining('Canal'), findsNothing);

      // Tap on Irrigation pill
      final irrigationPill = find.widgetWithText(InkWell, 'Irrigation');
      if (irrigationPill.evaluate().isNotEmpty) {
        await tester.tap(irrigationPill.first);
      } else {
        await tester.tap(find.text('Irrigation').first);
      }
      await tester.pumpAndSettle();

      // Irrigation is present, Fertilizer is filtered out
      expect(find.textContaining('Canal'), findsOneWidget);
      expect(find.textContaining('Urea'), findsNothing);
    });

    testWidgets('filters activities using search input', (tester) async {
      await tester.pumpWidget(
        MaterialApp(
          theme: AppTheme.theme,
          home: const PastActivitiesScreen(),
        ),
      );

      await tester.pumpAndSettle();

      // Enter search query
      final searchField = find.byType(TextField);
      expect(searchField, findsOneWidget);

      await tester.enterText(searchField, 'Chlorantraniliprole');
      await tester.pumpAndSettle();

      // Only Pesticide with Chlorantraniliprole is shown in cards
      expect(find.byType(ActivityCard), findsOneWidget);
      expect(
        find.descendant(
          of: find.byType(ActivityCard),
          matching: find.textContaining('Chlorantraniliprole'),
        ),
        findsOneWidget,
      );
      expect(find.textContaining('Urea'), findsNothing);
      expect(find.textContaining('Canal'), findsNothing);

      // Clear search
      await tester.enterText(searchField, '');
      await tester.pumpAndSettle();

      expect(find.textContaining('Urea'), findsOneWidget);
    });

    testWidgets('tapping an activity card opens ActivityDetailSheet', (tester) async {
      await tester.pumpWidget(
        MaterialApp(
          theme: AppTheme.theme,
          home: const PastActivitiesScreen(),
        ),
      );

      await tester.pumpAndSettle();

      // Find first ActivityCard and tap it
      final firstCard = find.byType(ActivityCard).first;
      await tester.tap(firstCard);
      await tester.pumpAndSettle();

      // Check that bottom sheet is displayed
      expect(find.byType(ActivityDetailSheet), findsOneWidget);
      expect(find.text('CONTEXT & FIELD LOCATION'), findsOneWidget);
      expect(find.text('Cultivation Cycle'), findsOneWidget);
    });

    testWidgets('tapping delete button prompts confirmation dialog', (tester) async {
      await tester.pumpWidget(
        MaterialApp(
          theme: AppTheme.theme,
          home: const PastActivitiesScreen(),
        ),
      );

      await tester.pumpAndSettle();

      // Find delete button
      final deleteButtons = find.byIcon(Icons.delete_outline_rounded);
      expect(deleteButtons, findsWidgets);

      await tester.tap(deleteButtons.first);
      await tester.pumpAndSettle();

      // Confirmation dialog should be displayed
      expect(find.text('Delete Activity'), findsOneWidget);
      expect(find.text('Cancel'), findsOneWidget);
      expect(find.text('Delete'), findsOneWidget);

      // Tap Cancel
      await tester.tap(find.text('Cancel'));
      await tester.pumpAndSettle();

      // Dialog closed
      expect(find.text('Are you sure you want to remove this'), findsNothing);
    });
  });
}
