/// Mirrors DTOs/PestDisease/PestDiseaseReportSummaryDto.cs — one diagnosis candidate on an
/// observation. Confidence is a possible-match score (0.0–1.0), never presented as certainty.
class PestDiseaseReport {
  final int id;
  final String possibleIssue;
  final double confidence;
  final String status; // "PendingOfficerReview" | "Approved" | "Rejected" | "RevisionRequested"
  final String? officerComment;
  final DateTime? reviewedAt;
  final DateTime createdAt;

  const PestDiseaseReport({
    required this.id,
    required this.possibleIssue,
    required this.confidence,
    required this.status,
    this.officerComment,
    this.reviewedAt,
    required this.createdAt,
  });

  /// "82%" from 0.82 — matches formatConfidence() in paddywise-web/src/features/pest-disease/types.ts.
  String get confidencePercent => '${(confidence * 100).round()}%';

  factory PestDiseaseReport.fromJson(Map<String, dynamic> json) {
    return PestDiseaseReport(
      id: json['id'] as int,
      possibleIssue: json['possibleIssue'] as String? ?? '',
      confidence: (json['confidence'] as num?)?.toDouble() ?? 0.0,
      status: json['status'] as String? ?? '',
      officerComment: json['officerComment'] as String?,
      reviewedAt: json['reviewedAt'] == null
          ? null
          : DateTime.tryParse(json['reviewedAt'] as String),
      createdAt: DateTime.tryParse(json['createdAt'] as String? ?? '') ??
          DateTime.now(),
    );
  }
}

/// Mirrors REPORT_STATUS_LABELS in paddywise-web/src/features/pest-disease/types.ts — keep
/// these two in sync so the wording matches across apps.
const Map<String, String> reportStatusLabels = {
  'PendingOfficerReview': 'Awaiting officer review',
  'Approved': 'Approved',
  'Rejected': 'Rejected',
  'RevisionRequested': 'Revision requested',
};

String reportStatusLabel(String status) => reportStatusLabels[status] ?? status;
