import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

import '../../../theme/app_theme.dart';
import '../models/cycle_models.dart';
import '../models/plan_models.dart';
import '../services/field_cultivation_service.dart';
import '../services/plan_poller.dart';
import '../widgets/fc_common.dart';
import '../widgets/status_badges.dart';

/// A cultivation plan as the farmer follows it: status in plain words, the
/// officer's comment, and dated tasks grouped by growth stage.
class PlanDetailScreen extends StatefulWidget {
  final int planId;

  const PlanDetailScreen({super.key, required this.planId});

  @override
  State<PlanDetailScreen> createState() => _PlanDetailScreenState();
}

class _PlanDetailScreenState extends State<PlanDetailScreen> {
  CultivationPlan? _plan;
  String? _errorMessage;

  /// Set while the plan is still being generated (Draft); see [_watchIfGenerating].
  PlanPoller? _poller;
  String? _pollMessage;

  @override
  void initState() {
    super.initState();
    _load();
  }

  @override
  void dispose() {
    _poller?.cancel();
    super.dispose();
  }

  Future<void> _load() async {
    try {
      final plan = await FieldCultivationService.getPlan(widget.planId);
      if (!mounted) return;
      setState(() {
        _plan = plan;
        _errorMessage = null;
      });
      _watchIfGenerating(plan);
    } catch (e) {
      if (!mounted) return;
      setState(() => _errorMessage = e.toString());
    }
  }

  /// A Draft is still being generated in the background: keep checking until
  /// it becomes something the farmer can act on.
  void _watchIfGenerating(CultivationPlan plan) {
    if (plan.status != PlanStatus.draft || (_poller?.isActive ?? false)) return;

    _pollMessage = null;
    _poller = PlanPoller(
      planId: plan.id,
      onUpdate: (updated) {
        if (mounted) setState(() => _plan = updated);
      },
      onError: (message, {required timedOut}) {
        if (mounted) setState(() => _pollMessage = message);
      },
    )..start();
  }

  Future<void> _requestAgain() async {
    final plan = _plan!;
    final created =
        await context.push<CultivationPlan>('/cycles/${plan.cycleId}/plans/new');
    if (created != null && mounted) {
      context.pushReplacement('/plans/${created.id}');
    }
  }

  @override
  Widget build(BuildContext context) {
    final plan = _plan;

    return Scaffold(
      backgroundColor: AppColors.cream,
      body: RefreshIndicator(
        color: AppColors.gold,
        onRefresh: _load,
        child: ListView(
          padding: const EdgeInsets.fromLTRB(16, 8, 16, 32),
          children: [
            FcBackLink(
              label: 'Season',
              fallbackRoute: plan == null ? '/cycles' : '/cycles/${plan.cycleId}',
            ),
            if (plan == null && _errorMessage == null)
              const FcLoading(message: 'Loading plan…')
            else if (plan == null)
              FcStateView.error(
                message: _errorMessage!,
                onAction: () {
                  setState(() => _errorMessage = null);
                  _load();
                },
              )
            else
              ..._buildContent(plan),
          ],
        ),
      ),
    );
  }

  List<Widget> _buildContent(CultivationPlan plan) {
    final stepsByStage = <String, List<PlanStep>>{};
    for (final step in plan.steps) {
      stepsByStage.putIfAbsent(step.stageLabel, () => []).add(step);
    }
    final isReviewed = plan.status == PlanStatus.approved ||
        plan.status == PlanStatus.rejected ||
        plan.status == PlanStatus.revisionRequested;

    return [
      FcPageHeader(
        eyebrow: 'AI cultivation plan',
        title: 'Your season plan',
        subtitle: 'Requested ${formatDate(plan.createdAt)}',
      ),
      const SizedBox(height: 14),
      _StatusBanner(plan: plan),
      if (plan.status == PlanStatus.draft) ...[
        const SizedBox(height: 12),
        if (_pollMessage != null)
          FcBanner.notice(message: '$_pollMessage Pull down to check again.')
        else
          const _GeneratingRow(),
      ],
      if (plan.officerComment != null && plan.officerComment!.isNotEmpty) ...[
        const SizedBox(height: 12),
        FcCard(
          color: const Color(0x1A22392A),
          borderColor: const Color(0x3322392A),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                children: [
                  Icon(isReviewed ? Icons.person_outline : Icons.info_outline,
                      size: 16, color: AppColors.forest),
                  const SizedBox(width: 6),
                  Text(
                    isReviewed ? 'Officer\'s comment' : 'Why this happened',
                    style: const TextStyle(
                        fontSize: 13, fontWeight: FontWeight.bold, color: AppColors.forest),
                  ),
                  const Spacer(),
                  if (plan.reviewedAt != null)
                    Text(formatDate(plan.reviewedAt),
                        style: const TextStyle(fontSize: 11, color: AppColors.inkSoft)),
                ],
              ),
              const SizedBox(height: 6),
              Text(plan.officerComment!,
                  style: const TextStyle(fontSize: 14, color: AppColors.ink, height: 1.4)),
            ],
          ),
        ),
      ],
      if (plan.status == PlanStatus.validationFailed && plan.validationErrors.isNotEmpty) ...[
        const FcSectionTitle('What the checks found'),
        FcCard(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              for (final error in plan.validationErrors)
                Padding(
                  padding: const EdgeInsets.symmetric(vertical: 4),
                  child: Row(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      const Padding(
                        padding: EdgeInsets.only(top: 2),
                        child: Icon(Icons.close_rounded, size: 14, color: AppColors.errorText),
                      ),
                      const SizedBox(width: 8),
                      Expanded(
                        child: Text(error,
                            style: const TextStyle(fontSize: 13, color: AppColors.ink)),
                      ),
                    ],
                  ),
                ),
            ],
          ),
        ),
      ],
      if (plan.status.invitesNewRequest) ...[
        const SizedBox(height: 14),
        ElevatedButton.icon(
          onPressed: _requestAgain,
          icon: const Icon(Icons.auto_awesome),
          label: const Text('Request a new plan'),
        ),
      ],
      const FcSectionTitle('Your goal'),
      Text('"${plan.objective}"',
          style: const TextStyle(
              fontSize: 14, fontStyle: FontStyle.italic, color: AppColors.inkSoft, height: 1.4)),
      if (plan.summary != null && plan.summary!.isNotEmpty) ...[
        const FcSectionTitle('Summary'),
        FcCard(
          child: Text(plan.summary!,
              style: const TextStyle(fontSize: 14, color: AppColors.ink, height: 1.45)),
        ),
      ],
      if (plan.steps.isNotEmpty) ...[
        const FcSectionTitle('Step by step'),
        for (final entry in stepsByStage.entries) ...[
          Padding(
            padding: const EdgeInsets.only(top: 6, bottom: 8),
            child: Text(
              entry.key.toUpperCase(),
              style: const TextStyle(
                fontSize: 11,
                fontWeight: FontWeight.bold,
                letterSpacing: 1.1,
                color: AppColors.shoot,
              ),
            ),
          ),
          for (final step in entry.value)
            Padding(
              padding: const EdgeInsets.only(bottom: 10),
              child: _StepCard(step: step),
            ),
        ],
      ],
      if (plan.assumptions.isNotEmpty) ...[
        const FcSectionTitle('Assumptions'),
        for (final assumption in plan.assumptions)
          Padding(
            padding: const EdgeInsets.only(bottom: 6),
            child: Row(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const Text('•  ', style: TextStyle(color: AppColors.inkSoft)),
                Expanded(
                  child: Text(assumption,
                      style: const TextStyle(fontSize: 13, color: AppColors.inkSoft)),
                ),
              ],
            ),
          ),
      ],
      if (plan.status != PlanStatus.approved && plan.steps.isNotEmpty) ...[
        const SizedBox(height: 16),
        const FcBanner.notice(
          message: 'Wait for your officer\'s approval before acting on this plan.',
        ),
      ],
    ];
  }
}

/// Shown under a Draft while the poller is waiting for the plan.
class _GeneratingRow extends StatelessWidget {
  const _GeneratingRow();

  @override
  Widget build(BuildContext context) {
    return const Row(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Padding(
          padding: EdgeInsets.only(top: 2),
          child: SizedBox(
            width: 16,
            height: 16,
            child: CircularProgressIndicator(color: AppColors.gold, strokeWidth: 2),
          ),
        ),
        SizedBox(width: 10),
        Expanded(
          child: Text(
            'Generating plan… Plans are prepared one at a time, so this one may be '
            'waiting behind others. This page updates by itself.',
            style: TextStyle(fontSize: 13, color: AppColors.inkSoft, height: 1.4),
          ),
        ),
      ],
    );
  }
}

class _StatusBanner extends StatelessWidget {
  final CultivationPlan plan;
  const _StatusBanner({required this.plan});

  @override
  Widget build(BuildContext context) {
    final (IconData icon, Color bg, Color fg) = switch (plan.status) {
      PlanStatus.approved => (Icons.verified_outlined, AppColors.successBg, AppColors.successText),
      PlanStatus.pendingOfficerApproval => (
          Icons.hourglass_top_rounded,
          const Color(0x26E1A63B),
          const Color(0xFF9C6C19)
        ),
      PlanStatus.draft => (Icons.hourglass_empty, AppColors.creamDeep, AppColors.inkSoft),
      _ => (Icons.error_outline, AppColors.errorBg, AppColors.errorText),
    };

    return Container(
      padding: const EdgeInsets.all(14),
      decoration: BoxDecoration(color: bg, borderRadius: BorderRadius.circular(12)),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Icon(icon, color: fg),
          const SizedBox(width: 12),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                PlanStatusBadge(plan.status),
                const SizedBox(height: 6),
                Text(plan.status.description,
                    style: TextStyle(fontSize: 13, color: fg, height: 1.4)),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

class _StepCard extends StatelessWidget {
  final PlanStep step;
  const _StepCard({required this.step});

  @override
  Widget build(BuildContext context) {
    return FcCard(
      padding: const EdgeInsets.all(14),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Container(
            padding: const EdgeInsets.all(8),
            decoration: BoxDecoration(
              color: AppColors.shootLight.withAlpha(90),
              borderRadius: BorderRadius.circular(10),
            ),
            child: Icon(step.category.icon, size: 18, color: AppColors.forest),
          ),
          const SizedBox(width: 12),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  children: [
                    Text(step.category.label,
                        style: const TextStyle(
                            fontSize: 11, fontWeight: FontWeight.w600, color: AppColors.shoot)),
                    const Spacer(),
                    if (step.windowLabel.isNotEmpty)
                      Text(step.windowLabel,
                          style: const TextStyle(
                              fontSize: 12,
                              fontWeight: FontWeight.w600,
                              color: AppColors.goldDeep)),
                  ],
                ),
                const SizedBox(height: 4),
                Text(step.task,
                    style: const TextStyle(
                        fontSize: 14,
                        fontWeight: FontWeight.w600,
                        color: AppColors.ink,
                        height: 1.35)),
                if (step.rationale.isNotEmpty) ...[
                  const SizedBox(height: 4),
                  Text(step.rationale,
                      style: const TextStyle(
                          fontSize: 12, color: AppColors.inkSoft, height: 1.35)),
                ],
              ],
            ),
          ),
        ],
      ),
    );
  }
}
