import 'package:flutter_test/flutter_test.dart';
import 'package:paddywise_mobile/features/pest_disease/models/observation.dart';
import 'package:paddywise_mobile/features/pest_disease/models/pest_disease_report.dart';
import 'package:paddywise_mobile/features/pest_disease/models/cultivation_cycle_summary.dart';

void main() {
  group('Observation.fromJson', () {
    Map<String, dynamic> baseJson({
      String? lastAnalyzedAt,
      List<Map<String, dynamic>> reports = const [],
    }) {
      return {
        'id': 12,
        'cultivationCycleId': 9,
        'fieldId': 9,
        'fieldName': 'QA Test Field',
        'reportedByUserId': 15,
        'reportedByUserName': 'QA Farmer',
        'observationType': 'Disease',
        'cropStage': 'Nursery',
        'symptoms': 'Brownish oval lesions with gray centers.',
        'severity': 'Moderate',
        'imageUrl': null,
        'lastAnalyzedAt': lastAnalyzedAt,
        'createdAt': '2026-09-29T09:45:36.017687Z',
        'updatedAt': '2026-09-29T09:45:36.017687Z',
        'reports': reports,
      };
    }

    test('never analysed: lastAnalyzedAt null, no reports', () {
      final observation = Observation.fromJson(baseJson());

      expect(observation.id, 12);
      expect(observation.observationType, 'Disease');
      expect(observation.lastAnalyzedAt, isNull);
      expect(observation.reports, isEmpty);
    });

    test('analysed, no likely match: lastAnalyzedAt set, empty reports', () {
      final observation = Observation.fromJson(
        baseJson(lastAnalyzedAt: '2026-09-29T09:50:00.000000Z'),
      );

      expect(observation.lastAnalyzedAt, isNotNull);
      expect(observation.reports, isEmpty);
    });

    test('analysed with candidates: reports parsed', () {
      final observation = Observation.fromJson(baseJson(
        lastAnalyzedAt: '2026-09-29T09:50:00.000000Z',
        reports: [
          {
            'id': 5,
            'possibleIssue': 'Rice Blast',
            'confidence': 0.85,
            'status': 'PendingOfficerReview',
            'officerComment': null,
            'reviewedAt': null,
            'createdAt': '2026-09-29T09:50:00.000000Z',
          },
        ],
      ));

      expect(observation.reports, hasLength(1));
      expect(observation.reports.first.possibleIssue, 'Rice Blast');
      expect(observation.reports.first.confidence, 0.85);
    });
  });

  group('PestDiseaseReport.fromJson', () {
    test('parses a reviewed report with an officer comment', () {
      final report = PestDiseaseReport.fromJson({
        'id': 6,
        'possibleIssue': 'Brown Spot',
        'confidence': 0.35,
        'status': 'Approved',
        'officerComment': 'Confirmed on field visit.',
        'reviewedAt': '2026-09-29T10:00:00.000000Z',
        'createdAt': '2026-09-29T09:50:00.000000Z',
      });

      expect(report.status, 'Approved');
      expect(report.officerComment, 'Confirmed on field visit.');
      expect(report.reviewedAt, isNotNull);
      expect(report.confidencePercent, '35%');
    });
  });

  group('CultivationCycleSummary.fromJson', () {
    test('builds the same label shape ObservationForm.tsx uses', () {
      final cycle = CultivationCycleSummary.fromJson({
        'id': 9,
        'fieldName': 'QA Test Field',
        'season': 'Maha',
        'year': 2026,
        'currentStage': 'Nursery',
      });

      expect(cycle.label, 'QA Test Field — Maha 2026 (Nursery)');
    });
  });

  group('CreateObservationRequest.toJson / UpdateObservationRequest.toJson', () {
    test('create request round-trips all fields', () {
      const request = CreateObservationRequest(
        cultivationCycleId: 9,
        observationType: 'Pest',
        symptoms: 'Small brown insects on leaf undersides.',
        severity: 'Low',
        imageUrl: null,
      );

      expect(request.toJson(), {
        'cultivationCycleId': 9,
        'observationType': 'Pest',
        'symptoms': 'Small brown insects on leaf undersides.',
        'severity': 'Low',
        'imageUrl': null,
      });
    });

    test('update request omits cultivationCycleId', () {
      const request = UpdateObservationRequest(
        observationType: 'Disease',
        symptoms: 'Updated symptoms.',
        severity: 'Severe',
      );

      final json = request.toJson();
      expect(json.containsKey('cultivationCycleId'), isFalse);
      expect(json['observationType'], 'Disease');
      expect(json['severity'], 'Severe');
    });
  });
}
