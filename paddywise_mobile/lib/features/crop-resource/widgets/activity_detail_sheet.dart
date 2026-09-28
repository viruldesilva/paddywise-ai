import 'package:flutter/material.dart';
import '../../../theme/app_theme.dart';
import '../models/crop_activity_models.dart';

/// Modal bottom sheet showing comprehensive details of a recorded crop activity.
class ActivityDetailSheet extends StatelessWidget {
  final CropActivityDto activity;
  final VoidCallback? onDelete;
  final VoidCallback? onEdit;

  const ActivityDetailSheet({
    super.key,
    required this.activity,
    this.onDelete,
    this.onEdit,
  });

  static Future<void> show(
    BuildContext context, {
    required CropActivityDto activity,
    VoidCallback? onDelete,
    VoidCallback? onEdit,
  }) {
    return showModalBottomSheet(
      context: context,
      isScrollControlled: true,
      backgroundColor: Colors.transparent,
      builder: (ctx) => ActivityDetailSheet(
        activity: activity,
        onDelete: onDelete,
        onEdit: onEdit,
      ),
    );
  }

  Color _getTypeColor(String type) {
    switch (type.toLowerCase()) {
      case 'fertilizer':
        return const Color(0xFF2E7D32); // Rich leaf green
      case 'irrigation':
        return const Color(0xFF0288D1); // Water blue
      case 'pesticide':
        return const Color(0xFFD97706); // Amber warning
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

  String _formatDisplayDate(String? raw) {
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

  @override
  Widget build(BuildContext context) {
    final typeColor = _getTypeColor(activity.activityType);
    final details = activity.parsedDetails;

    return Container(
      constraints: BoxConstraints(
        maxHeight: MediaQuery.of(context).size.height * 0.88,
      ),
      decoration: const BoxDecoration(
        color: AppColors.cream,
        borderRadius: BorderRadius.vertical(top: Radius.circular(24)),
      ),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          // Drag handle
          Center(
            child: Container(
              margin: const EdgeInsets.only(top: 12, bottom: 8),
              width: 44,
              height: 4,
              decoration: BoxDecoration(
                color: AppColors.line.withAlpha(120),
                borderRadius: BorderRadius.circular(2),
              ),
            ),
          ),

          // Header
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 8),
            child: Row(
              children: [
                Container(
                  padding: const EdgeInsets.all(10),
                  decoration: BoxDecoration(
                    color: typeColor.withAlpha(28),
                    borderRadius: BorderRadius.circular(12),
                  ),
                  child: Icon(
                    _getTypeIcon(activity.activityType),
                    color: typeColor,
                    size: 24,
                  ),
                ),
                const SizedBox(width: 14),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        '${activity.activityType} Details',
                        style: const TextStyle(
                          fontSize: 18,
                          fontWeight: FontWeight.bold,
                          color: AppColors.ink,
                          fontFamily: 'serif',
                        ),
                      ),
                      const SizedBox(height: 2),
                      Text(
                        'Logged on ${_formatDisplayDate(activity.date)}',
                        style: const TextStyle(
                          fontSize: 12,
                          color: AppColors.inkSoft,
                        ),
                      ),
                    ],
                  ),
                ),
                if (onEdit != null)
                  IconButton(
                    icon: const Icon(Icons.edit_outlined, color: AppColors.forest),
                    onPressed: () {
                      Navigator.of(context).pop();
                      onEdit!();
                    },
                    tooltip: 'Edit Activity',
                  ),
                IconButton(
                  icon: const Icon(Icons.close_rounded, color: AppColors.inkSoft),
                  onPressed: () => Navigator.of(context).pop(),
                  tooltip: 'Close',
                ),
              ],
            ),
          ),
          const Divider(height: 1, color: AppColors.line),

          // Content body
          Flexible(
            child: ListView(
              padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 16),
              shrinkWrap: true,
              children: [
                // General Context Card (Cycle & Field)
                _buildSectionHeader('Context & Field Location', Icons.pin_drop_outlined),
                const SizedBox(height: 8),
                _buildInfoCard([
                  _InfoRow(
                    label: 'Cultivation Cycle',
                    value: activity.cycleName ?? 'Cycle #${activity.cultivationCycleId}',
                    icon: Icons.calendar_month_outlined,
                  ),
                  if (activity.fieldName != null)
                    _InfoRow(
                      label: 'Field / Plot',
                      value: activity.fieldName!,
                      icon: Icons.grass_outlined,
                    ),
                  if (activity.farmerName != null)
                    _InfoRow(
                      label: 'Farmer',
                      value: activity.farmerName!,
                      icon: Icons.person_outline_rounded,
                    ),
                  _InfoRow(
                    label: 'Recorded By',
                    value: activity.loggedByUserName.isNotEmpty
                        ? activity.loggedByUserName
                        : 'System',
                    icon: Icons.badge_outlined,
                  ),
                  _InfoRow(
                    label: 'Activity Date',
                    value: _formatDisplayDate(activity.date),
                    icon: Icons.event_available_rounded,
                    highlight: true,
                  ),
                ]),

                const SizedBox(height: 18),

                // Specific Activity Attributes
                _buildSectionHeader(
                  '${activity.activityType} Parameters',
                  Icons.tune_rounded,
                ),
                const SizedBox(height: 8),
                _buildTypeSpecificDetails(details),

                const SizedBox(height: 12),
              ],
            ),
          ),

          // Persistent Bottom Actions (Edit & Delete)
          if (onEdit != null || onDelete != null) ...[
            const Divider(height: 1, color: AppColors.line),
            SafeArea(
              top: false,
              child: Padding(
                padding: const EdgeInsets.fromLTRB(20, 12, 20, 16),
                child: Row(
                  children: [
                    if (onEdit != null)
                      Expanded(
                        child: ElevatedButton.icon(
                          onPressed: () {
                            Navigator.of(context).pop();
                            onEdit!();
                          },
                          icon: const Icon(Icons.edit_outlined, size: 18),
                          label: const Text(
                            'Edit Activity',
                            style: TextStyle(fontWeight: FontWeight.bold, fontSize: 14),
                          ),
                          style: ElevatedButton.styleFrom(
                            backgroundColor: AppColors.forest,
                            foregroundColor: AppColors.cream,
                            padding: const EdgeInsets.symmetric(vertical: 14),
                            shape: RoundedRectangleBorder(
                              borderRadius: BorderRadius.circular(10),
                            ),
                          ),
                        ),
                      ),
                    if (onEdit != null && onDelete != null) const SizedBox(width: 12),
                    if (onDelete != null)
                      OutlinedButton.icon(
                        onPressed: () {
                          Navigator.of(context).pop();
                          onDelete!();
                        },
                        icon: const Icon(Icons.delete_outline_rounded,
                            color: AppColors.errorText, size: 18),
                        label: const Text(
                          'Delete',
                          style: TextStyle(
                            color: AppColors.errorText,
                            fontWeight: FontWeight.bold,
                          ),
                        ),
                        style: OutlinedButton.styleFrom(
                          side: const BorderSide(color: AppColors.errorText),
                          padding: const EdgeInsets.symmetric(
                              vertical: 14, horizontal: 16),
                          shape: RoundedRectangleBorder(
                            borderRadius: BorderRadius.circular(10),
                          ),
                        ),
                      ),
                  ],
                ),
              ),
            ),
          ],
        ],
      ),
    );
  }

  Widget _buildSectionHeader(String title, IconData icon) {
    return Row(
      children: [
        Icon(icon, size: 16, color: AppColors.forest),
        const SizedBox(width: 6),
        Text(
          title.toUpperCase(),
          style: const TextStyle(
            fontSize: 11,
            fontWeight: FontWeight.bold,
            letterSpacing: 0.8,
            color: AppColors.forest,
          ),
        ),
      ],
    );
  }

  Widget _buildInfoCard(List<_InfoRow> rows) {
    return Container(
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(12),
        border: Border.all(color: AppColors.line),
      ),
      child: Column(
        children: [
          for (int i = 0; i < rows.length; i++) ...[
            Padding(
              padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 11),
              child: Row(
                children: [
                  Icon(rows[i].icon, size: 18, color: AppColors.inkSoft),
                  const SizedBox(width: 10),
                  Text(
                    rows[i].label,
                    style: const TextStyle(
                      fontSize: 13,
                      color: AppColors.inkSoft,
                    ),
                  ),
                  const Spacer(),
                  Flexible(
                    child: Text(
                      rows[i].value,
                      textAlign: TextAlign.end,
                      style: TextStyle(
                        fontSize: 13,
                        fontWeight: FontWeight.w600,
                        color: rows[i].highlight ? AppColors.forest : AppColors.ink,
                      ),
                    ),
                  ),
                ],
              ),
            ),
            if (i < rows.length - 1)
              const Divider(height: 1, indent: 14, endIndent: 14, color: AppColors.line),
          ],
        ],
      ),
    );
  }

  Widget _buildTypeSpecificDetails(Map<String, dynamic> data) {
    final type = activity.activityType.toLowerCase();

    if (type == 'fertilizer') {
      final qty = data['quantity'];
      return _buildInfoCard([
        _InfoRow(
          label: 'Fertilizer Type',
          value: (data['type'] as String?)?.isNotEmpty == true
              ? data['type'] as String
              : '—',
          icon: Icons.science_outlined,
          highlight: true,
        ),
        _InfoRow(
          label: 'Dosage / Quantity',
          value: qty != null ? '$qty kg/ha' : '—',
          icon: Icons.scale_outlined,
        ),
        _InfoRow(
          label: 'Crop Growth Stage',
          value: (data['cropStage'] as String?)?.isNotEmpty == true
              ? data['cropStage'] as String
              : '—',
          icon: Icons.timeline_outlined,
        ),
        _InfoRow(
          label: 'Agro-Ecological Region',
          value: (data['region'] as String?)?.isNotEmpty == true
              ? data['region'] as String
              : '—',
          icon: Icons.terrain_outlined,
        ),
        _InfoRow(
          label: 'Application Method',
          value: (data['method'] as String?)?.isNotEmpty == true
              ? data['method'] as String
              : '—',
          icon: Icons.scatter_plot_outlined,
        ),
      ]);
    } else if (type == 'irrigation') {
      final waterLevel = data['waterLevel'];
      final duration = data['duration'];
      return _buildInfoCard([
        _InfoRow(
          label: 'Water Source',
          value: (data['source'] as String?)?.isNotEmpty == true
              ? data['source'] as String
              : '—',
          icon: Icons.water_outlined,
          highlight: true,
        ),
        _InfoRow(
          label: 'Standing Water Depth',
          value: waterLevel != null ? '$waterLevel cm' : '—',
          icon: Icons.straighten_outlined,
        ),
        _InfoRow(
          label: 'Flooding Duration',
          value: duration != null ? '$duration hours' : '—',
          icon: Icons.timer_outlined,
        ),
      ]);
    } else if (type == 'pesticide') {
      final qty = data['quantity'];
      return _buildInfoCard([
        _InfoRow(
          label: 'Product / Chemical Name',
          value: (data['product'] as String?)?.isNotEmpty == true
              ? data['product'] as String
              : '—',
          icon: Icons.medication_outlined,
          highlight: true,
        ),
        _InfoRow(
          label: 'Target Pest or Disease',
          value: (data['targetPest'] as String?)?.isNotEmpty == true
              ? data['targetPest'] as String
              : '—',
          icon: Icons.bug_report_outlined,
        ),
        _InfoRow(
          label: 'Quantity Applied',
          value: qty != null ? '$qty ml/g' : '—',
          icon: Icons.colorize_outlined,
        ),
        _InfoRow(
          label: 'Application Method',
          value: (data['method'] as String?)?.isNotEmpty == true
              ? data['method'] as String
              : '—',
          icon: Icons.air_outlined,
        ),
      ]);
    } else {
      // Other
      final specific = data['specificActivity'] as String? ?? 'General Operation';
      final notes = data['notes'] as String? ?? 'No notes recorded.';
      return Container(
        decoration: BoxDecoration(
          color: Colors.white,
          borderRadius: BorderRadius.circular(12),
          border: Border.all(color: AppColors.line),
        ),
        padding: const EdgeInsets.all(14),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                const Icon(Icons.task_alt_rounded, size: 18, color: AppColors.forest),
                const SizedBox(width: 8),
                Text(
                  specific,
                  style: const TextStyle(
                    fontSize: 15,
                    fontWeight: FontWeight.bold,
                    color: AppColors.forestDeep,
                  ),
                ),
              ],
            ),
            const SizedBox(height: 10),
            const Divider(height: 1, color: AppColors.line),
            const SizedBox(height: 10),
            const Text(
              'Field Observations & Notes:',
              style: TextStyle(
                fontSize: 12,
                fontWeight: FontWeight.w600,
                color: AppColors.inkSoft,
              ),
            ),
            const SizedBox(height: 4),
            Text(
              notes,
              style: const TextStyle(
                fontSize: 13,
                color: AppColors.ink,
                height: 1.4,
              ),
            ),
          ],
        ),
      );
    }
  }
}

class _InfoRow {
  final String label;
  final String value;
  final IconData icon;
  final bool highlight;

  _InfoRow({
    required this.label,
    required this.value,
    required this.icon,
    this.highlight = false,
  });
}
