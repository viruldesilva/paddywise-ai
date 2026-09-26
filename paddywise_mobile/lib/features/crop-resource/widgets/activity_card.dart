import 'package:flutter/material.dart';
import '../../../theme/app_theme.dart';
import '../models/crop_activity_models.dart';
import 'activity_detail_sheet.dart';

/// Interactive card widget presenting summary of a recorded crop activity.
class ActivityCard extends StatelessWidget {
  final CropActivityDto activity;
  final VoidCallback? onDelete;
  final VoidCallback? onTap;

  const ActivityCard({
    super.key,
    required this.activity,
    this.onDelete,
    this.onTap,
  });

  Color _getTypeColor(String type) {
    switch (type.toLowerCase()) {
      case 'fertilizer':
        return const Color(0xFF2E7D32); // Emerald leaf green
      case 'irrigation':
        return const Color(0xFF0288D1); // Water blue
      case 'pesticide':
        return const Color(0xFFD97706); // Warning amber
      default:
        return AppColors.forest;
    }
  }

  IconData _getTypeIcon(String type) {
    switch (type.toLowerCase()) {
      case 'fertilizer':
        return Icons.eco_rounded;
      case 'irrigation':
        return Icons.water_drop_rounded;
      case 'pesticide':
        return Icons.pest_control_rounded;
      default:
        return Icons.assignment_rounded;
    }
  }

  String _formatDate(String? raw) {
    if (raw == null || raw.isEmpty) return '—';
    try {
      final clean = raw.split('T').first;
      final parts = clean.split('-');
      if (parts.length == 3) {
        final year = int.parse(parts[0]);
        final month = int.parse(parts[1]);
        final day = int.parse(parts[2]);
        const months = [
          'Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun',
          'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'
        ];
        return '${months[month - 1]} $day, $year';
      }
    } catch (_) {}
    return raw;
  }

  String _buildSummary(Map<String, dynamic> data) {
    final type = activity.activityType.toLowerCase();
    if (type == 'fertilizer') {
      final parts = <String>[];
      if (data['type'] != null && (data['type'] as String).isNotEmpty) {
        parts.add(data['type'] as String);
      }
      if (data['quantity'] != null) {
        parts.add('${data['quantity']} kg/ha');
      }
      if (data['cropStage'] != null && (data['cropStage'] as String).isNotEmpty) {
        parts.add(data['cropStage'] as String);
      }
      return parts.isNotEmpty ? parts.join(' • ') : 'Fertilizer applied';
    } else if (type == 'irrigation') {
      final parts = <String>[];
      if (data['source'] != null && (data['source'] as String).isNotEmpty) {
        parts.add(data['source'] as String);
      }
      if (data['waterLevel'] != null) {
        parts.add('${data['waterLevel']} cm');
      }
      if (data['duration'] != null) {
        parts.add('${data['duration']} hrs');
      }
      return parts.isNotEmpty ? parts.join(' • ') : 'Irrigation flooded';
    } else if (type == 'pesticide') {
      final parts = <String>[];
      if (data['product'] != null && (data['product'] as String).isNotEmpty) {
        parts.add(data['product'] as String);
      }
      if (data['quantity'] != null) {
        parts.add('${data['quantity']} ml/g');
      }
      if (data['targetPest'] != null && (data['targetPest'] as String).isNotEmpty) {
        parts.add('Pest: ${data['targetPest']}');
      }
      return parts.isNotEmpty ? parts.join(' • ') : 'Pesticide sprayed';
    } else {
      final specific = data['specificActivity'] as String?;
      final notes = data['notes'] as String?;
      if (specific != null && specific.isNotEmpty) {
        return notes != null && notes.isNotEmpty
            ? '$specific: $notes'
            : specific;
      }
      return notes ?? 'Field management task';
    }
  }

  void _showDeleteConfirmation(BuildContext context) {
    showDialog(
      context: context,
      builder: (ctx) => AlertDialog(
        backgroundColor: AppColors.cream,
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
        title: Row(
          children: const [
            Icon(Icons.warning_amber_rounded, color: AppColors.errorText, size: 24),
            SizedBox(width: 8),
            Text(
              'Delete Activity',
              style: TextStyle(
                fontFamily: 'serif',
                fontWeight: FontWeight.bold,
                color: AppColors.ink,
                fontSize: 18,
              ),
            ),
          ],
        ),
        content: Text(
          'Are you sure you want to remove this ${activity.activityType} activity entry? This action cannot be undone.',
          style: const TextStyle(fontSize: 14, color: AppColors.inkSoft),
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(ctx).pop(),
            child: const Text('Cancel', style: TextStyle(color: AppColors.inkSoft)),
          ),
          ElevatedButton(
            onPressed: () {
              Navigator.of(ctx).pop();
              if (onDelete != null) onDelete!();
            },
            style: ElevatedButton.styleFrom(
              backgroundColor: AppColors.errorText,
              foregroundColor: Colors.white,
            ),
            child: const Text('Delete'),
          ),
        ],
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final typeColor = _getTypeColor(activity.activityType);
    final details = activity.parsedDetails;
    final summaryText = _buildSummary(details);

    return Container(
      margin: const EdgeInsets.only(bottom: 12),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(14),
        border: Border.all(color: AppColors.line),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withAlpha(8),
            blurRadius: 6,
            offset: const Offset(0, 2),
          ),
        ],
      ),
      child: Material(
        color: Colors.transparent,
        borderRadius: BorderRadius.circular(14),
        child: InkWell(
          borderRadius: BorderRadius.circular(14),
          onTap: onTap ??
              () => ActivityDetailSheet.show(
                    context,
                    activity: activity,
                    onDelete: onDelete != null
                        ? () => _showDeleteConfirmation(context)
                        : null,
                  ),
          child: Padding(
            padding: const EdgeInsets.all(16),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                // Top Row: Type Pill + Date + Delete Button
                Row(
                  children: [
                    Container(
                      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
                      decoration: BoxDecoration(
                        color: typeColor.withAlpha(22),
                        borderRadius: BorderRadius.circular(20),
                        border: Border.all(color: typeColor.withAlpha(60)),
                      ),
                      child: Row(
                        mainAxisSize: MainAxisSize.min,
                        children: [
                          Icon(_getTypeIcon(activity.activityType), size: 14, color: typeColor),
                          const SizedBox(width: 5),
                          Text(
                            activity.activityType,
                            style: TextStyle(
                              fontSize: 12,
                              fontWeight: FontWeight.bold,
                              color: typeColor,
                            ),
                          ),
                        ],
                      ),
                    ),
                    const Spacer(),
                    Text(
                      _formatDate(activity.date),
                      style: const TextStyle(
                        fontSize: 12,
                        fontWeight: FontWeight.w600,
                        color: AppColors.inkSoft,
                      ),
                    ),
                    if (onDelete != null) ...[
                      const SizedBox(width: 4),
                      IconButton(
                        icon: const Icon(Icons.delete_outline_rounded, size: 18),
                        color: AppColors.inkSoft.withAlpha(180),
                        tooltip: 'Delete',
                        padding: EdgeInsets.zero,
                        constraints: const BoxConstraints(minWidth: 28, minHeight: 28),
                        onPressed: () => _showDeleteConfirmation(context),
                      ),
                    ],
                  ],
                ),

                const SizedBox(height: 10),

                // Primary Summary Line
                Text(
                  summaryText,
                  maxLines: 2,
                  overflow: TextOverflow.ellipsis,
                  style: const TextStyle(
                    fontSize: 15,
                    fontWeight: FontWeight.w700,
                    color: AppColors.ink,
                    height: 1.3,
                  ),
                ),

                const SizedBox(height: 8),

                // Field / Cycle Location Info
                Row(
                  children: [
                    const Icon(Icons.pin_drop_outlined, size: 14, color: AppColors.inkSoft),
                    const SizedBox(width: 4),
                    Expanded(
                      child: Text(
                        [
                          if (activity.fieldName != null) activity.fieldName!,
                          if (activity.cycleName != null) activity.cycleName!,
                        ].join(' · '),
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                        style: const TextStyle(
                          fontSize: 12,
                          color: AppColors.inkSoft,
                        ),
                      ),
                    ),
                  ],
                ),

                const SizedBox(height: 12),
                const Divider(height: 1, color: AppColors.line),
                const SizedBox(height: 8),

                // Bottom row: Logged by + Tap for details link
                Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    Expanded(
                      child: Text(
                        'By: ${activity.loggedByUserName.isNotEmpty ? activity.loggedByUserName : "Farmer"}',
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                        style: const TextStyle(
                          fontSize: 11,
                          fontStyle: FontStyle.italic,
                          color: AppColors.inkSoft,
                        ),
                      ),
                    ),
                    Row(
                      mainAxisSize: MainAxisSize.min,
                      children: const [
                        Text(
                          'View Details',
                          style: TextStyle(
                            fontSize: 12,
                            fontWeight: FontWeight.bold,
                            color: AppColors.forest,
                          ),
                        ),
                        SizedBox(width: 2),
                        Icon(
                          Icons.chevron_right_rounded,
                          size: 16,
                          color: AppColors.forest,
                        ),
                      ],
                    ),
                  ],
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}
