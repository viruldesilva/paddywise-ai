import 'dart:convert';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:paddywise_mobile/features/crop-resource/models/crop_activity_models.dart';
import 'package:paddywise_mobile/features/crop-resource/screens/edit_activity_screen.dart';
import 'package:paddywise_mobile/theme/app_theme.dart';

void main() {
  group('EditActivityScreen Tests', () {
    final demoFertilizer = CropActivityDto(
      id: 301,
      cultivationCycleId: 1,
      activityType: 'Fertilizer',
      date: DateTime.now().subtract(const Duration(days: 2)).toIso8601String().split('T').first,
      detailsJson: jsonEncode({
        'activityType': 'Fertilizer',
        'date': DateTime.now().subtract(const Duration(days: 2)).toIso8601String().split('T').first,
        'type': 'Urea',
        'quantity': 50.0,
        'cropStage': 'Tillering',
        'region': 'Intermediate',
        'method': 'Top Dressing',
      }),
      loggedByUserId: 1,
      loggedByUserName: 'Bandara Farmer',
      createdAt: DateTime.now().subtract(const Duration(days: 2)).toIso8601String(),
      fieldName: 'Maha Kumbura (Plot 04)',
      cycleName: 'Yala 2026 · Bg 352',
    );

    final demoIrrigation = CropActivityDto(
      id: 302,
      cultivationCycleId: 1,
      activityType: 'Irrigation',
      date: DateTime.now().subtract(const Duration(days: 3)).toIso8601String().split('T').first,
      detailsJson: jsonEncode({
        'activityType': 'Irrigation',
        'date': DateTime.now().subtract(const Duration(days: 3)).toIso8601String().split('T').first,
        'waterLevel': 8.0,
        'duration': 4.0,
        'source': 'Canal',
      }),
      loggedByUserId: 1,
      loggedByUserName: 'Bandara Farmer',
      createdAt: DateTime.now().subtract(const Duration(days: 3)).toIso8601String(),
      fieldName: 'Maha Kumbura (Plot 04)',
      cycleName: 'Yala 2026 · Bg 352',
    );

    testWidgets('renders EditActivityScreen with pre-filled fertilizer details', (tester) async {
      await tester.pumpWidget(
        MaterialApp(
          theme: AppTheme.theme,
          home: EditActivityScreen(
            initialActivity: demoFertilizer,
          ),
        ),
      );

      await tester.pumpAndSettle();

      // Check header and titles
      expect(find.text('Edit Crop Activity'), findsOneWidget);
      expect(find.text('UPDATE OPERATION'), findsOneWidget);
      expect(find.text('Edit Fertilizer Record'), findsOneWidget);
      expect(find.text('#301'), findsOneWidget);

      // Check prefilled inputs
      expect(find.text('50.0'), findsOneWidget); // Quantity controller text
      expect(find.text('Update Fertilizer Activity'), findsOneWidget);
    });

    testWidgets('renders EditActivityScreen with pre-filled irrigation details', (tester) async {
      await tester.pumpWidget(
        MaterialApp(
          theme: AppTheme.theme,
          home: EditActivityScreen(
            initialActivity: demoIrrigation,
          ),
        ),
      );

      await tester.pumpAndSettle();

      expect(find.text('Edit Irrigation Record'), findsOneWidget);
      expect(find.text('8.0'), findsOneWidget); // Water level controller text
      expect(find.text('4.0'), findsOneWidget); // Duration controller text
      expect(find.text('Update Irrigation Activity'), findsOneWidget);
    });

    testWidgets('shows validation errors when invalid field is supplied', (tester) async {
      await tester.pumpWidget(
        MaterialApp(
          theme: AppTheme.theme,
          home: EditActivityScreen(
            initialActivity: demoFertilizer,
          ),
        ),
      );

      await tester.pumpAndSettle();

      // Clear the quantity input to make it invalid
      final qtyInput = find.widgetWithText(TextField, '50.0');
      expect(qtyInput, findsOneWidget);

      await tester.enterText(qtyInput, '');
      await tester.pumpAndSettle();

      // Tap update button
      final updateBtn = find.text('Update Fertilizer Activity');
      await tester.ensureVisible(updateBtn);
      await tester.tap(updateBtn);
      await tester.pumpAndSettle();

      // Verification error banner and field error
      expect(
        find.text('Please correct the highlighted fields before saving the activity.'),
        findsOneWidget,
      );
      expect(find.text('Quantity must be greater than 0 kg/ha.'), findsOneWidget);
    });
  });
}
