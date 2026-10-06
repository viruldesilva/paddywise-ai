import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';

/// Shared setup for the Component 1 mobile tests (not a test file itself).
///
/// `FieldCultivationService` goes through the static `ApiClient`, which builds a fresh
/// `http.Client()` per request. `http.runWithClient` makes that factory return a
/// [MockClient] for everything run inside it, so the real service, the real `ApiClient`
/// (bearer header, error unwrapping) and the real screens are exercised with no production
/// change and no network.

/// Records every request the app sends, in order.
class RecordedCall {
  final String method;
  final Uri url;
  final Map<String, dynamic>? body;
  RecordedCall(this.method, this.url, this.body);

  String get path => url.path.replaceFirst(RegExp(r'^.*/api'), '');
}

typedef Responder = http.Response Function(RecordedCall call);

/// Runs [body] with every `http.Client()` replaced by a [MockClient] answering through
/// [respond]. Returns the calls it saw.
Future<List<RecordedCall>> withFakeBackend(
  Responder respond,
  Future<void> Function() body,
) async {
  final calls = <RecordedCall>[];
  await http.runWithClient(
    body,
    () => MockClient((request) async {
      final call = RecordedCall(
        request.method,
        request.url,
        request.body.isEmpty ? null : jsonDecode(request.body) as Map<String, dynamic>,
      );
      calls.add(call);
      return respond(call);
    }),
  );
  return calls;
}

http.Response jsonResponse(Object body, [int status = 200]) =>
    http.Response(jsonEncode(body), status, headers: {'content-type': 'application/json'});

/// Pushes [screen] on top of a blank route so the form's `context.pop(result)` has
/// somewhere to return to; the popped value lands in [popped].
Future<void> pumpPushed(
  WidgetTester tester,
  Widget screen, {
  void Function(Object? result)? onPopped,
}) async {
  tester.view.physicalSize = const Size(1000, 3000);
  tester.view.devicePixelRatio = 1.0;
  addTearDown(tester.view.reset);

  final router = GoRouter(
    initialLocation: '/',
    routes: [
      GoRoute(path: '/', builder: (context, state) => const Scaffold(body: Text('HOME'))),
      GoRoute(path: '/form', builder: (context, state) => screen),
    ],
  );

  await tester.pumpWidget(MaterialApp.router(routerConfig: router));
  final result = router.push<Object?>('/form');
  if (onPopped != null) result.then(onPopped);
  await tester.pumpAndSettle();
}

// ------------------------------------------------------------------ fixtures

Map<String, dynamic> divisionJson({int id = 3, String name = 'Polonnaruwa'}) =>
    {'id': id, 'name': name, 'district': 'Polonnaruwa', 'province': 'North Central'};

Map<String, dynamic> varietyJson({int id = 2, String name = 'Bg 352', int durationDays = 105}) =>
    {'id': id, 'name': name, 'durationDays': durationDays, 'ageGroup': '3.5 month', 'notes': null};

Map<String, dynamic> fieldJson({int id = 4, String name = 'Lower paddy'}) => {
      'id': id,
      'name': name,
      'area': 2.5,
      'soilType': 'Alluvial',
      'irrigationType': 'Major tank',
      'latitude': 7.94,
      'longitude': 81.0,
      'divisionId': 3,
      'divisionName': 'Polonnaruwa',
      'farmerId': 9,
      'farmerName': 'Sunil',
      'isActive': true,
    };

Map<String, dynamic> cycleJson({
  int id = 12,
  String status = 'Active',
  String currentStage = 'Tillering',
  String expectedStageToday = 'Tillering',
}) =>
    {
      'id': id,
      'fieldId': 4,
      'fieldName': 'Lower paddy',
      'varietyId': 2,
      'varietyName': 'Bg 352',
      'durationDays': 105,
      'season': 'Maha',
      'year': 2026,
      'method': 'Transplanting',
      'sowingDate': '2026-09-01',
      'expectedHarvestDate': '2026-12-15',
      'actualHarvestDate': null,
      'currentStage': currentStage,
      'expectedStageToday': expectedStageToday,
      'status': status,
      'notes': null,
      'timeline': [
        {'stage': 'Nursery', 'start': '2026-09-01', 'end': '2026-09-16'},
        {'stage': 'Tillering', 'start': '2026-09-17', 'end': '2026-10-12'},
      ],
      'stageLogs': [
        {
          'id': 1,
          'stage': 'Tillering',
          'observedOn': '2026-09-20',
          'notes': 'Tillers visible',
          'loggedByUserId': 9,
          'loggedByUserName': 'Sunil',
          'createdAt': '2026-09-20T05:00:00Z',
        }
      ],
    };

Map<String, dynamic> planJson({int id = 31, String status = 'PendingOfficerApproval'}) => {
      'id': id,
      'cycleId': 12,
      'objective': 'Good yield.',
      'status': status,
      'plan': {
        'summary': 'Rest of the season.',
        'steps': [
          {
            'stage': 'Tillering',
            'windowStart': '2026-09-20',
            'windowEnd': '2026-10-12',
            'task': 'Top-dress per the ResourceAnalysisAgent.',
            'rationale': 'Tillering needs nitrogen.',
            'category': 'Nutrient',
          }
        ],
        'delegations': [],
        'assumptions': ['Canal water available.'],
      },
      'validationErrors': [],
      'officerComment': null,
      'reviewedAt': null,
      'createdAt': '2026-09-19T08:00:00Z',
      'agentRuns': [],
    };
