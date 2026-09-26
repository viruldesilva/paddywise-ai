import 'package:flutter/material.dart';
import '../../../theme/app_theme.dart';
import '../models/crop_activity_models.dart';

class ActivityTypeSelector extends StatelessWidget {
  final ActivityType selectedType;
  final ValueChanged<ActivityType> onTypeChanged;

  const ActivityTypeSelector({
    super.key,
    required this.selectedType,
    required this.onTypeChanged,
  });

  IconData _getIcon(ActivityType type) {
    switch (type) {
      case ActivityType.fertilizer:
        return Icons.eco_rounded;
      case ActivityType.irrigation:
        return Icons.water_drop_rounded;
      case ActivityType.pesticide:
        return Icons.shield_outlined;
      case ActivityType.other:
        return Icons.agriculture_rounded;
    }
  }

  @override
  Widget build(BuildContext context) {
    return Container(
      decoration: BoxDecoration(
        color: AppColors.creamDeep.withAlpha(120),
        borderRadius: BorderRadius.circular(12),
        border: Border.all(color: AppColors.line),
      ),
      padding: const EdgeInsets.all(4),
      child: Row(
        children: ActivityType.values.map((type) {
          final isSelected = type == selectedType;
          return Expanded(
            child: GestureDetector(
              onTap: () => onTypeChanged(type),
              child: AnimatedContainer(
                duration: const Duration(milliseconds: 200),
                padding: const EdgeInsets.symmetric(vertical: 10, horizontal: 4),
                decoration: BoxDecoration(
                  color: isSelected ? AppColors.forest : Colors.transparent,
                  borderRadius: BorderRadius.circular(8),
                  boxShadow: isSelected
                      ? [
                          BoxShadow(
                            color: AppColors.forest.withAlpha(40),
                            blurRadius: 4,
                            offset: const Offset(0, 2),
                          ),
                        ]
                      : null,
                ),
                child: Column(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    Icon(
                      _getIcon(type),
                      size: 20,
                      color: isSelected ? AppColors.cream : AppColors.inkSoft,
                    ),
                    const SizedBox(height: 4),
                    Text(
                      type.label,
                      style: TextStyle(
                        fontSize: 12,
                        fontWeight: isSelected ? FontWeight.bold : FontWeight.w500,
                        color: isSelected ? AppColors.cream : AppColors.ink,
                      ),
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                    ),
                  ],
                ),
              ),
            ),
          );
        }).toList(),
      ),
    );
  }
}
