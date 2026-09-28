import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

import '../../../theme/app_theme.dart';
import '../models/cycle_models.dart';
import '../services/field_cultivation_service.dart';
import '../widgets/fc_common.dart';
import '../widgets/status_badges.dart';

/// Every cultivation cycle across the farmer's fields: running seasons first,
/// then past ones.
class MyCyclesScreen extends StatefulWidget {
  const MyCyclesScreen({super.key});

  @override
  State<MyCyclesScreen> createState() => _MyCyclesScreenState();
}

class _MyCyclesScreenState extends State<MyCyclesScreen> {
  List<CultivationCycle>? _cycles;
  String? _errorMessage;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    try {
      final cycles = await FieldCultivationService.getMyCycles();
      if (!mounted) return;
      setState(() {
        _cycles = cycles..sort((a, b) => b.sowingDate.compareTo(a.sowingDate));
        _errorMessage = null;
      });
    } catch (e) {
      if (!mounted) return;
      setState(() => _errorMessage = e.toString());
    }
  }

  Future<void> _open(CultivationCycle cycle) async {
    await context.push('/cycles/${cycle.id}');
    if (mounted) _load();
  }

  @override
  Widget build(BuildContext context) {
    final cycles = _cycles;
    final running = cycles?.where((c) => c.status.isOpen).toList() ?? [];
    final past = cycles?.where((c) => !c.status.isOpen).toList() ?? [];

    return Scaffold(
      backgroundColor: AppColors.cream,
      body: RefreshIndicator(
        color: AppColors.gold,
        onRefresh: _load,
        child: ListView(
          padding: const EdgeInsets.fromLTRB(16, 16, 16, 32),
          children: [
            const FcPageHeader(
              eyebrow: 'Field & Cultivation',
              title: 'My Cultivations',
              subtitle: 'Your seasons across every field — log growth stages and follow '
                  'your AI cultivation plan.',
            ),
            const SizedBox(height: 8),
            if (cycles == null && _errorMessage == null)
              const FcLoading(message: 'Loading your seasons…')
            else if (cycles == null)
              FcStateView.error(
                message: _errorMessage!,
                onAction: () {
                  setState(() => _errorMessage = null);
                  _load();
                },
              )
            else if (cycles.isEmpty)
              FcStateView(
                icon: Icons.eco_outlined,
                title: 'No seasons yet',
                message: 'Open one of your fields and start a cultivation cycle.',
                actionLabel: 'Go to my fields',
                onAction: () => context.go('/fields'),
              )
            else ...[
              if (running.isNotEmpty) ...[
                const FcSectionTitle('Running now'),
                for (final cycle in running)
                  Padding(
                    padding: const EdgeInsets.only(bottom: 12),
                    child: _RunningCycleCard(cycle: cycle, onTap: () => _open(cycle)),
                  ),
              ],
              if (past.isNotEmpty) ...[
                const FcSectionTitle('Past seasons'),
                for (final cycle in past)
                  Padding(
                    padding: const EdgeInsets.only(bottom: 10),
                    child: FcCard(
                      onTap: () => _open(cycle),
                      child: Row(
                        children: [
                          Expanded(
                            child: Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                Text(
                                  '${cycle.title} · ${cycle.fieldName}',
                                  style: const TextStyle(
                                    fontSize: 15,
                                    fontWeight: FontWeight.bold,
                                    color: AppColors.ink,
                                    fontFamily: 'serif',
                                  ),
                                ),
                                const SizedBox(height: 4),
                                Text(
                                  '${cycle.varietyName} · sown ${formatDate(cycle.sowingDate)}',
                                  style: const TextStyle(
                                      fontSize: 12, color: AppColors.inkSoft),
                                ),
                              ],
                            ),
                          ),
                          CycleStatusBadge(cycle.status),
                        ],
                      ),
                    ),
                  ),
              ],
            ],
          ],
        ),
      ),
    );
  }
}

class _RunningCycleCard extends StatelessWidget {
  final CultivationCycle cycle;
  final VoidCallback onTap;

  const _RunningCycleCard({required this.cycle, required this.onTap});

  @override
  Widget build(BuildContext context) {
    return FcCard(
      onTap: onTap,
      borderColor: AppColors.shoot,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Expanded(
                child: Text(
                  cycle.fieldName,
                  style: const TextStyle(
                    fontSize: 16,
                    fontWeight: FontWeight.bold,
                    color: AppColors.ink,
                    fontFamily: 'serif',
                  ),
                ),
              ),
              CycleStatusBadge(cycle.status),
            ],
          ),
          const SizedBox(height: 4),
          Text(
            '${cycle.title} · ${cycle.varietyName} · ${cycle.method.label}',
            style: const TextStyle(fontSize: 12, color: AppColors.inkSoft),
          ),
          const SizedBox(height: 12),
          ClipRRect(
            borderRadius: BorderRadius.circular(4),
            child: LinearProgressIndicator(
              value: cycle.progress,
              minHeight: 6,
              backgroundColor: AppColors.creamDeep,
              color: AppColors.shoot,
            ),
          ),
          const SizedBox(height: 8),
          Row(
            children: [
              Text(
                'Day ${cycle.dayOfSeason} / ${cycle.durationDays}',
                style: const TextStyle(
                    fontSize: 12, fontWeight: FontWeight.w600, color: AppColors.forest),
              ),
              const Spacer(),
              Text(
                cycle.currentStage.label,
                style: const TextStyle(fontSize: 12, color: AppColors.forest),
              ),
            ],
          ),
          if (cycle.isBehindTimeline) ...[
            const SizedBox(height: 8),
            Text(
              'Timeline expects ${cycle.expectedStageToday.label} — log a stage if the crop has moved on.',
              style: const TextStyle(fontSize: 12, color: AppColors.clay),
            ),
          ],
        ],
      ),
    );
  }
}
