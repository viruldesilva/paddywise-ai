/// Cultivation plan DTOs, mirroring `CultivationPlanResponseDto` and
/// `Agents/FieldCultivation/CultivationPlanModels.cs`.
library;

import 'package:flutter/material.dart';

import 'cycle_models.dart';

enum PlanStatus {
  draft('Draft', 'Being prepared',
      'The planning assistant is still working on this plan.'),
  validationFailed('ValidationFailed', 'Needs another try',
      'The plan did not pass our safety checks, so it was not sent to an officer. You can ask for a new one.'),
  pendingOfficerApproval('PendingOfficerApproval', 'Waiting for officer',
      'Your agricultural officer will review this plan before you follow it.'),
  approved('Approved', 'Approved',
      'Your agricultural officer approved this plan. Follow the steps below through the season.'),
  rejected('Rejected', 'Rejected',
      'Your agricultural officer did not approve this plan. Read their comment and ask for a new one.'),
  revisionRequested('RevisionRequested', 'Changes requested',
      'Your agricultural officer asked for changes. Read their comment and ask for a new plan.');

  final String wire;
  final String label;
  final String description;
  const PlanStatus(this.wire, this.label, this.description);

  static PlanStatus fromString(String? value) =>
      values.firstWhere((s) => s.wire == value, orElse: () => draft);

  /// The backend refuses a new request while a plan is being generated,
  /// pending or approved.
  bool get blocksNewRequest =>
      this == draft || this == pendingOfficerApproval || this == approved;

  /// The farmer is expected to ask again after these outcomes.
  bool get invitesNewRequest =>
      this == validationFailed || this == rejected || this == revisionRequested;
}

enum PlanStepCategory {
  landPrep('LandPrep', 'Land preparation', Icons.agriculture_outlined),
  water('Water', 'Water', Icons.water_drop_outlined),
  nutrient('Nutrient', 'Nutrients', Icons.science_outlined),
  protection('Protection', 'Crop protection', Icons.shield_outlined),
  monitoring('Monitoring', 'Monitoring', Icons.visibility_outlined),
  harvest('Harvest', 'Harvest', Icons.grass_outlined),
  other('', 'Other', Icons.task_alt_outlined);

  final String wire;
  final String label;
  final IconData icon;
  const PlanStepCategory(this.wire, this.label, this.icon);

  static PlanStepCategory fromString(String? value) =>
      values.firstWhere((c) => c.wire == value, orElse: () => other);
}

/// One dated task in the plan.
class PlanStep {
  final String stageRaw;
  final GrowthStage? stage;
  final DateTime? windowStart;
  final DateTime? windowEnd;
  final String task;
  final String rationale;
  final PlanStepCategory category;

  const PlanStep({
    required this.stageRaw,
    required this.stage,
    required this.windowStart,
    required this.windowEnd,
    required this.task,
    required this.rationale,
    required this.category,
  });

  factory PlanStep.fromJson(Map<String, dynamic> json) {
    final stageRaw = json['stage'] as String? ?? '';
    return PlanStep(
      stageRaw: stageRaw,
      stage: GrowthStage.tryParse(stageRaw),
      // A plan that failed validation may carry malformed dates.
      windowStart: _tryDate(json['windowStart']),
      windowEnd: _tryDate(json['windowEnd']),
      task: json['task'] as String? ?? '',
      rationale: json['rationale'] as String? ?? '',
      category: PlanStepCategory.fromString(json['category'] as String?),
    );
  }

  String get stageLabel => stage?.label ?? (stageRaw.isEmpty ? 'Other' : stageRaw);

  String get windowLabel {
    if (windowStart == null || windowEnd == null) return '';
    if (windowStart == windowEnd) return formatShortDate(windowStart!);
    return '${formatShortDate(windowStart!)} – ${formatShortDate(windowEnd!)}';
  }
}

DateTime? _tryDate(dynamic value) {
  if (value is! String || value.isEmpty) return null;
  try {
    return parseDateOnly(value);
  } catch (_) {
    return null;
  }
}

/// A cultivation plan as the farmer sees it (CultivationPlanResponseDto).
/// Agent run logs are left out: they are for officers on the web.
class CultivationPlan {
  final int id;
  final int cycleId;
  final String objective;
  final PlanStatus status;
  final String? summary;
  final List<PlanStep> steps;
  final List<String> assumptions;
  final List<String> validationErrors;
  final String? officerComment;
  final DateTime? reviewedAt;
  final DateTime createdAt;

  const CultivationPlan({
    required this.id,
    required this.cycleId,
    required this.objective,
    required this.status,
    required this.summary,
    required this.steps,
    required this.assumptions,
    required this.validationErrors,
    required this.officerComment,
    required this.reviewedAt,
    required this.createdAt,
  });

  factory CultivationPlan.fromJson(Map<String, dynamic> json) {
    final plan = json['plan'] as Map<String, dynamic>?;
    return CultivationPlan(
      id: json['id'] as int,
      cycleId: json['cycleId'] as int? ?? 0,
      objective: json['objective'] as String? ?? '',
      status: PlanStatus.fromString(json['status'] as String?),
      summary: plan?['summary'] as String?,
      steps: (plan?['steps'] as List<dynamic>? ?? [])
          .map((e) => PlanStep.fromJson(e as Map<String, dynamic>))
          .toList(),
      assumptions: (plan?['assumptions'] as List<dynamic>? ?? [])
          .map((e) => e.toString())
          .toList(),
      validationErrors: (json['validationErrors'] as List<dynamic>? ?? [])
          .map((e) => e.toString())
          .toList(),
      officerComment: json['officerComment'] as String?,
      reviewedAt: DateTime.tryParse(json['reviewedAt'] as String? ?? '')?.toLocal(),
      createdAt:
          DateTime.tryParse(json['createdAt'] as String? ?? '')?.toLocal() ?? DateTime.now(),
    );
  }
}

/// RequestPlanDto's Objective [MaxLength(1000)].
class PlanRules {
  static const int objectiveMaxLength = 1000;
}
