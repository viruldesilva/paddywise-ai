import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

import '../../../theme/app_theme.dart';
import '../models/cycle_models.dart';
import '../models/field_models.dart';
import '../services/field_cultivation_service.dart';
import '../widgets/fc_common.dart';
import '../widgets/status_badges.dart';

/// One field: its details, the season running on it, and past seasons.
class FieldDetailScreen extends StatefulWidget {
  final int fieldId;

  const FieldDetailScreen({super.key, required this.fieldId});

  @override
  State<FieldDetailScreen> createState() => _FieldDetailScreenState();
}

class _FieldDetailScreenState extends State<FieldDetailScreen> {
  Field? _field;
  List<CultivationCycle> _cycles = [];
  String? _errorMessage;
  bool _isDeleting = false;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    try {
      final results = await Future.wait([
        FieldCultivationService.getField(widget.fieldId),
        FieldCultivationService.getMyCycles(fieldId: widget.fieldId),
      ]);
      if (!mounted) return;
      setState(() {
        _field = results[0] as Field;
        _cycles = (results[1] as List<CultivationCycle>)
          ..sort((a, b) => b.sowingDate.compareTo(a.sowingDate));
        _errorMessage = null;
      });
    } catch (e) {
      if (!mounted) return;
      setState(() => _errorMessage = e.toString());
    }
  }

  Future<void> _edit() async {
    final saved = await context.push<Field>('/fields/${widget.fieldId}/edit', extra: _field);
    if (saved != null && mounted) {
      setState(() => _field = saved);
      showFcSnack(context, 'Field updated.');
    }
  }

  Future<void> _delete() async {
    final field = _field!;
    final confirmed = await confirmFc(
      context,
      title: 'Remove ${field.name}?',
      message: 'The field will no longer appear in your list. Its past seasons are kept '
          'for your officer\'s records.',
      confirmLabel: 'Remove',
    );
    if (!confirmed || !mounted) return;

    setState(() => _isDeleting = true);
    try {
      await FieldCultivationService.deleteField(field.id);
      if (!mounted) return;
      showFcSnack(context, '${field.name} removed.');
      context.canPop() ? context.pop() : context.go('/fields');
    } catch (e) {
      if (!mounted) return;
      setState(() => _isDeleting = false);
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(e.toString())));
    }
  }

  Future<void> _startCycle() async {
    final cycle = await context.push<CultivationCycle>(
      '/fields/${widget.fieldId}/start-cycle',
      extra: _field,
    );
    if (cycle != null && mounted) {
      showFcSnack(context, '${cycle.title} season started.');
      await context.push('/cycles/${cycle.id}');
      if (mounted) _load();
    }
  }

  Future<void> _openCycle(CultivationCycle cycle) async {
    await context.push('/cycles/${cycle.id}');
    if (mounted) _load();
  }

  @override
  Widget build(BuildContext context) {
    final field = _field;
    final openCycle = _cycles.where((c) => c.status.isOpen).firstOrNull;
    final pastCycles = _cycles.where((c) => !c.status.isOpen).toList();

    return Scaffold(
      backgroundColor: AppColors.cream,
      body: RefreshIndicator(
        color: AppColors.gold,
        onRefresh: _load,
        child: ListView(
          padding: const EdgeInsets.fromLTRB(16, 8, 16, 32),
          children: [
            const FcBackLink(label: 'My fields', fallbackRoute: '/fields'),
            if (field == null && _errorMessage == null)
              const FcLoading(message: 'Loading field…')
            else if (field == null)
              FcStateView.error(
                message: _errorMessage!,
                onAction: () {
                  setState(() => _errorMessage = null);
                  _load();
                },
              )
            else ...[
              FcPageHeader(
                eyebrow: 'Field',
                title: field.name,
                subtitle: field.divisionName,
                trailing: PopupMenuButton<String>(
                  icon: const Icon(Icons.more_vert, color: AppColors.inkSoft),
                  enabled: !_isDeleting,
                  onSelected: (value) => value == 'edit' ? _edit() : _delete(),
                  itemBuilder: (context) => const [
                    PopupMenuItem(value: 'edit', child: Text('Edit field')),
                    PopupMenuItem(value: 'delete', child: Text('Remove field')),
                  ],
                ),
              ),
              const SizedBox(height: 14),
              FcCard(
                child: Column(
                  children: [
                    FcInfoRow(
                        icon: Icons.straighten, label: 'Area', value: field.areaLabel),
                    FcInfoRow(
                        icon: Icons.layers_outlined,
                        label: 'Soil',
                        value: field.soilType.isEmpty ? 'Not recorded' : field.soilType),
                    FcInfoRow(
                        icon: Icons.water_drop_outlined,
                        label: 'Water source',
                        value: field.irrigationType.isEmpty
                            ? 'Not recorded'
                            : field.irrigationType),
                    FcInfoRow(
                      icon: Icons.my_location_outlined,
                      label: 'Location',
                      value: field.latitude == null || field.longitude == null
                          ? 'Not recorded'
                          : '${field.latitude!.toStringAsFixed(4)}, '
                              '${field.longitude!.toStringAsFixed(4)}',
                    ),
                  ],
                ),
              ),
              const FcSectionTitle('This season'),
              if (_errorMessage != null) ...[
                FcBanner(message: _errorMessage!),
                const SizedBox(height: 10),
              ],
              if (openCycle != null)
                _CycleTile(cycle: openCycle, onTap: () => _openCycle(openCycle), highlight: true)
              else
                FcCard(
                  color: AppColors.creamDeep.withAlpha(120),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      const Text(
                        'No season running on this field',
                        style: TextStyle(
                          fontSize: 15,
                          fontWeight: FontWeight.bold,
                          color: AppColors.ink,
                          fontFamily: 'serif',
                        ),
                      ),
                      const SizedBox(height: 4),
                      const Text(
                        'Start a cultivation cycle to get a stage-by-stage timeline and an '
                        'AI cultivation plan reviewed by your officer.',
                        style: TextStyle(fontSize: 13, color: AppColors.inkSoft, height: 1.4),
                      ),
                      const SizedBox(height: 12),
                      ElevatedButton.icon(
                        onPressed: _startCycle,
                        icon: const Icon(Icons.play_arrow_rounded),
                        label: const Text('Start cultivation'),
                      ),
                    ],
                  ),
                ),
              if (pastCycles.isNotEmpty) ...[
                const FcSectionTitle('Past seasons'),
                for (final cycle in pastCycles)
                  Padding(
                    padding: const EdgeInsets.only(bottom: 10),
                    child: _CycleTile(cycle: cycle, onTap: () => _openCycle(cycle)),
                  ),
              ],
            ],
          ],
        ),
      ),
    );
  }
}

class _CycleTile extends StatelessWidget {
  final CultivationCycle cycle;
  final VoidCallback onTap;
  final bool highlight;

  const _CycleTile({required this.cycle, required this.onTap, this.highlight = false});

  @override
  Widget build(BuildContext context) {
    return FcCard(
      onTap: onTap,
      borderColor: highlight ? AppColors.shoot : AppColors.line,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Expanded(
                child: Text(
                  '${cycle.title} · ${cycle.varietyName}',
                  style: const TextStyle(
                    fontSize: 15,
                    fontWeight: FontWeight.bold,
                    color: AppColors.ink,
                    fontFamily: 'serif',
                  ),
                ),
              ),
              CycleStatusBadge(cycle.status),
            ],
          ),
          const SizedBox(height: 6),
          Text(
            'Sown ${formatDate(cycle.sowingDate)} · '
            '${cycle.status == CycleStatus.harvested ? 'harvested ${formatDate(cycle.actualHarvestDate)}' : 'harvest ~${formatDate(cycle.expectedHarvestDate)}'}',
            style: const TextStyle(fontSize: 12, color: AppColors.inkSoft),
          ),
          if (highlight) ...[
            const SizedBox(height: 10),
            ClipRRect(
              borderRadius: BorderRadius.circular(4),
              child: LinearProgressIndicator(
                value: cycle.progress,
                minHeight: 6,
                backgroundColor: AppColors.creamDeep,
                color: AppColors.shoot,
              ),
            ),
            const SizedBox(height: 6),
            Text(
              'Day ${cycle.dayOfSeason} of ${cycle.durationDays} · ${cycle.currentStage.label}',
              style: const TextStyle(fontSize: 12, color: AppColors.forest),
            ),
          ],
        ],
      ),
    );
  }
}
