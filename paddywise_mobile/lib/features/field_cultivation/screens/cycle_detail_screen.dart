import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

import '../../../theme/app_theme.dart';
import '../models/cycle_models.dart';
import '../models/plan_models.dart';
import '../services/field_cultivation_service.dart';
import '../widgets/fc_common.dart';
import '../widgets/stage_timeline.dart';
import '../widgets/status_badges.dart';

/// One season on one field: progress, the growth-stage timeline, stage
/// logging, and the AI cultivation plan with its officer review.
class CycleDetailScreen extends StatefulWidget {
  final int cycleId;

  const CycleDetailScreen({super.key, required this.cycleId});

  @override
  State<CycleDetailScreen> createState() => _CycleDetailScreenState();
}

class _CycleDetailScreenState extends State<CycleDetailScreen> {
  CultivationCycle? _cycle;
  List<CultivationPlan>? _plans;
  String? _errorMessage;
  String? _plansError;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    // The plans are read separately so a failure there costs only the plan card.
    final plansFuture = FieldCultivationService.getPlansForCycle(widget.cycleId)
        .then<Object>((plans) => plans, onError: (Object e) => e);
    try {
      final cycle = await FieldCultivationService.getCycle(widget.cycleId);
      final plans = await plansFuture;
      if (!mounted) return;
      setState(() {
        _cycle = cycle;
        _errorMessage = null;
        if (plans is List<CultivationPlan>) {
          _plans = plans;
          _plansError = null;
        } else {
          _plansError = plans.toString();
        }
      });
    } catch (e) {
      if (!mounted) return;
      setState(() => _errorMessage = e.toString());
    }
  }

  Future<void> _logStage() async {
    final cycle = _cycle!;
    final updated = await showModalBottomSheet<CultivationCycle>(
      context: context,
      isScrollControlled: true,
      backgroundColor: Colors.transparent,
      builder: (context) => _LogStageSheet(cycle: cycle),
    );
    if (updated != null && mounted) {
      setState(() => _cycle = updated);
      showFcSnack(
        context,
        updated.status == CycleStatus.harvested
            ? 'Harvest recorded. This season is now closed.'
            : '${updated.currentStage.label} logged.',
      );
    }
  }

  Future<void> _abandon() async {
    final confirmed = await confirmFc(
      context,
      title: 'Abandon this season?',
      message: 'Use this if the crop was lost or you will not continue. The season is closed '
          'and cannot be reopened, but you can start a new one on this field.',
      confirmLabel: 'Abandon',
    );
    if (!confirmed || !mounted) return;
    try {
      final updated = await FieldCultivationService.abandonCycle(widget.cycleId);
      if (!mounted) return;
      setState(() => _cycle = updated);
      showFcSnack(context, 'Season abandoned.');
    } catch (e) {
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(e.toString())));
    }
  }

  Future<void> _requestPlan() async {
    final plan =
        await context.push<CultivationPlan>('/cycles/${widget.cycleId}/plans/new', extra: _cycle);
    if (plan != null && mounted) {
      await context.push('/plans/${plan.id}');
      if (mounted) _load();
    }
  }

  Future<void> _openPlan(CultivationPlan plan) async {
    await context.push('/plans/${plan.id}');
    if (mounted) _load();
  }

  @override
  Widget build(BuildContext context) {
    final cycle = _cycle;

    return Scaffold(
      backgroundColor: AppColors.cream,
      floatingActionButton: cycle != null && cycle.status.isOpen
          ? FloatingActionButton.extended(
              onPressed: _logStage,
              backgroundColor: AppColors.gold,
              foregroundColor: AppColors.forestDeep,
              icon: const Icon(Icons.edit_calendar_outlined),
              label: const Text('Log growth stage'),
            )
          : null,
      body: RefreshIndicator(
        color: AppColors.gold,
        onRefresh: _load,
        child: ListView(
          padding: const EdgeInsets.fromLTRB(16, 8, 16, 96),
          children: [
            FcBackLink(
              label: cycle?.fieldName.isNotEmpty == true ? cycle!.fieldName : 'Back',
              fallbackRoute: cycle == null ? '/cycles' : '/fields/${cycle.fieldId}',
            ),
            if (cycle == null && _errorMessage == null)
              const FcLoading(message: 'Loading season…')
            else if (cycle == null)
              FcStateView.error(
                message: _errorMessage!,
                onAction: () {
                  setState(() => _errorMessage = null);
                  _load();
                },
              )
            else
              ..._buildContent(cycle),
          ],
        ),
      ),
    );
  }

  List<Widget> _buildContent(CultivationCycle cycle) {
    return [
      FcPageHeader(
        eyebrow: 'Cultivation season',
        title: '${cycle.title} · ${cycle.varietyName}',
        subtitle: '${cycle.fieldName} · ${cycle.method.label}',
        trailing: cycle.status.isOpen
            ? PopupMenuButton<String>(
                icon: const Icon(Icons.more_vert, color: AppColors.inkSoft),
                onSelected: (_) => _abandon(),
                itemBuilder: (context) => const [
                  PopupMenuItem(value: 'abandon', child: Text('Abandon season')),
                ],
              )
            : null,
      ),
      const SizedBox(height: 14),
      _ProgressCard(cycle: cycle),
      if (_errorMessage != null) ...[
        const SizedBox(height: 10),
        FcBanner(message: _errorMessage!),
      ],
      if (cycle.isBehindTimeline) ...[
        const SizedBox(height: 10),
        FcBanner.notice(
          icon: Icons.schedule,
          message: 'By the calendar your crop should be at ${cycle.expectedStageToday.label}, '
              'but the last stage you logged is ${cycle.currentStage.label}. '
              'Log a new stage if the crop has moved on.',
        ),
      ],
      FcSectionTitle('AI cultivation plan'),
      _buildPlanSection(cycle),
      const FcSectionTitle('Growth stages'),
      FcCard(child: StageTimeline(cycle: cycle)),
      if (cycle.notes != null && cycle.notes!.isNotEmpty) ...[
        const FcSectionTitle('Notes'),
        FcCard(
          child: Text(cycle.notes!,
              style: const TextStyle(fontSize: 13, color: AppColors.inkSoft, height: 1.4)),
        ),
      ],
    ];
  }

  Widget _buildPlanSection(CultivationCycle cycle) {
    if (_plansError != null && _plans == null) {
      return FcBanner(message: _plansError!);
    }
    final plans = _plans ?? const <CultivationPlan>[];
    final latest = plans.firstOrNull;
    final earlier = plans.skip(1).toList();
    final canRequest = cycle.status.isOpen && !(latest?.status.blocksNewRequest ?? false);

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        if (latest == null)
          FcCard(
            color: AppColors.creamDeep.withAlpha(120),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const Row(
                  children: [
                    Icon(Icons.auto_awesome, size: 18, color: AppColors.goldDeep),
                    SizedBox(width: 8),
                    Text('No plan yet',
                        style: TextStyle(
                            fontSize: 15,
                            fontWeight: FontWeight.bold,
                            color: AppColors.ink,
                            fontFamily: 'serif')),
                  ],
                ),
                const SizedBox(height: 6),
                Text(
                  cycle.status.isOpen
                      ? 'Tell the planning assistant what you want from this season. It '
                          'drafts a dated, stage-by-stage plan and your agricultural officer '
                          'reviews it before you follow it.'
                      : 'No plan was requested for this season.',
                  style: const TextStyle(fontSize: 13, color: AppColors.inkSoft, height: 1.4),
                ),
              ],
            ),
          )
        else
          FcCard(
            onTap: () => _openPlan(latest),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  children: [
                    Expanded(
                      child: Text(
                        'Requested ${formatDate(latest.createdAt)}',
                        style: const TextStyle(fontSize: 12, color: AppColors.inkSoft),
                      ),
                    ),
                    PlanStatusBadge(latest.status),
                  ],
                ),
                const SizedBox(height: 8),
                Text(
                  latest.summary?.isNotEmpty == true ? latest.summary! : latest.objective,
                  maxLines: 3,
                  overflow: TextOverflow.ellipsis,
                  style: const TextStyle(fontSize: 14, color: AppColors.ink, height: 1.4),
                ),
                const SizedBox(height: 8),
                Text(latest.status.description,
                    style: const TextStyle(fontSize: 12, color: AppColors.forest)),
                const SizedBox(height: 8),
                const Text('View plan →',
                    style: TextStyle(
                        fontSize: 13,
                        fontWeight: FontWeight.w600,
                        color: AppColors.goldDeep)),
              ],
            ),
          ),
        if (canRequest) ...[
          const SizedBox(height: 12),
          ElevatedButton.icon(
            onPressed: _requestPlan,
            icon: const Icon(Icons.auto_awesome),
            label: Text(latest == null ? 'Get an AI cultivation plan' : 'Request a new plan'),
          ),
        ],
        if (earlier.isNotEmpty) ...[
          const SizedBox(height: 12),
          Theme(
            data: Theme.of(context).copyWith(dividerColor: Colors.transparent),
            child: ExpansionTile(
              tilePadding: EdgeInsets.zero,
              title: Text('Earlier plans (${earlier.length})',
                  style: const TextStyle(fontSize: 14, color: AppColors.inkSoft)),
              children: [
                for (final plan in earlier)
                  ListTile(
                    contentPadding: EdgeInsets.zero,
                    onTap: () => _openPlan(plan),
                    title: Text(plan.objective,
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                        style: const TextStyle(fontSize: 13, color: AppColors.ink)),
                    subtitle: Text(formatDate(plan.createdAt),
                        style: const TextStyle(fontSize: 12)),
                    trailing: PlanStatusBadge(plan.status),
                  ),
              ],
            ),
          ),
        ],
      ],
    );
  }
}

class _ProgressCard extends StatelessWidget {
  final CultivationCycle cycle;
  const _ProgressCard({required this.cycle});

  @override
  Widget build(BuildContext context) {
    final harvested = cycle.status == CycleStatus.harvested;

    return FcCard(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              CycleStatusBadge(cycle.status),
              const Spacer(),
              if (cycle.status.isOpen)
                Text(
                  cycle.sowingDate.isAfter(today)
                      ? 'Sowing in ${cycle.sowingDate.difference(today).inDays} days'
                      : 'Day ${cycle.dayOfSeason} of ${cycle.durationDays}',
                  style: const TextStyle(
                      fontSize: 13, fontWeight: FontWeight.w600, color: AppColors.forest),
                ),
            ],
          ),
          if (cycle.status.isOpen) ...[
            const SizedBox(height: 10),
            ClipRRect(
              borderRadius: BorderRadius.circular(4),
              child: LinearProgressIndicator(
                value: cycle.progress,
                minHeight: 8,
                backgroundColor: AppColors.creamDeep,
                color: AppColors.shoot,
              ),
            ),
          ],
          const SizedBox(height: 8),
          FcInfoRow(
              icon: Icons.event_outlined, label: 'Sown', value: formatDate(cycle.sowingDate)),
          FcInfoRow(
            icon: Icons.agriculture_outlined,
            label: harvested ? 'Harvested' : 'Expected harvest',
            value: formatDate(harvested ? cycle.actualHarvestDate : cycle.expectedHarvestDate),
          ),
          FcInfoRow(
              icon: Icons.eco_outlined,
              label: 'Last logged stage',
              value: cycle.currentStage.label),
        ],
      ),
    );
  }
}

/// Bottom sheet to record that the crop reached a growth stage.
class _LogStageSheet extends StatefulWidget {
  final CultivationCycle cycle;
  const _LogStageSheet({required this.cycle});

  @override
  State<_LogStageSheet> createState() => _LogStageSheetState();
}

class _LogStageSheetState extends State<_LogStageSheet> {
  final _notesController = TextEditingController();
  late GrowthStage _stage;
  late DateTime _observedOn;
  bool _isSaving = false;
  String? _errorMessage;

  /// Stages can only move forward from the last one logged.
  List<GrowthStage> get _choices => GrowthStage.values
      .where((s) => s.index >= widget.cycle.currentStage.index)
      .toList();

  @override
  void initState() {
    super.initState();
    final hasLogs = widget.cycle.stageLogs.isNotEmpty;
    final next = widget.cycle.currentStage.index + (hasLogs ? 1 : 0);
    _stage = GrowthStage.values[next.clamp(0, GrowthStage.values.length - 1)];
    _observedOn = today;
  }

  @override
  void dispose() {
    _notesController.dispose();
    super.dispose();
  }

  Future<void> _pickDate() async {
    final sown = widget.cycle.sowingDate;
    final picked = await showDatePicker(
      context: context,
      initialDate: _observedOn,
      firstDate: sown.isAfter(today) ? today : sown,
      lastDate: today,
      helpText: 'Date observed',
    );
    if (picked != null) setState(() => _observedOn = picked);
  }

  Future<void> _save() async {
    if (_stage == GrowthStage.harvest) {
      final confirmed = await confirmFc(
        context,
        title: 'Record harvest?',
        message: 'Logging Harvest closes this season. You will not be able to log more stages.',
        confirmLabel: 'Record harvest',
      );
      if (!confirmed || !mounted) return;
    }

    setState(() {
      _isSaving = true;
      _errorMessage = null;
    });
    final notes = _notesController.text.trim();
    try {
      final updated = await FieldCultivationService.logStage(
        widget.cycle.id,
        stage: _stage,
        observedOn: _observedOn,
        notes: notes.isEmpty ? null : notes,
      );
      if (mounted) Navigator.of(context).pop(updated);
    } catch (e) {
      if (!mounted) return;
      setState(() {
        _errorMessage = e.toString();
        _isSaving = false;
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: EdgeInsets.only(bottom: MediaQuery.of(context).viewInsets.bottom),
      child: Container(
        decoration: const BoxDecoration(
          color: AppColors.cream,
          borderRadius: BorderRadius.vertical(top: Radius.circular(20)),
        ),
        padding: const EdgeInsets.fromLTRB(20, 12, 20, 20),
        child: SafeArea(
          top: false,
          child: SingleChildScrollView(
            child: Column(
              mainAxisSize: MainAxisSize.min,
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                Center(
                  child: Container(
                    width: 40,
                    height: 4,
                    decoration: BoxDecoration(
                        color: AppColors.line, borderRadius: BorderRadius.circular(2)),
                  ),
                ),
                const SizedBox(height: 14),
                const Text('Log a growth stage',
                    style: TextStyle(
                        fontSize: 18,
                        fontWeight: FontWeight.bold,
                        color: AppColors.ink,
                        fontFamily: 'serif')),
                const SizedBox(height: 4),
                Text(
                  'The calendar expects ${widget.cycle.expectedStageToday.label} around now.',
                  style: const TextStyle(fontSize: 13, color: AppColors.inkSoft),
                ),
                const FcFieldLabel('Stage reached'),
                Wrap(
                  spacing: 8,
                  runSpacing: 8,
                  children: [
                    for (final stage in _choices)
                      ChoiceChip(
                        label: Text(stage.label),
                        selected: _stage == stage,
                        onSelected: (_) => setState(() => _stage = stage),
                        selectedColor: AppColors.shootLight,
                        backgroundColor: Colors.white,
                        side: const BorderSide(color: AppColors.line),
                        showCheckmark: false,
                      ),
                  ],
                ),
                const FcFieldLabel('Date observed'),
                FcCard(
                  onTap: _pickDate,
                  padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
                  child: Row(
                    children: [
                      const Icon(Icons.event_outlined, size: 18, color: AppColors.shoot),
                      const SizedBox(width: 10),
                      Expanded(child: Text(formatDate(_observedOn))),
                      const Text('Change',
                          style: TextStyle(
                              fontSize: 13,
                              fontWeight: FontWeight.w600,
                              color: AppColors.goldDeep)),
                    ],
                  ),
                ),
                const FcFieldLabel('Notes (optional)'),
                TextField(
                  controller: _notesController,
                  maxLines: 2,
                  maxLength: CycleRules.notesMaxLength,
                  textCapitalization: TextCapitalization.sentences,
                  decoration: const InputDecoration(
                    hintText: 'e.g. Even tillering, some yellowing on the lower leaves',
                  ),
                ),
                if (_errorMessage != null) ...[
                  FcBanner(message: _errorMessage!),
                  const SizedBox(height: 12),
                ],
                ElevatedButton(
                  onPressed: _isSaving ? null : _save,
                  child: _isSaving
                      ? const SizedBox(
                          height: 20,
                          width: 20,
                          child: CircularProgressIndicator(
                              strokeWidth: 2, color: AppColors.forestDeep),
                        )
                      : Text('Log ${_stage.label}'),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}
