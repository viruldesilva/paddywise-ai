import 'package:flutter_test/flutter_test.dart';
import 'package:paddywise_mobile/features/pest_disease/models/observation.dart';
import 'package:paddywise_mobile/features/pest_disease/models/pest_disease_report.dart';
import 'package:paddywise_mobile/features/pest_disease/widgets/observation_status_badge.dart';

Observation _observation({
  DateTime? lastAnalyzedAt,
  List<PestDiseaseReport> reports = const [],
}) {
  final now = DateTime.now();
  return Observation(
    id: 1,
    cultivationCycleId: 1,
    fieldId: 1,
    fieldName: 'Test Field',
    reportedByUserId: 1,
    reportedByUserName: 'Farmer',
    observationType: 'Disease',
    cropStage: 'Nursery',
    symptoms: 'Test symptoms',
    severity: 'Low',
    lastAnalyzedAt: lastAnalyzedAt,
    createdAt: now,
    updatedAt: now,
    reports: reports,
  );
}

PestDiseaseReport _report(String status) {
  final now = DateTime.now();
  return PestDiseaseReport(
    id: 1,
    possibleIssue: 'Rice Blast',
    confidence: 0.5,
    status: status,
    createdAt: now,
  );
}

void main() {
  group('computeObservationListStatus', () {
    test('no lastAnalyzedAt -> "Not analysed yet"', () {
      final status = computeObservationListStatus(_observation());
      expect(status.label, 'Not analysed yet');
      expect(status.kind, ObservationStatusKind.notAnalysed);
    });

    test('lastAnalyzedAt set, no reports -> "Analysed — no likely match"', () {
      final status = computeObservationListStatus(
        _observation(lastAnalyzedAt: DateTime.now()),
      );
      expect(status.label, 'Analysed — no likely match');
      expect(status.kind, ObservationStatusKind.noMatch);
    });

    test('reports pending -> "N possible issue(s) — awaiting officer review"', () {
      final single = computeObservationListStatus(_observation(
        lastAnalyzedAt: DateTime.now(),
        reports: [_report('PendingOfficerReview')],
      ));
      expect(single.label, '1 possible issue — awaiting officer review');
      expect(single.kind, ObservationStatusKind.pendingReview);

      final multiple = computeObservationListStatus(_observation(
        lastAnalyzedAt: DateTime.now(),
        reports: [_report('PendingOfficerReview'), _report('PendingOfficerReview')],
      ));
      expect(multiple.label, '2 possible issues — awaiting officer review');
    });

    test('an approved report wins over any still-pending ones', () {
      final status = computeObservationListStatus(_observation(
        lastAnalyzedAt: DateTime.now(),
        reports: [_report('PendingOfficerReview'), _report('Approved')],
      ));
      expect(status.label, 'Approved');
      expect(status.kind, ObservationStatusKind.approved);
    });

    test('revision requested surfaces over rejected', () {
      final status = computeObservationListStatus(_observation(
        lastAnalyzedAt: DateTime.now(),
        reports: [_report('Rejected'), _report('RevisionRequested')],
      ));
      expect(status.label, 'Revision requested');
      expect(status.kind, ObservationStatusKind.revisionRequested);
    });

    test('rejected shows when nothing else applies', () {
      final status = computeObservationListStatus(_observation(
        lastAnalyzedAt: DateTime.now(),
        reports: [_report('Rejected')],
      ));
      expect(status.label, 'Rejected');
      expect(status.kind, ObservationStatusKind.rejected);
    });
  });
}
