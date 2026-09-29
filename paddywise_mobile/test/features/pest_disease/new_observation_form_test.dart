import 'dart:convert';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'package:paddywise_mobile/features/pest_disease/screens/new_observation_screen.dart';
import 'package:paddywise_mobile/features/pest_disease/services/observation_api_service.dart';

Map<String, dynamic> _cycleJson() => {
      'id': 9,
      'fieldName': 'QA Test Field',
      'season': 'Maha',
      'year': 2026,
      'currentStage': 'Nursery',
    };

Map<String, dynamic> _observationJson({int id = 12}) => {
      'id': id,
      'cultivationCycleId': 9,
      'fieldId': 9,
      'fieldName': 'QA Test Field',
      'reportedByUserId': 15,
      'reportedByUserName': 'QA Farmer',
      'observationType': 'Unknown',
      'cropStage': 'Nursery',
      'symptoms': 'Yellowing leaf tips',
      'severity': 'Low',
      'imageUrl': null,
      'lastAnalyzedAt': null,
      'createdAt': '2026-09-29T09:45:36.017687Z',
      'updatedAt': '2026-09-29T09:45:36.017687Z',
      'reports': [],
    };

/// Wraps the form in a minimal GoRouter (pushed as a second route, so the form's
/// context.pop() on submit success has somewhere valid to return to) and a cycle list
/// already available, matching what the screen expects on open.
Future<void> _pumpForm(
  WidgetTester tester, {
  required ObservationApiService service,
}) async {
  // The form is taller than the default 800x600 test surface, so widgets past the fold
  // (including the Cancel/Submit row) would otherwise sit outside ListView's build cache
  // extent and never enter the element tree. A larger surface avoids needing to scroll to
  // reach them.
  tester.view.physicalSize = const Size(1000, 3000);
  tester.view.devicePixelRatio = 1.0;
  addTearDown(tester.view.reset);

  final router = GoRouter(
    initialLocation: '/',
    routes: [
      GoRoute(path: '/', builder: (context, state) => const Scaffold(body: SizedBox())),
      GoRoute(
        path: '/new',
        builder: (context, state) => NewObservationScreen(service: service),
      ),
    ],
  );

  await tester.pumpWidget(MaterialApp.router(routerConfig: router));
  router.push('/new');
  await tester.pumpAndSettle();
}

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();

  setUp(() {
    SharedPreferences.setMockInitialValues({});
  });

  group('NewObservationScreen validation', () {
    testWidgets('blocks submit when symptoms are empty', (tester) async {
      var createCalled = false;
      final client = MockClient((request) async {
        if (request.url.path.endsWith('/cycles')) {
          return http.Response(jsonEncode([_cycleJson()]), 200);
        }
        createCalled = true;
        return http.Response(jsonEncode(_observationJson()), 201);
      });

      await _pumpForm(tester, service: ObservationApiService(client: client));

      await tester.tap(find.text('Submit report'));
      await tester.pumpAndSettle();

      expect(find.text('Describe what you see.'), findsOneWidget);
      expect(createCalled, isFalse);
    });

    testWidgets('blocks submit when symptoms exceed the 2000-character limit', (tester) async {
      var createCalled = false;
      final client = MockClient((request) async {
        if (request.url.path.endsWith('/cycles')) {
          return http.Response(jsonEncode([_cycleJson()]), 200);
        }
        createCalled = true;
        return http.Response(jsonEncode(_observationJson()), 201);
      });

      await _pumpForm(tester, service: ObservationApiService(client: client));

      // maxLength on the TextFormField already stops the user typing past 2000 characters,
      // so the >2000 branch of the validator is only reachable by exceeding it some other
      // way (e.g. a pasted or pre-filled value) — set the controller's text directly to
      // exercise that validator branch regardless of the input widget's own enforcement.
      final fieldFinder = find.byType(TextFormField);
      final field = tester.widget<TextFormField>(fieldFinder);
      field.controller!.text = 'x' * 2001;
      await tester.pump();

      await tester.tap(find.text('Submit report'));
      await tester.pumpAndSettle();

      expect(find.text('Symptoms cannot exceed 2000 characters.'), findsOneWidget);
      expect(createCalled, isFalse);
    });

    testWidgets('submits successfully with valid defaults', (tester) async {
      var createCalled = false;

      final client = MockClient((request) async {
        if (request.url.path.endsWith('/cycles')) {
          return http.Response(jsonEncode([_cycleJson()]), 200);
        }
        if (request.method == 'POST' && request.url.path.endsWith('/observations')) {
          createCalled = true;
          final body = jsonDecode(request.body) as Map<String, dynamic>;
          expect(body['cultivationCycleId'], 9);
          expect(body['observationType'], 'Unknown'); // the default, unchanged
          expect(body['severity'], 'Low'); // the default, unchanged
          expect(body['symptoms'], 'Yellowing leaf tips');
          return http.Response(jsonEncode(_observationJson()), 201);
        }
        fail('Unexpected request: ${request.method} ${request.url}');
      });

      await _pumpForm(tester, service: ObservationApiService(client: client));

      // Select the (only) cultivation cycle.
      await tester.tap(find.byType(DropdownButtonFormField<int>));
      await tester.pumpAndSettle();
      await tester.tap(find.text('QA Test Field — Maha 2026 (Nursery)').last);
      await tester.pumpAndSettle();

      await tester.enterText(find.byType(TextFormField), 'Yellowing leaf tips');
      await tester.tap(find.text('Submit report'));
      await tester.pumpAndSettle();

      expect(createCalled, isTrue);
      // A successful submit pops back to the pushed route's caller.
      expect(find.text('Report a pest or disease problem'), findsNothing);
    });
  });
}
