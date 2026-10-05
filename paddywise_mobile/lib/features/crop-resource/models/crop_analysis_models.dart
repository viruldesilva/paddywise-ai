/// Agentic AI Models for Crop Activity Analysis matching backend DTOs.
library;

class FieldOverview {
  final int cycleId;
  final String fieldName;
  final String varietyName;
  final String ageGroup;
  final int daysAfterSowing;
  final String currentStage;
  final String season;
  final int year;
  final String divisionName;
  final String district;

  const FieldOverview({
    required this.cycleId,
    required this.fieldName,
    required this.varietyName,
    required this.ageGroup,
    required this.daysAfterSowing,
    required this.currentStage,
    required this.season,
    required this.year,
    required this.divisionName,
    required this.district,
  });

  factory FieldOverview.fromJson(Map<String, dynamic> json) {
    return FieldOverview(
      cycleId: json['cycleId'] as int? ?? 0,
      fieldName: json['fieldName'] as String? ?? 'Field',
      varietyName: json['varietyName'] as String? ?? 'Rice Variety',
      ageGroup: json['ageGroup'] as String? ?? '3.5 month',
      daysAfterSowing: json['daysAfterSowing'] as int? ?? 0,
      currentStage: json['currentStage'] as String? ?? 'Tillering',
      season: json['season'] as String? ?? 'Yala',
      year: json['year'] as int? ?? DateTime.now().year,
      divisionName: json['divisionName'] as String? ?? '',
      district: json['district'] as String? ?? '',
    );
  }
}

class WaterDiagnostic {
  final String status;
  final double? latestWaterLevelCm;
  final int totalIrrigationEvents;
  final double totalDurationHours;
  final int daysSinceLastIrrigation;
  final String assessment;

  const WaterDiagnostic({
    required this.status,
    this.latestWaterLevelCm,
    required this.totalIrrigationEvents,
    required this.totalDurationHours,
    required this.daysSinceLastIrrigation,
    required this.assessment,
  });

  factory WaterDiagnostic.fromJson(Map<String, dynamic> json) {
    return WaterDiagnostic(
      status: json['status'] as String? ?? 'Adequate',
      latestWaterLevelCm: (json['latestWaterLevelCm'] as num?)?.toDouble(),
      totalIrrigationEvents: json['totalIrrigationEvents'] as int? ?? 0,
      totalDurationHours: (json['totalDurationHours'] as num?)?.toDouble() ?? 0.0,
      daysSinceLastIrrigation: json['daysSinceLastIrrigation'] as int? ?? 0,
      assessment: json['assessment'] as String? ?? '',
    );
  }
}

class FertilizerDiagnostic {
  final String status;
  final double totalUreaKgPerHa;
  final double totalTspKgPerHa;
  final double totalMopKgPerHa;
  final int applicationsCount;
  final int daysSinceLastFertilizer;
  final String splitCompliance;
  final String assessment;

  const FertilizerDiagnostic({
    required this.status,
    required this.totalUreaKgPerHa,
    required this.totalTspKgPerHa,
    required this.totalMopKgPerHa,
    required this.applicationsCount,
    required this.daysSinceLastFertilizer,
    required this.splitCompliance,
    required this.assessment,
  });

  factory FertilizerDiagnostic.fromJson(Map<String, dynamic> json) {
    return FertilizerDiagnostic(
      status: json['status'] as String? ?? 'Balanced',
      totalUreaKgPerHa: (json['totalUreaKgPerHa'] as num?)?.toDouble() ?? 0.0,
      totalTspKgPerHa: (json['totalTspKgPerHa'] as num?)?.toDouble() ?? 0.0,
      totalMopKgPerHa: (json['totalMopKgPerHa'] as num?)?.toDouble() ?? 0.0,
      applicationsCount: json['applicationsCount'] as int? ?? 0,
      daysSinceLastFertilizer: json['daysSinceLastFertilizer'] as int? ?? 0,
      splitCompliance: json['splitCompliance'] as String? ?? 'Active',
      assessment: json['assessment'] as String? ?? '',
    );
  }
}

class PestDiagnostic {
  final String status;
  final int treatmentsCount;
  final List<String> productsUsed;
  final List<String> targetPests;
  final int daysSinceLastTreatment;
  final bool bannedChemicalDetected;
  final String assessment;

  const PestDiagnostic({
    required this.status,
    required this.treatmentsCount,
    required this.productsUsed,
    required this.targetPests,
    required this.daysSinceLastTreatment,
    required this.bannedChemicalDetected,
    required this.assessment,
  });

  factory PestDiagnostic.fromJson(Map<String, dynamic> json) {
    return PestDiagnostic(
      status: json['status'] as String? ?? 'Safe',
      treatmentsCount: json['treatmentsCount'] as int? ?? 0,
      productsUsed: (json['productsUsed'] as List<dynamic>?)?.map((e) => e.toString()).toList() ?? [],
      targetPests: (json['targetPests'] as List<dynamic>?)?.map((e) => e.toString()).toList() ?? [],
      daysSinceLastTreatment: json['daysSinceLastTreatment'] as int? ?? 0,
      bannedChemicalDetected: json['bannedChemicalDetected'] as bool? ?? false,
      assessment: json['assessment'] as String? ?? '',
    );
  }
}

class OtherDiagnostic {
  final int activitiesCount;
  final List<String> activityTypes;
  final String weedingStatus;
  final String assessment;

  const OtherDiagnostic({
    required this.activitiesCount,
    required this.activityTypes,
    required this.weedingStatus,
    required this.assessment,
  });

  factory OtherDiagnostic.fromJson(Map<String, dynamic> json) {
    return OtherDiagnostic(
      activitiesCount: json['activitiesCount'] as int? ?? 0,
      activityTypes: (json['activityTypes'] as List<dynamic>?)?.map((e) => e.toString()).toList() ?? [],
      weedingStatus: json['weedingStatus'] as String? ?? 'Not Recorded',
      assessment: json['assessment'] as String? ?? '',
    );
  }
}

class DiagnosticsSummary {
  final WaterDiagnostic water;
  final FertilizerDiagnostic fertilizer;
  final PestDiagnostic pest;
  final OtherDiagnostic other;

  const DiagnosticsSummary({
    required this.water,
    required this.fertilizer,
    required this.pest,
    required this.other,
  });

  factory DiagnosticsSummary.fromJson(Map<String, dynamic> json) {
    return DiagnosticsSummary(
      water: WaterDiagnostic.fromJson(json['water'] as Map<String, dynamic>? ?? {}),
      fertilizer: FertilizerDiagnostic.fromJson(json['fertilizer'] as Map<String, dynamic>? ?? {}),
      pest: PestDiagnostic.fromJson(json['pest'] as Map<String, dynamic>? ?? {}),
      other: OtherDiagnostic.fromJson(json['other'] as Map<String, dynamic>? ?? {}),
    );
  }
}

class Citation {
  final String document;
  final String section;

  const Citation({
    required this.document,
    required this.section,
  });

  factory Citation.fromJson(Map<String, dynamic> json) {
    return Citation(
      document: json['document'] as String? ?? '',
      section: json['section'] as String? ?? '',
    );
  }
}

class ActivityRecommendation {
  final String id;
  final int? dbId;
  final String category;
  final String priority; // "HIGH", "MEDIUM", "LOW"
  final String action;
  final String reason;
  final String evidence;
  final double confidenceScore;
  final List<Citation> citations;
  final bool requiresOfficerReview;
  final String status; // "PENDING_OFFICER_REVIEW", "APPROVED", "REJECTED", "EXECUTED"
  final String? executionPayloadJson;
  final String? reviewedBy;
  final String? reviewNotes;
  final DateTime? reviewedAt;
  final int? executedActivityId;
  final DateTime? executedAt;

  const ActivityRecommendation({
    required this.id,
    this.dbId,
    required this.category,
    required this.priority,
    required this.action,
    required this.reason,
    required this.evidence,
    required this.confidenceScore,
    required this.citations,
    required this.requiresOfficerReview,
    this.status = 'PENDING_OFFICER_REVIEW',
    this.executionPayloadJson,
    this.reviewedBy,
    this.reviewNotes,
    this.reviewedAt,
    this.executedActivityId,
    this.executedAt,
  });

  factory ActivityRecommendation.fromJson(Map<String, dynamic> json) {
    return ActivityRecommendation(
      id: json['id']?.toString() ?? json['recommendationUid']?.toString() ?? '',
      dbId: json['dbId'] as int? ?? (json['id'] is int ? json['id'] as int : null),
      category: json['category'] as String? ?? 'General',
      priority: json['priority'] as String? ?? 'MEDIUM',
      action: json['action'] as String? ?? '',
      reason: json['reason'] as String? ?? '',
      evidence: json['evidence'] as String? ?? '',
      confidenceScore: (json['confidenceScore'] as num?)?.toDouble() ?? 0.85,
      citations: (json['citations'] as List<dynamic>?)
              ?.map((c) => Citation.fromJson(c as Map<String, dynamic>))
              .toList() ??
          [],
      requiresOfficerReview: json['requiresOfficerReview'] as bool? ?? false,
      status: json['status'] as String? ?? 'PENDING_OFFICER_REVIEW',
      executionPayloadJson: json['executionPayloadJson'] as String?,
      reviewedBy: json['reviewedBy'] as String? ?? json['officerName'] as String?,
      reviewNotes: json['reviewNotes'] as String? ?? json['officerComment'] as String?,
      reviewedAt: json['reviewedAt'] != null ? DateTime.tryParse(json['reviewedAt'].toString()) : null,
      executedActivityId: json['executedActivityId'] as int?,
      executedAt: json['executedAt'] != null ? DateTime.tryParse(json['executedAt'].toString()) : null,
    );
  }

  ActivityRecommendation copyWith({
    String? id,
    int? dbId,
    String? category,
    String? priority,
    String? action,
    String? reason,
    String? evidence,
    double? confidenceScore,
    List<Citation>? citations,
    bool? requiresOfficerReview,
    String? status,
    String? executionPayloadJson,
    String? reviewedBy,
    String? reviewNotes,
    DateTime? reviewedAt,
    int? executedActivityId,
    DateTime? executedAt,
  }) {
    return ActivityRecommendation(
      id: id ?? this.id,
      dbId: dbId ?? this.dbId,
      category: category ?? this.category,
      priority: priority ?? this.priority,
      action: action ?? this.action,
      reason: reason ?? this.reason,
      evidence: evidence ?? this.evidence,
      confidenceScore: confidenceScore ?? this.confidenceScore,
      citations: citations ?? this.citations,
      requiresOfficerReview: requiresOfficerReview ?? this.requiresOfficerReview,
      status: status ?? this.status,
      executionPayloadJson: executionPayloadJson ?? this.executionPayloadJson,
      reviewedBy: reviewedBy ?? this.reviewedBy,
      reviewNotes: reviewNotes ?? this.reviewNotes,
      reviewedAt: reviewedAt ?? this.reviewedAt,
      executedActivityId: executedActivityId ?? this.executedActivityId,
      executedAt: executedAt ?? this.executedAt,
    );
  }
}

class CropActivityAnalysisOutput {
  final FieldOverview fieldOverview;
  final DiagnosticsSummary diagnostics;
  final List<ActivityRecommendation> recommendations;
  final List<String> warnings;
  final bool requiresOfficerReview;
  final String executiveSummary;
  final String analyzedAt;

  const CropActivityAnalysisOutput({
    required this.fieldOverview,
    required this.diagnostics,
    required this.recommendations,
    required this.warnings,
    required this.requiresOfficerReview,
    required this.executiveSummary,
    required this.analyzedAt,
  });

  factory CropActivityAnalysisOutput.fromJson(Map<String, dynamic> json) {
    return CropActivityAnalysisOutput(
      fieldOverview: FieldOverview.fromJson(json['fieldOverview'] as Map<String, dynamic>? ?? {}),
      diagnostics: DiagnosticsSummary.fromJson(json['diagnostics'] as Map<String, dynamic>? ?? {}),
      recommendations: (json['recommendations'] as List<dynamic>?)
              ?.map((r) => ActivityRecommendation.fromJson(r as Map<String, dynamic>))
              .toList() ??
          [],
      warnings: (json['warnings'] as List<dynamic>?)?.map((w) => w.toString()).toList() ?? [],
      requiresOfficerReview: json['requiresOfficerReview'] as bool? ?? false,
      executiveSummary: json['executiveSummary'] as String? ?? '',
      analyzedAt: json['analyzedAt'] as String? ?? DateTime.now().toIso8601String(),
    );
  }
}

class AiChatMessage {
  final String role; // "user" or "assistant"
  final String content;

  const AiChatMessage({
    required this.role,
    required this.content,
  });

  Map<String, dynamic> toJson() => {
        'role': role,
        'content': content,
      };

  factory AiChatMessage.fromJson(Map<String, dynamic> json) {
    return AiChatMessage(
      role: json['role'] as String? ?? 'user',
      content: json['content'] as String? ?? '',
    );
  }
}

class AiChatResponse {
  final String answer;
  final List<String> suggestedFollowUps;
  final bool requiresOfficerReview;
  final String answeredAt;

  const AiChatResponse({
    required this.answer,
    required this.suggestedFollowUps,
    required this.requiresOfficerReview,
    required this.answeredAt,
  });

  factory AiChatResponse.fromJson(Map<String, dynamic> json) {
    return AiChatResponse(
      answer: json['answer'] as String? ?? '',
      suggestedFollowUps: (json['suggestedFollowUps'] as List<dynamic>?)?.map((s) => s.toString()).toList() ?? [],
      requiresOfficerReview: json['requiresOfficerReview'] as bool? ?? false,
      answeredAt: json['answeredAt'] as String? ?? DateTime.now().toIso8601String(),
    );
  }
}
