import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:paddywise_mobile/features/crop-resource/models/crop_activity_models.dart';
import 'package:paddywise_mobile/features/crop-resource/services/crop_activity_service.dart';

// Reused unchanged from Component 1's suite: http.runWithClient + MockClient.
import '../field_cultivation/fc_test_harness.dart';

Map<String, dynamic> _activityJson({int id = 7, String type = 'Irrigation'}) => {
      'id': id,
      'cultivationCycleId': 12,
      'activityType': type,
      'date': '2026-10-01',
      'detailsJson': '{"waterLevel":3,"duration":2,"source":"Canal"}',
      'loggedByUserId': 9,
      'loggedByUserName': 'Sunil',
      'createdAt': '2026-10-01T05:00:00Z',
    };

const _create = CreateCropActivityRequest(
  activityType: 'Irrigation',
  date: '2026-10-01',
  detailsJson: '{"waterLevel":3,"duration":2,"source":"Canal"}',
);

/// CropActivityService against a fake backend, with the demo fallback OFF (the default),
/// so every result comes from the backend or is an error.
void main() {
  setUp(() => CropActivityService.useDemoFallback = false);

  group('reads', () {
    test('getActivitiesForCycle and getAllActivities hit the right paths and query', () async {
      final calls = await withFakeBackend((_) => jsonResponse([_activityJson()]), () async {
        expect((await CropActivityService.getActivitiesForCycle(12)).single.id, 7);
        await CropActivityService.getAllActivities();
        await CropActivityService.getAllActivities(cycleId: 12, activityType: 'Fertilizer');
        await CropActivityService.getAllActivities(activityType: 'All'); // "All" is not sent
      });

      expect(calls.map((c) => c.path), ['/cycles/12/activities', '/activities', '/activities', '/activities']);
      expect(calls[1].url.queryParameters, isEmpty);
      expect(calls[2].url.queryParameters, {'cycleId': '12', 'activityType': 'Fertilizer'});
      expect(calls[3].url.queryParameters, isEmpty);
    });

    test('getCycleById parses the cycle; a failed read with the fallback off is an error', () async {
      await withFakeBackend((call) {
        if (call.path == '/cycles/12') {
          return jsonResponse({'id': 12, 'season': 'Maha', 'year': 2026, 'varietyName': 'Bg 352'});
        }
        return jsonResponse({'message': 'Cultivation cycle not found.'}, 404);
      }, () async {
        expect((await CropActivityService.getCycleById(12)).displayName, 'Maha 2026 · Bg 352');
        await expectLater(CropActivityService.getCycleById(99),
            throwsA(predicate((e) => e.toString().contains('Cultivation cycle #99 was not found.'))));
      });
    });

    test('a failed list read returns an empty list, not demo data, when the fallback is off', () async {
      await withFakeBackend((_) => jsonResponse({'message': 'boom'}, 500), () async {
        expect(await CropActivityService.getActivitiesForCycle(12), isEmpty);
        expect(await CropActivityService.getAllActivities(), isEmpty);
      });
    });
  });

  group('writes', () {
    test('createActivity POSTs the request body and parses the created activity', () async {
      final calls = await withFakeBackend((_) => jsonResponse(_activityJson(), 201), () async {
        final dto = await CropActivityService.createActivity(cycleId: 12, request: _create);
        expect(dto.id, 7);
      });

      expect(calls.single.method, 'POST');
      expect(calls.single.path, '/cycles/12/activities');
      expect(calls.single.body, _create.toJson());
    });

    test('updateActivity PUTs to /activities/{id}; deleteActivity DELETEs it', () async {
      final calls = await withFakeBackend(
        (call) => call.method == 'DELETE' ? http.Response('', 204) : jsonResponse(_activityJson()),
        () async {
          await CropActivityService.updateActivity(
            activityId: 7,
            request: const UpdateCropActivityRequest(activityType: 'Irrigation', date: '2026-10-01', detailsJson: '{}'),
          );
          await CropActivityService.deleteActivity(7);
        },
      );

      expect(calls.map((c) => '${c.method} ${c.path}'), ['PUT /activities/7', 'DELETE /activities/7']);
    });

    test('a failed delete with the fallback off is an error', () async {
      await withFakeBackend((_) => jsonResponse({'message': 'Crop activity not found.'}, 404), () async {
        await expectLater(CropActivityService.deleteActivity(7),
            throwsA(predicate((e) => e.toString().contains('Failed to delete activity from server.'))));
      });
    });
  });

  group('errors reach the farmer unchanged', () {
    Future<String> failCreate(http.Response response) async {
      String? message;
      await withFakeBackend((_) => response, () async {
        try {
          await CropActivityService.createActivity(cycleId: 12, request: _create);
        } catch (e) {
          message = e.toString();
        }
      });
      return message!;
    }

    test('a { message } body is passed through', () async {
      const text = 'Activity date cannot be in the future.';
      expect(await failCreate(jsonResponse({'message': text}, 400)), contains(text));
    });

    test('a ValidationProblemDetails body shows its title, then its first error', () async {
      expect(
        await failCreate(jsonResponse({
          'title': 'One or more validation errors occurred.',
          'errors': {'DetailsJson': ['The DetailsJson field is required.']},
        }, 400)),
        contains('One or more validation errors occurred.'),
      );
      expect(
        await failCreate(jsonResponse({
          'errors': {'DetailsJson': ['The DetailsJson field is required.']},
        }, 400)),
        contains('The DetailsJson field is required.'),
      );
    });

    test('an empty 403 (the backend sends Forbid() with no body) gets the ownership message', () async {
      expect(await failCreate(http.Response('', 403)),
          contains('Only the registered farmer can log activities for this cultivation cycle.'));
    });

    test('a dropped connection gives the network message', () async {
      String? message;
      await http.runWithClient(() async {
        try {
          await CropActivityService.createActivity(cycleId: 12, request: _create);
        } catch (e) {
          message = e.toString();
        }
      }, () => _ThrowingClient());

      expect(message, contains('Network connection error. Could not connect to server.'));
    });
  });

  group('demo fallback', () {
    tearDown(() => CropActivityService.useDemoFallback = false);

    test('when switched on, a dropped connection serves the demo cycle instead of failing', () async {
      CropActivityService.useDemoFallback = true;
      late CultivationCycleSummary cycle;
      await http.runWithClient(() async {
        cycle = await CropActivityService.getCycleById(12);
      }, () => _ThrowingClient());

      expect(cycle.id, CropActivityService.defaultDemoCycle.id);
    });
  });
}

class _ThrowingClient extends http.BaseClient {
  @override
  Future<http.StreamedResponse> send(http.BaseRequest request) =>
      Future.error(http.ClientException('Connection reset by peer'));
}
