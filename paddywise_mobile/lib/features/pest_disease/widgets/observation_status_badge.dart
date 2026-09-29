import 'package:flutter/material.dart';
import '../../../theme/app_theme.dart';
import '../models/observation.dart';
import '../models/pest_disease_report.dart';

enum ObservationStatusKind {
  notAnalysed,
  noMatch,
  pendingReview,
  approved,
  revisionRequested,
  rejected,
}

class ObservationListStatus {
  final String label;
  final ObservationStatusKind kind;

  const ObservationListStatus(this.label, this.kind);
}

/// The list card's single-line summary status. Mirrors the four states specified for the
/// mobile "My Observations" list: not analysed yet; analysed with no likely match; N
/// possible issues awaiting officer review; or the officer's decision once any candidate has
/// been reviewed.
///
/// An observation can carry several candidate reports, each reviewed independently by an
/// officer, but the list card shows one summary line — once any candidate has been reviewed,
/// this picks the most actionable outcome to surface (Approved > Revision requested >
/// Rejected) rather than every candidate's status individually; the detail screen shows each
/// report's own status.
ObservationListStatus computeObservationListStatus(Observation observation) {
  if (observation.lastAnalyzedAt == null) {
    return const ObservationListStatus(
        'Not analysed yet', ObservationStatusKind.notAnalysed);
  }

  if (observation.reports.isEmpty) {
    return const ObservationListStatus(
        'Analysed — no likely match', ObservationStatusKind.noMatch);
  }

  for (final status in const ['Approved', 'RevisionRequested', 'Rejected']) {
    final hasStatus = observation.reports.any((report) => report.status == status);
    if (hasStatus) {
      return ObservationListStatus(reportStatusLabel(status), _kindForStatus(status));
    }
  }

  final pendingCount = observation.reports.length;
  final noun = pendingCount == 1 ? 'issue' : 'issues';
  return ObservationListStatus(
    '$pendingCount possible $noun — awaiting officer review',
    ObservationStatusKind.pendingReview,
  );
}

ObservationStatusKind _kindForStatus(String status) {
  switch (status) {
    case 'Approved':
      return ObservationStatusKind.approved;
    case 'RevisionRequested':
      return ObservationStatusKind.revisionRequested;
    case 'Rejected':
      return ObservationStatusKind.rejected;
    default:
      return ObservationStatusKind.pendingReview;
  }
}

class ObservationStatusBadge extends StatelessWidget {
  final Observation observation;

  const ObservationStatusBadge({super.key, required this.observation});

  @override
  Widget build(BuildContext context) {
    final status = computeObservationListStatus(observation);
    final (Color bg, Color fg) = switch (status.kind) {
      ObservationStatusKind.notAnalysed => (AppColors.line, AppColors.inkSoft),
      ObservationStatusKind.noMatch => (AppColors.creamDeep, AppColors.inkSoft),
      ObservationStatusKind.pendingReview => (AppColors.badgeBuyerBg, AppColors.badgeBuyerText),
      ObservationStatusKind.approved => (AppColors.successBg, AppColors.successText),
      ObservationStatusKind.revisionRequested => (AppColors.badgeBuyerBg, AppColors.badgeBuyerText),
      ObservationStatusKind.rejected => (AppColors.errorBg, AppColors.errorText),
    };

    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
      decoration: BoxDecoration(
        color: bg,
        borderRadius: BorderRadius.circular(9999),
      ),
      child: Text(
        status.label,
        style: TextStyle(fontSize: 12, fontWeight: FontWeight.w600, color: fg),
      ),
    );
  }
}
