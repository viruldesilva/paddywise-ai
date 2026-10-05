import 'package:flutter/material.dart';

import '../../../theme/app_theme.dart';
import '../models/cycle_models.dart';
import '../models/plan_models.dart';

class _Pill extends StatelessWidget {
  final String label;
  final Color background;
  final Color foreground;

  const _Pill(this.label, this.background, this.foreground);

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
      decoration: BoxDecoration(color: background, borderRadius: BorderRadius.circular(20)),
      child: Text(
        label.toUpperCase(),
        style: TextStyle(
          fontSize: 10,
          fontWeight: FontWeight.bold,
          letterSpacing: 0.6,
          color: foreground,
        ),
      ),
    );
  }
}

class CycleStatusBadge extends StatelessWidget {
  final CycleStatus status;
  const CycleStatusBadge(this.status, {super.key});

  @override
  Widget build(BuildContext context) {
    return switch (status) {
      CycleStatus.planned =>
        _Pill(status.label, AppColors.badgeBuyerBg, AppColors.badgeBuyerText),
      CycleStatus.active => _Pill(status.label, AppColors.successBg, AppColors.successText),
      CycleStatus.harvested =>
        _Pill(status.label, AppColors.badgeOfficerBg, AppColors.badgeOfficerText),
      CycleStatus.abandoned =>
        _Pill(status.label, AppColors.creamDeep, AppColors.inkSoft),
    };
  }
}

class PlanStatusBadge extends StatelessWidget {
  final PlanStatus status;
  const PlanStatusBadge(this.status, {super.key});

  @override
  Widget build(BuildContext context) {
    return switch (status) {
      PlanStatus.draft => _Pill(status.label, AppColors.creamDeep, AppColors.inkSoft),
      PlanStatus.pendingOfficerApproval =>
        _Pill(status.label, AppColors.badgeBuyerBg, AppColors.badgeBuyerText),
      PlanStatus.approved => _Pill(status.label, AppColors.successBg, AppColors.successText),
      PlanStatus.validationFailed ||
      PlanStatus.rejected =>
        _Pill(status.label, AppColors.errorBg, AppColors.errorText),
      PlanStatus.revisionRequested =>
        _Pill(status.label, AppColors.badgeAdminBg, AppColors.badgeAdminText),
    };
  }
}
