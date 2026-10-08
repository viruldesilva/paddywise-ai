import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:paddywise_mobile/core/api/api_client.dart';
import 'package:paddywise_mobile/features/field_cultivation/models/cycle_models.dart';
import 'package:paddywise_mobile/features/field_cultivation/models/field_models.dart';
import 'package:paddywise_mobile/features/field_cultivation/models/plan_models.dart';
import 'package:paddywise_mobile/features/field_cultivation/services/field_cultivation_service.dart';

import 'fc_test_harness.dart';

/// FieldCultivationService through the real ApiClient, against a fake backend.
/// A plan request is one short POST that returns a Draft; PlanPoller (see
/// plan_poller_test.dart) then follows it until it is generated.
void main() {
  group('reads', () {
    test('getDivisions, getVarieties and getMyFields parse lists', () async {
      late List<Division> divisions;
      late List<Variety> varieties;
      late List<Field> fields;

      final calls = await withFakeBackend((call) => switch (call.path) {
            '/divisions' => jsonResponse([divisionJson()]),
            '/varieties' => jsonResponse([varietyJson(), varietyJson(id: 3, name: 'Bg 360')]),
            '/fields' => jsonResponse([fieldJson()]),
            _ => jsonResponse({'message': 'unexpected'}, 500),
          }, () async {
        divisions = await FieldCultivationService.getDivisions();
        varieties = await FieldCultivationService.getVarieties();
        fields = await FieldCultivationService.getMyFields();
      });

      expect(divisions.single.name, 'Polonnaruwa');
      expect(varieties.map((v) => v.name), ['Bg 352', 'Bg 360']);
      expect(fields.single.areaLabel, '2.5 acres');
      expect(calls.map((c) => c.method), everyElement('GET'));
    });

    test('getMyCycles passes fieldId as a query parameter only when given', () async {
      final calls = await withFakeBackend((_) => jsonResponse([cycleJson()]), () async {
        await FieldCultivationService.getMyCycles();
        await FieldCultivationService.getMyCycles(fieldId: 4);
      });

      expect(calls[0].url.queryParameters, isEmpty);
      expect(calls[1].url.queryParameters, {'fieldId': '4'});
    });

    test('getField, getCycle, getPlan and getPlansForCycle hit the right paths', () async {
      final calls = await withFakeBackend((call) {
        if (call.path.startsWith('/fields/')) return jsonResponse(fieldJson());
        if (call.path == '/cycles/12') return jsonResponse(cycleJson());
        if (call.path == '/plans/31') return jsonResponse(planJson());
        return jsonResponse([planJson(id: 32), planJson(id: 31, status: 'Rejected')]);
      }, () async {
        expect((await FieldCultivationService.getField(4)).id, 4);
        expect((await FieldCultivationService.getCycle(12)).id, 12);
        expect((await FieldCultivationService.getPlan(31)).id, 31);
        final plans = await FieldCultivationService.getPlansForCycle(12);
        expect(plans.map((p) => p.id), [32, 31]); // backend order kept: newest first
      });

      expect(calls.map((c) => c.path), ['/fields/4', '/cycles/12', '/plans/31', '/cycles/12/plans']);
    });
  });

  group('writes', () {
    const fieldRequest = FieldRequest(
      name: 'Lower paddy',
      area: 2.5,
      soilType: 'Alluvial',
      irrigationType: 'Major tank',
      divisionId: 3,
    );

    test('createField POSTs, updateField PUTs, deleteField DELETEs', () async {
      final calls = await withFakeBackend(
        (call) => call.method == 'DELETE' ? http.Response('', 204) : jsonResponse(fieldJson()),
        () async {
          await FieldCultivationService.createField(fieldRequest);
          await FieldCultivationService.updateField(4, fieldRequest);
          await FieldCultivationService.deleteField(4);
        },
      );

      expect(calls.map((c) => '${c.method} ${c.path}'),
          ['POST /fields', 'PUT /fields/4', 'DELETE /fields/4']);
      expect(calls[0].body, fieldRequest.toJson());
      expect(calls[1].body, fieldRequest.toJson());
    });

    test('startCultivation posts to the field route with the DateOnly sowing date', () async {
      final calls = await withFakeBackend((_) => jsonResponse(cycleJson(), 201), () async {
        await FieldCultivationService.startCultivation(
          4,
          StartCycleRequest(
            varietyId: 2,
            season: Season.maha,
            year: 2026,
            method: CultivationMethod.transplanting,
            sowingDate: DateTime(2026, 9, 1),
          ),
        );
      });

      expect(calls.single.path, '/fields/4/start-cultivation');
      expect(calls.single.body!['sowingDate'], '2026-09-01');
      expect(calls.single.body!['season'], 'Maha');
    });

    test('logStage sends the wire stage name and a DateOnly date', () async {
      final calls = await withFakeBackend((_) => jsonResponse(cycleJson()), () async {
        await FieldCultivationService.logStage(12,
            stage: GrowthStage.panicleInitiation, observedOn: DateTime(2026, 10, 20), notes: 'PI');
      });

      expect(calls.single.path, '/cycles/12/stages');
      expect(calls.single.body, {'stage': 'PanicleInitiation', 'observedOn': '2026-10-20', 'notes': 'PI'});
    });

    test('abandonCycle PATCHes status Abandoned — the only status change the app makes', () async {
      final calls = await withFakeBackend(
          (_) => jsonResponse(cycleJson(status: 'Abandoned')), () async {
        final cycle = await FieldCultivationService.abandonCycle(12);
        expect(cycle.status, CycleStatus.abandoned);
      });

      expect(calls.single.method, 'PATCH');
      expect(calls.single.path, '/cycles/12/status');
      expect(calls.single.body, {'status': 'Abandoned'});
    });

    test('requestPlan posts the objective once and parses the Draft it gets back', () async {
      final calls = await withFakeBackend((_) => jsonResponse(planJson(status: 'Draft'), 202), () async {
        final plan = await FieldCultivationService.requestPlan(12, 'Good yield.');
        expect(plan.status, PlanStatus.draft);
      });

      expect(calls, hasLength(1));
      expect(calls.single.path, '/cycles/12/plans');
      expect(calls.single.body, {'objective': 'Good yield.'});
      expect(FieldCultivationService.planPollInterval, const Duration(seconds: 4));
      expect(FieldCultivationService.planPollLimit, const Duration(minutes: 20));
    });
  });

  group('errors reach the user unchanged', () {
    Future<ApiException> failWith(http.Response response) async {
      ApiException? caught;
      await withFakeBackend((_) => response, () async {
        try {
          await FieldCultivationService.requestPlan(12, 'Plan.');
        } on ApiException catch (e) {
          caught = e;
        }
      });
      return caught!;
    }

    test('a controller { message } is passed through exactly', () async {
      const message = 'This cycle already has a plan awaiting officer approval or already approved.';
      final error = await failWith(jsonResponse({'message': message}, 400));
      expect(error.toString(), message);
      expect(error.statusCode, 400);
    });

    test('a model-validation { errors } body shows its first error', () async {
      final error = await failWith(jsonResponse({
        'title': 'One or more validation errors occurred.',
        'errors': {
          'Objective': ['Objective cannot exceed 1000 characters.']
        },
      }, 400));
      expect(error.message, 'Objective cannot exceed 1000 characters.');
    });

    test('a 403 with { message } shows the ownership message', () async {
      final error = await failWith(
          jsonResponse({'message': 'You do not have access to this cultivation cycle.'}, 403));
      expect(error.message, 'You do not have access to this cultivation cycle.');
      expect(error.statusCode, 403);
    });

    test('a dropped connection becomes the friendly cannot-reach message', () async {
      ApiException? caught;
      await http.runWithClient(() async {
        try {
          await FieldCultivationService.getMyFields();
        } on ApiException catch (e) {
          caught = e;
        }
      }, () => _ThrowingClient());

      expect(caught!.message, startsWith('Cannot reach the Kumburu server'));
    });
  });
}

class _ThrowingClient extends http.BaseClient {
  @override
  Future<http.StreamedResponse> send(http.BaseRequest request) =>
      Future.error(http.ClientException('Connection reset by peer'));
}
