import 'dart:convert';
import 'dart:io';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'package:paddywise_mobile/features/pest_disease/models/observation.dart';
import 'package:paddywise_mobile/features/pest_disease/services/observation_api_service.dart';

Map<String, dynamic> _observationJson({
  int id = 1,
  String? lastAnalyzedAt,
  List<Map<String, dynamic>> reports = const [],
}) {
  return {
    'id': id,
    'cultivationCycleId': 9,
    'fieldId': 9,
    'fieldName': 'QA Test Field',
    'reportedByUserId': 15,
    'reportedByUserName': 'QA Farmer',
    'observationType': 'Disease',
    'cropStage': 'Nursery',
    'symptoms': 'Test symptoms',
    'severity': 'Low',
    'imageUrl': null,
    'lastAnalyzedAt': lastAnalyzedAt,
    'createdAt': '2026-09-29T09:45:36.017687Z',
    'updatedAt': '2026-09-29T09:45:36.017687Z',
    'reports': reports,
  };
}

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();

  setUp(() {
    SharedPreferences.setMockInitialValues({});
  });

  group('ObservationApiService', () {
    test('getObservations parses the list', () async {
      final client = MockClient((request) async {
        expect(request.method, 'GET');
        expect(request.url.path, contains('/observations'));
        return http.Response(
          jsonEncode([_observationJson(id: 1), _observationJson(id: 2)]),
          200,
        );
      });

      final service = ObservationApiService(client: client);
      final observations = await service.getObservations();

      expect(observations, hasLength(2));
      expect(observations.map((o) => o.id), [1, 2]);
    });

    test('getObservations passes cultivationCycleId as a query param', () async {
      final client = MockClient((request) async {
        expect(request.url.queryParameters['cultivationCycleId'], '9');
        return http.Response(jsonEncode([]), 200);
      });

      final service = ObservationApiService(client: client);
      await service.getObservations(cultivationCycleId: 9);
    });

    test('getObservationById parses a single observation', () async {
      final client = MockClient((request) async {
        expect(request.url.path, contains('/observations/42'));
        return http.Response(jsonEncode(_observationJson(id: 42)), 200);
      });

      final service = ObservationApiService(client: client);
      final observation = await service.getObservationById(42);

      expect(observation.id, 42);
    });

    test('createObservation posts the request body and parses the result', () async {
      final client = MockClient((request) async {
        expect(request.method, 'POST');
        expect(request.url.path, contains('/observations'));
        final body = jsonDecode(request.body) as Map<String, dynamic>;
        expect(body['cultivationCycleId'], 9);
        expect(body['symptoms'], 'New symptoms');
        return http.Response(jsonEncode(_observationJson(id: 99)), 201);
      });

      final service = ObservationApiService(client: client);
      final observation = await service.createObservation(const CreateObservationRequest(
        cultivationCycleId: 9,
        observationType: 'Pest',
        symptoms: 'New symptoms',
        severity: 'Low',
      ));

      expect(observation.id, 99);
    });

    test('updateObservation surfaces the backend edit-lock 400 message exactly', () async {
      final client = MockClient((request) async {
        expect(request.method, 'PUT');
        return http.Response(
          jsonEncode({
            'message': 'This observation already has a diagnosis and can no longer be edited.'
          }),
          400,
        );
      });

      final service = ObservationApiService(client: client);

      await expectLater(
        service.updateObservation(
          1,
          const UpdateObservationRequest(
            observationType: 'Pest',
            symptoms: 'Edited',
            severity: 'Low',
          ),
        ),
        throwsA(isA<ApiException>().having(
          (e) => e.message,
          'message',
          'This observation already has a diagnosis and can no longer be edited.',
        )),
      );
    });

    test('uploadPhoto sends a multipart request and parses the updated observation', () async {
      final tempDir = await Directory.systemTemp.createTemp('pest_disease_test');
      final file = File('${tempDir.path}/photo.jpg');
      await file.writeAsBytes([0xFF, 0xD8, 0xFF]); // arbitrary bytes; content is irrelevant here

      addTearDown(() => tempDir.delete(recursive: true));

      final client = MockClient((request) async {
        expect(request.method, 'POST');
        expect(request.url.path, contains('/observations/7/photo'));
        expect(request.headers['content-type'], contains('multipart/form-data'));
        return http.Response(
          jsonEncode(_observationJson(id: 7)..['imageUrl'] = 'https://blob.example/photo.jpg'),
          200,
        );
      });

      final service = ObservationApiService(client: client);
      final observation = await service.uploadPhoto(
        observationId: 7,
        filePath: file.path,
        contentType: 'image/jpeg',
      );

      expect(observation.imageUrl, 'https://blob.example/photo.jpg');
    });

    test('requestAnalysis returns the observation on a normal successful response', () async {
      final client = MockClient((request) async {
        expect(request.url.path, contains('/request-analysis'));
        return http.Response(
          jsonEncode(_observationJson(
            id: 1,
            lastAnalyzedAt: '2026-09-29T10:00:00.000000Z',
            reports: [
              {
                'id': 1,
                'possibleIssue': 'Rice Blast',
                'confidence': 0.8,
                'status': 'PendingOfficerReview',
                'officerComment': null,
                'reviewedAt': null,
                'createdAt': '2026-09-29T10:00:00.000000Z',
              }
            ],
          )),
          200,
        );
      });

      final service = ObservationApiService(client: client);
      final observation = await service.requestAnalysis(1);

      expect(observation.lastAnalyzedAt, isNotNull);
      expect(observation.reports, hasLength(1));
    });

    test('requestAnalysis surfaces a real backend error directly, without polling', () async {
      var callCount = 0;
      final client = MockClient((request) async {
        callCount++;
        return http.Response(
          jsonEncode({'message': 'This observation already has a diagnosis and can no longer be edited.'}),
          400,
        );
      });

      final service = ObservationApiService(client: client, pollInterval: const Duration(milliseconds: 1));

      await expectLater(
        service.requestAnalysis(1),
        throwsA(isA<ApiException>().having(
          (e) => e.message,
          'message',
          'This observation already has a diagnosis and can no longer be edited.',
        )),
      );
      // A real HTTP error response is not a "connection dropped" case — no poll should follow.
      expect(callCount, 1);
    });

    test('requestAnalysis falls back to polling on a network-level failure, '
        'and returns once lastAnalyzedAt is set', () async {
      var callCount = 0;
      final client = MockClient((request) async {
        callCount++;
        if (request.url.path.endsWith('/request-analysis')) {
          // Simulate a dropped connection on the direct call.
          throw http.ClientException('Connection reset by peer');
        }

        // Polling GET /observations/{id}: still processing for the first two polls, then done.
        final pollAttempt = callCount - 1; // first call above was the request-analysis attempt
        if (pollAttempt < 3) {
          return http.Response(jsonEncode(_observationJson(id: 1)), 200);
        }
        return http.Response(
          jsonEncode(_observationJson(
            id: 1,
            lastAnalyzedAt: '2026-09-29T10:05:00.000000Z',
          )),
          200,
        );
      });

      final service = ObservationApiService(
        client: client,
        pollInterval: const Duration(milliseconds: 5),
        pollWindow: const Duration(seconds: 5),
      );

      final observation = await service.requestAnalysis(1);

      expect(observation.lastAnalyzedAt, isNotNull);
      expect(callCount, greaterThanOrEqualTo(4)); // 1 failed direct call + at least 3 polls
    });

    test('requestAnalysis throws a timeout message if the poll window elapses unanalysed',
        () async {
      final client = MockClient((request) async {
        if (request.url.path.endsWith('/request-analysis')) {
          throw http.ClientException('Connection reset by peer');
        }
        return http.Response(jsonEncode(_observationJson(id: 1)), 200); // never analysed
      });

      final service = ObservationApiService(
        client: client,
        pollInterval: const Duration(milliseconds: 5),
        pollWindow: const Duration(milliseconds: 30),
      );

      await expectLater(
        service.requestAnalysis(1),
        throwsA(isA<ApiException>().having(
          (e) => e.message,
          'message',
          contains('taking longer than expected'),
        )),
      );
    });

    test('getCycles parses the cycle list', () async {
      final client = MockClient((request) async {
        expect(request.url.path, contains('/cycles'));
        return http.Response(
          jsonEncode([
            {
              'id': 9,
              'fieldName': 'QA Test Field',
              'season': 'Maha',
              'year': 2026,
              'currentStage': 'Nursery',
            }
          ]),
          200,
        );
      });

      final service = ObservationApiService(client: client);
      final cycles = await service.getCycles();

      expect(cycles, hasLength(1));
      expect(cycles.first.label, 'QA Test Field — Maha 2026 (Nursery)');
    });
  });
}
