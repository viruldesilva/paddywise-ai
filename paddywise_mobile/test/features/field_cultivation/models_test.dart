import 'package:flutter_test/flutter_test.dart';
import 'package:paddywise_mobile/features/field_cultivation/models/cycle_models.dart';
import 'package:paddywise_mobile/features/field_cultivation/models/field_models.dart';
import 'package:paddywise_mobile/features/field_cultivation/models/plan_models.dart';

import 'fc_test_harness.dart';

void main() {
  group('DateOnly helpers', () {
    test('parseDateOnly / toDateOnly round-trip with no time-zone shift', () {
      final date = parseDateOnly('2026-01-05');
      expect(date, DateTime(2026, 1, 5));
      expect(toDateOnly(date), '2026-01-05');
      expect(toDateOnly(DateTime(987, 3, 9)), '0987-03-09');
    });

    test('parseNullableDateOnly tolerates null and empty', () {
      expect(parseNullableDateOnly(null), isNull);
      expect(parseNullableDateOnly(''), isNull);
      expect(parseNullableDateOnly('2026-12-15'), DateTime(2026, 12, 15));
    });

    test('formatDate shows a dash for null', () {
      expect(formatDate(null), '—');
      expect(formatDate(DateTime(2026, 3, 14)), '14 Mar 2026');
    });
  });

  group('Division / Variety / Field.fromJson', () {
    test('Division label includes the district only when present', () {
      expect(Division.fromJson(divisionJson()).label, 'Polonnaruwa · Polonnaruwa');
      expect(Division.fromJson({'id': 1, 'name': 'Ampara'}).label, 'Ampara');
    });

    test('Variety parses its duration', () {
      final variety = Variety.fromJson(varietyJson());
      expect(variety.durationDays, 105);
      expect(variety.notes, isNull);
    });

    test('Field parses every property and labels its area', () {
      final field = Field.fromJson(fieldJson());
      expect(field.area, 2.5);
      expect(field.areaLabel, '2.5 acres');
      expect(field.latitude, 7.94);
      expect(field.longitude, 81.0); // an integral double stays a double
      expect(field.isActive, isTrue);
      expect(Field.fromJson({...fieldJson(), 'area': 1}).areaLabel, '1 acre');
    });
  });

  group('CultivationCycle.fromJson', () {
    test('parses enums, dates, timeline and stage logs', () {
      final cycle = CultivationCycle.fromJson(cycleJson());
      expect(cycle.season, Season.maha);
      expect(cycle.method, CultivationMethod.transplanting);
      expect(cycle.status, CycleStatus.active);
      expect(cycle.currentStage, GrowthStage.tillering);
      expect(cycle.sowingDate, DateTime(2026, 9, 1));
      expect(cycle.actualHarvestDate, isNull);
      expect(cycle.timeline, hasLength(2));
      expect(cycle.stageLogs.single.loggedByUserName, 'Sunil');
      expect(cycle.title, 'Maha 2026');
    });

    test('every CycleStatus wire value parses, and isOpen is Planned/Active only', () {
      for (final status in CycleStatus.values) {
        final cycle = CultivationCycle.fromJson(cycleJson(status: status.wire));
        expect(cycle.status, status);
      }
      expect(CycleStatus.values.where((s) => s.isOpen),
          [CycleStatus.planned, CycleStatus.active]);
    });

    test('every GrowthStage wire value parses; unknown falls back to Nursery', () {
      for (final stage in GrowthStage.values) {
        expect(GrowthStage.fromString(stage.wire), stage);
      }
      expect(GrowthStage.tryParse('Ripening'), isNull);
      expect(GrowthStage.fromString('Ripening'), GrowthStage.nursery);
    });

    test('isBehindTimeline: only an active cycle whose report lags the timeline', () {
      CultivationCycle make(String status, String current, String expected) =>
          CultivationCycle.fromJson(
              cycleJson(status: status, currentStage: current, expectedStageToday: expected));

      expect(make('Active', 'Nursery', 'Tillering').isBehindTimeline, isTrue);
      expect(make('Active', 'Tillering', 'Tillering').isBehindTimeline, isFalse);
      expect(make('Active', 'Flowering', 'Tillering').isBehindTimeline, isFalse);
      expect(make('Planned', 'Nursery', 'Tillering').isBehindTimeline, isFalse);
    });
  });

  group('CultivationPlan.fromJson', () {
    test('parses the plan, its steps and the window label', () {
      final plan = CultivationPlan.fromJson(planJson());
      expect(plan.status, PlanStatus.pendingOfficerApproval);
      expect(plan.summary, 'Rest of the season.');
      final step = plan.steps.single;
      expect(step.stage, GrowthStage.tillering);
      expect(step.category, PlanStepCategory.nutrient);
      expect(step.windowLabel, '20 Sep – 12 Oct');
      expect(plan.assumptions, ['Canal water available.']);
    });

    test('a single-day window shows one date', () {
      final json = planJson();
      final steps = (json['plan'] as Map<String, dynamic>)['steps'] as List<dynamic>;
      (steps.first as Map<String, dynamic>)['windowEnd'] = '2026-09-20';
      expect(CultivationPlan.fromJson(json).steps.single.windowLabel, '20 Sep');
    });

    test('a failed plan with no plan body and validation errors still parses', () {
      final plan = CultivationPlan.fromJson({
        ...planJson(status: 'ValidationFailed'),
        'plan': null,
        'validationErrors': ['Step 1 is in the past.'],
      });
      expect(plan.status, PlanStatus.validationFailed);
      expect(plan.steps, isEmpty);
      expect(plan.validationErrors, ['Step 1 is in the past.']);
    });

    test('each PlanStatus parses, and which ones block or invite a new request', () {
      for (final status in PlanStatus.values) {
        expect(CultivationPlan.fromJson(planJson(status: status.wire)).status, status);
      }
      expect(PlanStatus.values.where((s) => s.blocksNewRequest),
          [PlanStatus.draft, PlanStatus.pendingOfficerApproval, PlanStatus.approved]);
      expect(PlanStatus.values.where((s) => s.invitesNewRequest), [
        PlanStatus.validationFailed,
        PlanStatus.rejected,
        PlanStatus.revisionRequested,
      ]);
      expect(PlanStatus.fromString('Unknown'), PlanStatus.draft);
    });

    test('officer comment and review time parse when present', () {
      final plan = CultivationPlan.fromJson({
        ...planJson(status: 'RevisionRequested'),
        'officerComment': 'Add a water step.',
        'reviewedAt': '2026-09-21T10:00:00Z',
      });
      expect(plan.officerComment, 'Add a water step.');
      expect(plan.reviewedAt!.toUtc(), DateTime.utc(2026, 9, 21, 10));
    });
  });

  group('requests sent to the backend', () {
    test('FieldRequest.toJson sends every editable value (create and full-replace update)', () {
      const request = FieldRequest(
        name: 'Lower paddy',
        area: 2.5,
        soilType: 'Alluvial',
        irrigationType: 'Major tank',
        divisionId: 3,
      );
      expect(request.toJson(), {
        'name': 'Lower paddy',
        'area': 2.5,
        'soilType': 'Alluvial',
        'irrigationType': 'Major tank',
        'divisionId': 3,
        'latitude': null,
        'longitude': null,
      });
    });

    test('StartCycleRequest.toJson sends wire names and a DateOnly sowing date', () {
      final request = StartCycleRequest(
        varietyId: 2,
        season: Season.yala,
        year: 2027,
        method: CultivationMethod.directSeeding,
        sowingDate: DateTime(2027, 4, 3),
      );
      expect(request.toJson(4), {
        'fieldId': 4,
        'varietyId': 2,
        'season': 'Yala',
        'year': 2027,
        'method': 'DirectSeeding',
        'sowingDate': '2027-04-03',
        'notes': null,
      });
    });

    test('client-side limits match the backend DTO attributes', () {
      expect(FieldRules.nameMaxLength, 100);
      expect(FieldRules.areaMin, 0.01);
      expect(FieldRules.areaMax, 1000);
      expect(FieldRules.soilTypeMaxLength, 50);
      expect(CycleRules.maxBackdateDays, 30);
      expect(CycleRules.maxLookaheadDays, 365);
      expect(CycleRules.notesMaxLength, 1000);
      expect(PlanRules.objectiveMaxLength, 1000);
    });
  });
}
