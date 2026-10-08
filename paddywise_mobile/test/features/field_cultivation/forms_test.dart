import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:paddywise_mobile/features/field_cultivation/models/cycle_models.dart';
import 'package:paddywise_mobile/features/field_cultivation/models/field_models.dart';
import 'package:paddywise_mobile/features/field_cultivation/models/plan_models.dart';
import 'package:paddywise_mobile/features/field_cultivation/screens/field_form_screen.dart';
import 'package:paddywise_mobile/features/field_cultivation/screens/request_plan_screen.dart';
import 'package:paddywise_mobile/features/field_cultivation/screens/start_cycle_screen.dart';

import 'fc_test_harness.dart';

/// The three Component 1 forms, driven through the real screens and the real service
/// against a fake backend.
void main() {
  // ---------------------------------------------------------------- add a field

  group('FieldFormScreen', () {
    Responder backend({void Function(RecordedCall)? onCreate}) => (call) {
          if (call.path == '/divisions') return jsonResponse([divisionJson()]);
          if (call.method == 'POST' && call.path == '/fields') {
            onCreate?.call(call);
            return jsonResponse(fieldJson(), 201);
          }
          return jsonResponse({'message': 'unexpected ${call.method} ${call.path}'}, 500);
        };

    Finder input(String hint) => find.widgetWithText(TextFormField, hint);

    testWidgets('name, area and division are required', (tester) async {
      final calls = await withFakeBackend(backend(), () async {
        await pumpPushed(tester, const FieldFormScreen());
        await tester.tap(find.text('Register field'));
        await tester.pumpAndSettle();
      });

      expect(find.text('Field name is required.'), findsOneWidget);
      expect(find.text('Area is required.'), findsOneWidget);
      expect(find.text('Division is required.'), findsOneWidget);
      expect(calls.where((c) => c.method == 'POST'), isEmpty);
    });

    for (final (area, valid) in [('0.01', true), ('0.009', false), ('1000', true), ('1000.5', false), ('abc', false)]) {
      testWidgets('area $area is ${valid ? 'accepted' : 'refused'}', (tester) async {
        await withFakeBackend(backend(), () async {
          await pumpPushed(tester, const FieldFormScreen());
          await tester.enterText(input('e.g. 2.5'), area);
          await tester.tap(find.text('Register field'));
          await tester.pumpAndSettle();
        });

        expect(find.text('Area must be between 0.01 and 1000.0.'), valid ? findsNothing : findsOneWidget);
      });
    }

    for (final (lat, lng, error) in [
      ('90', '180', null),
      ('-90', '-180', null),
      ('90.01', '0', 'Latitude must be between -90.0 and 90.0.'),
      ('0', '-180.5', 'Longitude must be between -180.0 and 180.0.'),
    ]) {
      testWidgets('location $lat, $lng ${error == null ? 'passes' : 'is refused'}', (tester) async {
        await withFakeBackend(backend(), () async {
          await pumpPushed(tester, const FieldFormScreen());
          await tester.enterText(input('Latitude'), lat);
          await tester.enterText(input('Longitude'), lng);
          await tester.tap(find.text('Register field'));
          await tester.pumpAndSettle();
        });

        if (error == null) {
          expect(find.textContaining('Latitude must be'), findsNothing);
          expect(find.textContaining('Longitude must be'), findsNothing);
        } else {
          expect(find.text(error), findsOneWidget);
        }
      });
    }

    testWidgets('the name box stops at 100 characters', (tester) async {
      await withFakeBackend(backend(), () async {
        await pumpPushed(tester, const FieldFormScreen());
        await tester.enterText(input('e.g. Lower paddy, north bund'), 'n' * 150);
        await tester.pump();
      });

      final name = tester.widget<TextFormField>(input('e.g. Lower paddy, north bund'));
      expect(name.controller!.text.length, FieldRules.nameMaxLength);
    });

    testWidgets('a valid form posts the field and pops with it', (tester) async {
      Map<String, dynamic>? sent;
      Object? popped;
      await withFakeBackend(backend(onCreate: (c) => sent = c.body), () async {
        await pumpPushed(tester, const FieldFormScreen(), onPopped: (r) => popped = r);
        await tester.enterText(input('e.g. Lower paddy, north bund'), '  Lower paddy ');
        await tester.enterText(input('e.g. 2.5'), '2.5');
        await tester.tap(find.byType(DropdownButtonFormField<int>));
        await tester.pumpAndSettle();
        await tester.tap(find.text('Polonnaruwa · Polonnaruwa').last);
        await tester.pumpAndSettle();
        await tester.tap(find.text('Alluvial'));
        await tester.tap(find.text('Register field'));
        await tester.pumpAndSettle();
      });

      expect(sent, {
        'name': 'Lower paddy',
        'area': 2.5,
        'soilType': 'Alluvial',
        'irrigationType': '',
        'divisionId': 3,
        'latitude': null,
        'longitude': null,
      });
      expect(popped, isA<Field>());
      expect(find.text('HOME'), findsOneWidget);
    });

    testWidgets('a backend { message } is shown on the form', (tester) async {
      await withFakeBackend((call) {
        if (call.path == '/divisions') return jsonResponse([divisionJson()]);
        return jsonResponse({'message': 'Division not found'}, 400);
      }, () async {
        await pumpPushed(tester, const FieldFormScreen());
        await tester.enterText(input('e.g. Lower paddy, north bund'), 'Lower paddy');
        await tester.enterText(input('e.g. 2.5'), '2');
        await tester.tap(find.byType(DropdownButtonFormField<int>));
        await tester.pumpAndSettle();
        await tester.tap(find.text('Polonnaruwa · Polonnaruwa').last);
        await tester.pumpAndSettle();
        await tester.tap(find.text('Register field'));
        await tester.pumpAndSettle();
      });

      expect(find.text('Division not found'), findsOneWidget);
    });
  });

  // ---------------------------------------------------------------- start a cycle

  group('StartCycleScreen', () {
    Responder backend({void Function(RecordedCall)? onStart}) => (call) {
          if (call.path == '/varieties') return jsonResponse([varietyJson()]);
          if (call.path == '/fields/4/start-cultivation') {
            onStart?.call(call);
            return jsonResponse(cycleJson(status: 'Planned'), 201);
          }
          return jsonResponse({'message': 'unexpected'}, 500);
        };

    testWidgets('a variety is required', (tester) async {
      final calls = await withFakeBackend(backend(), () async {
        await pumpPushed(tester, const StartCycleScreen(fieldId: 4));
        await tester.tap(find.text('Start season'));
        await tester.pumpAndSettle();
      });

      expect(find.text('Choose the variety you are sowing.'), findsOneWidget);
      expect(calls.where((c) => c.method == 'POST'), isEmpty);
    });

    testWidgets('the sowing date picker only offers today-30 to today+365', (tester) async {
      await withFakeBackend(backend(), () async {
        await pumpPushed(tester, const StartCycleScreen(fieldId: 4));
        await tester.tap(find.text('Change'));
        await tester.pumpAndSettle();
      });

      final picker = tester.widget<DatePickerDialog>(find.byType(DatePickerDialog));
      expect(picker.firstDate, today.subtract(const Duration(days: CycleRules.maxBackdateDays)));
      expect(picker.lastDate, today.add(const Duration(days: CycleRules.maxLookaheadDays)));
      expect(picker.helpText, 'Sowing date');
    });

    testWidgets('notes stop at 1000 characters', (tester) async {
      await withFakeBackend(backend(), () async {
        await pumpPushed(tester, const StartCycleScreen(fieldId: 4));
        await tester.enterText(
            find.widgetWithText(TextFormField, 'Anything your officer should know, e.g. seed source'),
            'n' * 1200);
        await tester.pump();
      });

      final notes = tester.widget<TextFormField>(
          find.widgetWithText(TextFormField, 'Anything your officer should know, e.g. seed source'));
      expect(notes.controller!.text.length, CycleRules.notesMaxLength);
    });

    testWidgets('a valid form posts today as the sowing date and pops with the cycle', (tester) async {
      Map<String, dynamic>? sent;
      Object? popped;
      await withFakeBackend(backend(onStart: (c) => sent = c.body), () async {
        await pumpPushed(tester, const StartCycleScreen(fieldId: 4), onPopped: (r) => popped = r);
        // The first int dropdown is the variety; the second is the season year.
        await tester.tap(find.byType(DropdownButtonFormField<int>).first);
        await tester.pumpAndSettle();
        await tester.tap(find.text('Bg 352 · 105 days').last);
        await tester.pumpAndSettle();
        await tester.tap(find.text('Start season'));
        await tester.pumpAndSettle();
      });

      expect(sent!['varietyId'], 2);
      expect(sent!['fieldId'], 4);
      expect(sent!['sowingDate'], toDateOnly(today));
      expect(sent!['method'], 'Transplanting');
      expect(sent!['notes'], isNull);
      expect(popped, isA<CultivationCycle>());
    });
  });

  // ---------------------------------------------------------------- request a plan

  group('RequestPlanScreen', () {
    testWidgets('an empty objective is blocked without calling the backend', (tester) async {
      final calls = await withFakeBackend((_) => jsonResponse(planJson(), 201), () async {
        await pumpPushed(tester, const RequestPlanScreen(cycleId: 12));
        await tester.enterText(find.byType(TextField), '   ');
        await tester.tap(find.text('Generate my plan'));
        await tester.pumpAndSettle();
      });

      expect(find.text('Tell the assistant what you want from this season.'), findsOneWidget);
      expect(calls, isEmpty);
    });

    testWidgets('the objective box stops at 1000 characters', (tester) async {
      await withFakeBackend((_) => jsonResponse(planJson(), 201), () async {
        await pumpPushed(tester, const RequestPlanScreen(cycleId: 12));
        await tester.enterText(find.byType(TextField), 'o' * 1500);
        await tester.pump();
      });

      final box = tester.widget<TextField>(find.byType(TextField));
      expect(box.controller!.text.length, PlanRules.objectiveMaxLength);
    });

    testWidgets('submitting posts the trimmed objective and pops with the plan', (tester) async {
      Object? popped;
      final calls = await withFakeBackend((_) => jsonResponse(planJson(), 201), () async {
        await pumpPushed(tester, const RequestPlanScreen(cycleId: 12), onPopped: (r) => popped = r);
        await tester.enterText(find.byType(TextField), '  A good yield.  ');
        await tester.tap(find.text('Generate my plan'));
        await tester.pumpAndSettle();
      });

      expect(calls.single.path, '/cycles/12/plans');
      expect(calls.single.body, {'objective': 'A good yield.'});
      expect(popped, isA<CultivationPlan>());
    });

    testWidgets('a Draft is polled until it is generated, then the screen pops with it', (tester) async {
      Object? popped;
      var polls = 0;
      final calls = await withFakeBackend((call) {
        if (call.method == 'POST') return jsonResponse(planJson(status: 'Draft'), 202);
        polls++;
        return jsonResponse(planJson(status: polls < 2 ? 'Draft' : 'PendingOfficerApproval'));
      }, () async {
        await pumpPushed(tester, const RequestPlanScreen(cycleId: 12), onPopped: (r) => popped = r);
        await tester.enterText(find.byType(TextField), 'A good yield.');
        await tester.tap(find.text('Generate my plan'));
        await tester.pump();
        await tester.pump();

        expect(find.textContaining('waiting behind other'), findsOneWidget);
        expect(popped, isNull);

        await tester.pump(const Duration(seconds: 4)); // first poll: still Draft
        await tester.pump();
        expect(popped, isNull);
        await tester.pump(const Duration(seconds: 4)); // second poll: generated
        await tester.pumpAndSettle();
      });

      expect(calls.map((c) => '${c.method} ${c.path}'),
          ['POST /cycles/12/plans', 'GET /plans/31', 'GET /plans/31']);
      expect((popped as CultivationPlan).status, PlanStatus.pendingOfficerApproval);
    });

    testWidgets('leaving while the plan is generated stops polling', (tester) async {
      final calls = await withFakeBackend((call) => call.method == 'POST'
          ? jsonResponse(planJson(status: 'Draft'), 202)
          : jsonResponse(planJson(status: 'Draft')), () async {
        await pumpPushed(tester, const RequestPlanScreen(cycleId: 12));
        await tester.enterText(find.byType(TextField), 'A good yield.');
        await tester.tap(find.text('Generate my plan'));
        await tester.pump();
        await tester.pump();
        await tester.pump(const Duration(seconds: 4));
        await tester.pump();

        await tester.binding.handlePopRoute(); // the system back button
        await tester.pumpAndSettle();
        expect(find.byType(RequestPlanScreen), findsNothing);

        await tester.pump(const Duration(seconds: 30));
      });

      expect(calls.where((c) => c.method == 'GET'), hasLength(1));
    });

    testWidgets('a backend refusal is shown and the form comes back', (tester) async {
      const message = 'This cycle already has a plan awaiting officer approval or already approved.';
      await withFakeBackend((_) => jsonResponse({'message': message}, 400), () async {
        await pumpPushed(tester, const RequestPlanScreen(cycleId: 12));
        await tester.enterText(find.byType(TextField), 'A good yield.');
        await tester.tap(find.text('Generate my plan'));
        await tester.pumpAndSettle();
      });

      expect(find.text(message), findsOneWidget);
      expect(find.text('Generate my plan'), findsOneWidget);
    });
  });
}
