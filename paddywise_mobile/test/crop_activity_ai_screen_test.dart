import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:paddywise_mobile/features/crop-resource/screens/crop_activity_ai_screen.dart';
import 'package:paddywise_mobile/theme/app_theme.dart';

void main() {
  group('CropActivityAiScreen Tests', () {
    testWidgets('renders CropActivityAiScreen with agentic AI badge and summary', (tester) async {
      tester.view.physicalSize = const Size(1080, 2400);
      tester.view.devicePixelRatio = 1.0;
      addTearDown(() => tester.view.resetPhysicalSize());

      await tester.pumpWidget(
        MaterialApp(
          theme: AppTheme.theme,
          home: const CropActivityAiScreen(),
        ),
      );

      await tester.pumpAndSettle();

      // Check header and titles
      expect(find.text('AI Field Advisor'), findsOneWidget);
      expect(find.text('AGENTIC AI ADVISOR'), findsOneWidget);
      expect(find.text('Maha Kumbura (Plot 04)'), findsOneWidget);

      // Check executive summary section
      expect(find.text('Agronomic Executive Summary'), findsOneWidget);

      // Check diagnostics section
      expect(find.text('Real-Time Field Diagnostics'), findsOneWidget);
      expect(find.text('Water Hydrology'), findsOneWidget);
      expect(find.text('Nutrient Balance'), findsOneWidget);
      expect(find.text('Crop Protection'), findsOneWidget);
      expect(find.text('Field Operations'), findsOneWidget);

      // Check recommendations
      expect(find.text('Agronomic Recommendations'), findsOneWidget);
      expect(find.textContaining('Panicle Initiation'), findsWidgets);

      // Check Ask Field Advisor chat console
      expect(find.text('Ask Field Advisor'), findsOneWidget);
      expect(find.text('When is my next fertilizer split due?'), findsOneWidget);
    });

    testWidgets('tapping suggested question sends message and receives AI response', (tester) async {
      tester.view.physicalSize = const Size(1080, 2400);
      tester.view.devicePixelRatio = 1.0;
      addTearDown(() => tester.view.resetPhysicalSize());

      await tester.pumpWidget(
        MaterialApp(
          theme: AppTheme.theme,
          home: const CropActivityAiScreen(),
        ),
      );

      await tester.pumpAndSettle();

      // Tap on the first suggested question chip
      final chip = find.text('When is my next fertilizer split due?');
      expect(chip, findsOneWidget);
      await tester.tap(chip);
      await tester.pumpAndSettle();

      // Verify that user message appears in thread
      expect(find.text('When is my next fertilizer split due?'), findsWidgets);

      // Verify that AI assistant response appears
      expect(find.textContaining('Bg 352 variety'), findsOneWidget);
    });
  });
}
