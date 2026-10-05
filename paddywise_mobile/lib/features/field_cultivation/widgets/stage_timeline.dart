import 'package:flutter/material.dart';

import '../../../theme/app_theme.dart';
import '../models/cycle_models.dart';

/// The six growth stages with their planned windows, marking which the farmer
/// has reported, which is current, and where the calendar says the crop
/// should be today.
class StageTimeline extends StatelessWidget {
  final CultivationCycle cycle;

  const StageTimeline({super.key, required this.cycle});

  @override
  Widget build(BuildContext context) {
    final windows = {for (final w in cycle.timeline) w.stage: w};
    final logsByStage = <GrowthStage, List<StageLog>>{};
    for (final log in cycle.stageLogs) {
      logsByStage.putIfAbsent(log.stage, () => []).add(log);
    }
    final isOpen = cycle.status.isOpen;

    return Column(
      children: [
        for (final stage in GrowthStage.values)
          _StageRow(
            stage: stage,
            window: windows[stage],
            logs: logsByStage[stage] ?? const [],
            isReached: stage.index < cycle.currentStage.index ||
                (stage == cycle.currentStage && logsByStage.containsKey(stage)) ||
                cycle.status == CycleStatus.harvested,
            isCurrent: isOpen && stage == cycle.currentStage,
            isExpectedToday: isOpen && stage == cycle.expectedStageToday,
            isLast: stage == GrowthStage.values.last,
          ),
      ],
    );
  }
}

class _StageRow extends StatelessWidget {
  final GrowthStage stage;
  final StageWindow? window;
  final List<StageLog> logs;
  final bool isReached;
  final bool isCurrent;
  final bool isExpectedToday;
  final bool isLast;

  const _StageRow({
    required this.stage,
    required this.window,
    required this.logs,
    required this.isReached,
    required this.isCurrent,
    required this.isExpectedToday,
    required this.isLast,
  });

  @override
  Widget build(BuildContext context) {
    final Color dotColor = isCurrent
        ? AppColors.gold
        : isReached
            ? AppColors.shoot
            : AppColors.creamDeep;

    return IntrinsicHeight(
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          SizedBox(
            width: 28,
            child: Column(
              children: [
                const SizedBox(height: 2),
                Container(
                  width: 20,
                  height: 20,
                  decoration: BoxDecoration(
                    color: dotColor,
                    shape: BoxShape.circle,
                    border: Border.all(
                      color: isReached || isCurrent ? dotColor : AppColors.line,
                      width: 2,
                    ),
                  ),
                  child: isReached && !isCurrent
                      ? const Icon(Icons.check, size: 12, color: Colors.white)
                      : null,
                ),
                if (!isLast)
                  Expanded(
                    child: Container(
                      width: 2,
                      margin: const EdgeInsets.symmetric(vertical: 2),
                      color: isReached ? AppColors.shootLight : AppColors.line,
                    ),
                  ),
              ],
            ),
          ),
          const SizedBox(width: 10),
          Expanded(
            child: Padding(
              padding: EdgeInsets.only(bottom: isLast ? 0 : 16),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Wrap(
                    spacing: 6,
                    runSpacing: 4,
                    crossAxisAlignment: WrapCrossAlignment.center,
                    children: [
                      Text(
                        stage.label,
                        style: TextStyle(
                          fontSize: 15,
                          fontWeight: isCurrent ? FontWeight.bold : FontWeight.w600,
                          color: isReached || isCurrent ? AppColors.ink : AppColors.inkSoft,
                        ),
                      ),
                      if (isCurrent) const _Tag('Reported now', AppColors.gold),
                      if (isExpectedToday && !isCurrent)
                        const _Tag('Expected today', AppColors.clay),
                    ],
                  ),
                  if (window != null)
                    Padding(
                      padding: const EdgeInsets.only(top: 2),
                      child: Text(
                        '${formatShortDate(window!.start)} – ${formatShortDate(window!.end)}',
                        style: const TextStyle(fontSize: 12, color: AppColors.inkSoft),
                      ),
                    ),
                  for (final log in logs)
                    Container(
                      margin: const EdgeInsets.only(top: 6),
                      padding: const EdgeInsets.all(8),
                      decoration: BoxDecoration(
                        color: AppColors.cream,
                        borderRadius: BorderRadius.circular(8),
                      ),
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(
                            'Seen ${formatDate(log.observedOn)}'
                            '${log.loggedByUserName.isEmpty ? '' : ' · ${log.loggedByUserName}'}',
                            style: const TextStyle(
                              fontSize: 12,
                              fontWeight: FontWeight.w600,
                              color: AppColors.forest,
                            ),
                          ),
                          if (log.notes != null && log.notes!.isNotEmpty)
                            Padding(
                              padding: const EdgeInsets.only(top: 2),
                              child: Text(log.notes!,
                                  style: const TextStyle(
                                      fontSize: 12, color: AppColors.inkSoft)),
                            ),
                        ],
                      ),
                    ),
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }
}

class _Tag extends StatelessWidget {
  final String label;
  final Color color;
  const _Tag(this.label, this.color);

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 7, vertical: 2),
      decoration: BoxDecoration(
        color: color.withAlpha(40),
        borderRadius: BorderRadius.circular(20),
      ),
      child: Text(
        label,
        style: TextStyle(fontSize: 10, fontWeight: FontWeight.bold, color: color),
      ),
    );
  }
}
