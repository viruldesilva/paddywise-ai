import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:paddywise_mobile/features/crop-resource/models/crop_activity_models.dart';
import 'package:paddywise_mobile/features/crop-resource/widgets/activity_form.dart';
import 'package:paddywise_mobile/theme/app_theme.dart';

String _ymd(DateTime d) =>
    '${d.year.toString().padLeft(4, '0')}-${d.month.toString().padLeft(2, '0')}-${d.day.toString().padLeft(2, '0')}';

DateTime get _today {
  final now = DateTime.now();
  return DateTime(now.year, now.month, now.day);
}

CultivationCycleSummary _cycle({String? sowingDate, String? actualHarvestDate}) => CultivationCycleSummary(
      id: 12,
      fieldId: 4,
      fieldName: 'Lower paddy',
      varietyId: 2,
      varietyName: 'Bg 352',
      season: 'Maha',
      year: 2026,
      currentStage: 'Tillering',
      sowingDate: sowingDate ?? _ymd(_today.subtract(const Duration(days: 40))),
      actualHarvestDate: actualHarvestDate,
    );

/// The real ActivityForm, prefilled through initialData (which is how the edit screen uses
/// it), then submitted. The existing root-level screen tests cover tab switching and the
/// empty-form banner; these cover the rules and the submitted payload. Rules mirror
/// CropActivityService on the backend.
Future<List<Map<String, dynamic>>> _submit(
  WidgetTester tester, {
  required ActivityType type,
  required Map<String, dynamic> initialData,
  CultivationCycleSummary? cycle,
}) async {
  tester.view.physicalSize = const Size(1080, 3200);
  tester.view.devicePixelRatio = 1.0;
  addTearDown(tester.view.reset);

  final submitted = <Map<String, dynamic>>[];
  await tester.pumpWidget(MaterialApp(
    theme: AppTheme.theme,
    home: Scaffold(
      body: SingleChildScrollView(
        child: ActivityForm(
          activityType: type,
          cycle: cycle ?? _cycle(),
          initialData: initialData,
          submitButtonText: 'Save',
          onSubmit: (data) async => submitted.add(data),
        ),
      ),
    ),
  ));
  await tester.pumpAndSettle();
  await tester.tap(find.text('Save'));
  await tester.pumpAndSettle();
  return submitted;
}

Map<String, dynamic> _irrigation({String? date, Object waterLevel = 3, Object duration = 2}) => {
      'date': date ?? _ymd(_today),
      'waterLevel': waterLevel,
      'duration': duration,
      'source': 'Canal',
    };

void main() {
  group('date window', () {
    for (final (label, daysAgo, ok) in [('today', 0, true), ('7 days ago', 7, true), ('8 days ago', 8, false)]) {
      testWidgets('an activity dated $label is ${ok ? 'accepted' : 'refused'}', (tester) async {
        final sent = await _submit(tester,
            type: ActivityType.irrigation,
            initialData: _irrigation(date: _ymd(_today.subtract(Duration(days: daysAgo)))));

        expect(sent, ok ? hasLength(1) : isEmpty);
        if (!ok) expect(find.textContaining('Activity date must be within the last week'), findsOneWidget);
      });
    }

    testWidgets('a future date is refused', (tester) async {
      final sent = await _submit(tester,
          type: ActivityType.irrigation, initialData: _irrigation(date: _ymd(_today.add(const Duration(days: 1)))));

      expect(sent, isEmpty);
      expect(find.text('Activity date cannot be in the future.'), findsOneWidget);
    });

    testWidgets('a date before a recent sowing is refused', (tester) async {
      final sowing = _today.subtract(const Duration(days: 3));
      final sent = await _submit(tester,
          type: ActivityType.irrigation,
          cycle: _cycle(sowingDate: _ymd(sowing)),
          initialData: _irrigation(date: _ymd(sowing.subtract(const Duration(days: 1)))));

      expect(sent, isEmpty);
      expect(find.text('Activity date must be within the last week (since ${_ymd(sowing)}).'), findsOneWidget);
    });

    testWidgets('a date after the actual harvest is refused', (tester) async {
      final harvest = _today.subtract(const Duration(days: 3));
      final sent = await _submit(tester,
          type: ActivityType.irrigation,
          cycle: _cycle(actualHarvestDate: _ymd(harvest)),
          initialData: _irrigation(date: _ymd(harvest.add(const Duration(days: 1)))));

      expect(sent, isEmpty);
      expect(find.text('Activity date cannot be in the future.'), findsOneWidget); // the form's wording for "after the max date"
    });
  });

  group('irrigation limits', () {
    for (final (level, duration, error) in [
      (0, 2, null),
      (150, 2, null),
      (-1, 2, 'Water level must be 0 cm or greater.'),
      (151, 2, 'Water level cannot exceed 150 cm.'),
      (3, 0, 'Duration must be greater than 0 hours.'),
      (3, 72, null),
      (3, 73, 'Duration cannot exceed 72 hours.'),
    ]) {
      testWidgets('water $level cm for $duration h ${error == null ? 'passes' : 'is refused'}', (tester) async {
        final sent = await _submit(tester,
            type: ActivityType.irrigation, initialData: _irrigation(waterLevel: level, duration: duration));

        if (error == null) {
          expect(sent, hasLength(1));
        } else {
          expect(sent, isEmpty);
          expect(find.text(error), findsOneWidget);
        }
      });
    }
  });

  group('pesticide fields', () {
    Map<String, dynamic> pesticide({String product = 'Fipronil 50 SC', String targetPest = 'Stem borer', Object quantity = 1.5}) =>
        {'date': _ymd(_today), 'product': product, 'targetPest': targetPest, 'quantity': quantity, 'method': 'Spraying'};

    for (final (data, error) in [
      (pesticide(product: 'F'), 'Product name must be at least 2 characters.'),
      (pesticide(product: 'x' * 101), 'Product name cannot exceed 100 characters.'),
      (pesticide(targetPest: ''), 'Target pest / disease is required.'),
      (pesticide(quantity: 0), 'Quantity must be greater than 0 ml/g.'),
    ]) {
      testWidgets('refuses: $error', (tester) async {
        final sent = await _submit(tester, type: ActivityType.pesticide, initialData: data);

        expect(sent, isEmpty);
        expect(find.text(error), findsOneWidget);
      });
    }
  });

  group('valid submit', () {
    testWidgets('irrigation sends every key the backend requires', (tester) async {
      final sent = await _submit(tester, type: ActivityType.irrigation, initialData: _irrigation(waterLevel: 4.5, duration: 3));

      expect(sent.single, {
        'activityType': 'Irrigation',
        'date': _ymd(_today),
        'waterLevel': 4.5,
        'duration': 3.0,
        'source': 'Canal',
      });
    });

    testWidgets('fertilizer sends type, quantity, stage, region and method', (tester) async {
      final sent = await _submit(tester, type: ActivityType.fertilizer, initialData: {
        'date': _ymd(_today),
        'type': 'Urea',
        'quantity': 50,
        'region': 'Dry',
        'method': 'Broadcasting',
      });

      expect(sent.single, {
        'activityType': 'Fertilizer',
        'date': _ymd(_today),
        'type': 'Urea',
        'quantity': 50.0,
        'cropStage': 'Tillering', // taken from the cycle's current stage
        'region': 'Dry',
        'method': 'Broadcasting',
      });
    });
  });
}
