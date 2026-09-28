import 'package:flutter_test/flutter_test.dart';
import 'package:paddywise_mobile/features/field_cultivation/models/cycle_models.dart';
import 'package:paddywise_mobile/features/field_cultivation/models/field_models.dart';
import 'package:paddywise_mobile/features/field_cultivation/models/plan_models.dart';

void main() {
  test('Field parses FieldResponseDto, including integer area', () {
    final field = Field.fromJson({
      'id': 4,
      'name': 'Lower paddy',
      'area': 3,
      'soilType': 'Alluvial',
      'irrigationType': 'Major tank',
      'latitude': null,
      'longitude': 80.77,
      'divisionId': 2,
      'divisionName': 'Polonnaruwa',
      'farmerId': 9,
      'farmerName': 'Sunil',
      'isActive': true,
    });

    expect(field.area, 3.0);
    expect(field.areaLabel, '3 acres');
    expect(field.latitude, isNull);
    expect(field.longitude, 80.77);
  });

  test('CultivationCycle parses enums, DateOnly values and stage logs', () {
    final cycle = CultivationCycle.fromJson({
      'id': 12,
      'fieldId': 4,
      'fieldName': 'Lower paddy',
      'varietyId': 1,
      'varietyName': 'Bg 352',
      'durationDays': 105,
      'season': 'Maha',
      'year': 2026,
      'method': 'DirectSeeding',
      'sowingDate': '2026-09-01',
      'expectedHarvestDate': '2026-12-15',
      'actualHarvestDate': null,
      'currentStage': 'PanicleInitiation',
      'expectedStageToday': 'Flowering',
      'status': 'Active',
      'notes': null,
      'timeline': [
        {'stage': 'Nursery', 'start': '2026-09-01', 'end': '2026-09-16'},
      ],
      'stageLogs': [
        {
          'id': 1,
          'stage': 'Tillering',
          'observedOn': '2026-09-20',
          'notes': 'Even',
          'loggedByUserId': 9,
          'loggedByUserName': 'Sunil',
          'createdAt': '2026-09-20T04:00:00Z',
        },
      ],
    });

    expect(cycle.season, Season.maha);
    expect(cycle.method, CultivationMethod.directSeeding);
    expect(cycle.status, CycleStatus.active);
    expect(cycle.sowingDate, DateTime(2026, 9, 1));
    expect(cycle.timeline.single.end, DateTime(2026, 9, 16));
    expect(cycle.stageLogs.single.stage, GrowthStage.tillering);
    expect(cycle.isBehindTimeline, isTrue);
    expect(toDateOnly(DateTime(2026, 3, 7)), '2026-03-07');
  });

  test('CultivationPlan tolerates a failed plan with odd stages and dates', () {
    final plan = CultivationPlan.fromJson({
      'id': 7,
      'cycleId': 12,
      'objective': 'Good yield',
      'status': 'ValidationFailed',
      'plan': {
        'summary': 'Draft',
        'steps': [
          {
            'stage': 'Ripening',
            'windowStart': 'not-a-date',
            'windowEnd': '2026-10-01',
            'task': 'Drain field',
            'rationale': '',
            'category': 'Water',
          },
        ],
        'delegations': [],
        'assumptions': ['Tank water available'],
      },
      'validationErrors': ['Stage "Ripening" is not a growth stage.'],
      'officerComment': null,
      'reviewedAt': null,
      'createdAt': '2026-09-20T04:00:00Z',
      'agentRuns': [],
    });

    expect(plan.status, PlanStatus.validationFailed);
    expect(plan.status.invitesNewRequest, isTrue);
    expect(plan.steps.single.stage, isNull);
    expect(plan.steps.single.stageLabel, 'Ripening');
    expect(plan.steps.single.windowLabel, '');
    expect(plan.steps.single.category, PlanStepCategory.water);
    expect(plan.validationErrors, hasLength(1));
  });
}
