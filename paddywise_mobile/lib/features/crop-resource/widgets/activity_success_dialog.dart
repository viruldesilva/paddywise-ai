import 'package:flutter/material.dart';
import '../../../theme/app_theme.dart';

class ActivitySuccessDialog extends StatelessWidget {
  final bool isSuccess;
  final String title;
  final String message;
  final Map<String, dynamic>? data;
  final VoidCallback onBackToCycle;
  final VoidCallback onAddAnother;
  final VoidCallback? onBackToDashboard;

  const ActivitySuccessDialog({
    super.key,
    required this.isSuccess,
    required this.title,
    required this.message,
    this.data,
    required this.onBackToCycle,
    required this.onAddAnother,
    this.onBackToDashboard,
  });

  static Future<void> show({
    required BuildContext context,
    required bool isSuccess,
    required String title,
    required String message,
    Map<String, dynamic>? data,
    required VoidCallback onBackToCycle,
    required VoidCallback onAddAnother,
    VoidCallback? onBackToDashboard,
  }) {
    return showDialog<void>(
      context: context,
      barrierDismissible: false,
      builder: (ctx) => ActivitySuccessDialog(
        isSuccess: isSuccess,
        title: title,
        message: message,
        data: data,
        onBackToDashboard: onBackToDashboard != null
            ? () {
                Navigator.pop(ctx);
                onBackToDashboard();
              }
            : null,
        onBackToCycle: () {
          Navigator.pop(ctx);
          onBackToCycle();
        },
        onAddAnother: () {
          Navigator.pop(ctx);
          onAddAnother();
        },
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return Dialog(
      backgroundColor: Colors.white,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(16),
      ),
      insetPadding: const EdgeInsets.symmetric(horizontal: 20, vertical: 24),
      child: Padding(
        padding: const EdgeInsets.all(22),
        child: SingleChildScrollView(
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              // Icon Circle
              Container(
                width: 68,
                height: 68,
                decoration: BoxDecoration(
                  color: isSuccess ? AppColors.successBg : AppColors.errorBg,
                  shape: BoxShape.circle,
                ),
                child: Icon(
                  isSuccess ? Icons.check_circle_outline_rounded : Icons.error_outline_rounded,
                  size: 42,
                  color: isSuccess ? AppColors.successText : AppColors.errorText,
                ),
              ),
              const SizedBox(height: 16),

              // Title
              Text(
                title,
                textAlign: TextAlign.center,
                style: const TextStyle(
                  fontSize: 18,
                  fontWeight: FontWeight.bold,
                  fontFamily: 'serif',
                  color: AppColors.ink,
                ),
              ),
              const SizedBox(height: 8),

              // Message
              Text(
                message,
                textAlign: TextAlign.center,
                style: const TextStyle(
                  fontSize: 13,
                  color: AppColors.inkSoft,
                  height: 1.4,
                ),
              ),
              const SizedBox(height: 18),

              // Data summary card
              if (isSuccess && data != null && data!.isNotEmpty) ...[
                Container(
                  width: double.infinity,
                  padding: const EdgeInsets.all(14),
                  decoration: BoxDecoration(
                    color: AppColors.creamDeep.withAlpha(100),
                    borderRadius: BorderRadius.circular(10),
                    border: Border.all(color: AppColors.line),
                  ),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      const Text(
                        'SAVED DETAILS',
                        style: TextStyle(
                          fontSize: 10,
                          fontWeight: FontWeight.bold,
                          letterSpacing: 0.8,
                          color: AppColors.shoot,
                        ),
                      ),
                      const SizedBox(height: 8),
                      _buildDetailRow('Activity Type', data!['activityType']?.toString()),
                      _buildDetailRow('Date', data!['date']?.toString()),
                      if (data!['type'] != null)
                        _buildDetailRow('Fertilizer', data!['type']?.toString()),
                      if (data!['quantity'] != null)
                        _buildDetailRow(
                          'Quantity',
                          '${data!['quantity']} ${data!['activityType'] == 'Fertilizer' ? 'kg/ha' : 'ml/g'}',
                        ),
                      if (data!['cropStage'] != null)
                        _buildDetailRow('Growth Stage', data!['cropStage']?.toString()),
                      if (data!['region'] != null)
                        _buildDetailRow('Climatic Zone', data!['region']?.toString()),
                      if (data!['method'] != null)
                        _buildDetailRow('Method', data!['method']?.toString()),
                      if (data!['waterLevel'] != null)
                        _buildDetailRow('Water Level', '${data!['waterLevel']} cm'),
                      if (data!['duration'] != null)
                        _buildDetailRow('Duration', '${data!['duration']} hrs'),
                      if (data!['source'] != null)
                        _buildDetailRow('Water Source', data!['source']?.toString()),
                      if (data!['product'] != null)
                        _buildDetailRow('Product', data!['product']?.toString()),
                      if (data!['targetPest'] != null)
                        _buildDetailRow('Target Pest/Disease', data!['targetPest']?.toString()),
                      if (data!['specificActivity'] != null)
                        _buildDetailRow('Activity', data!['specificActivity']?.toString()),
                      if (data!['notes'] != null && data!['notes'].toString().isNotEmpty)
                        _buildDetailRow('Notes', data!['notes']?.toString()),
                    ],
                  ),
                ),
                const SizedBox(height: 20),
              ],

              // Actions
              Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  ElevatedButton.icon(
                    onPressed: isSuccess
                        ? (onBackToDashboard ?? onBackToCycle)
                        : () => Navigator.pop(context),
                    icon: Icon(
                      isSuccess ? Icons.dashboard_outlined : Icons.refresh_rounded,
                      size: 18,
                    ),
                    label: Text(isSuccess ? 'Back to Dashboard' : 'Try Again'),
                    style: ElevatedButton.styleFrom(
                      backgroundColor: AppColors.forest,
                      foregroundColor: AppColors.cream,
                      padding: const EdgeInsets.symmetric(vertical: 14),
                    ),
                  ),
                  const SizedBox(height: 8),
                  if (isSuccess) ...[
                    OutlinedButton(
                      onPressed: onBackToCycle,
                      style: OutlinedButton.styleFrom(
                        padding: const EdgeInsets.symmetric(vertical: 13),
                        side: const BorderSide(color: AppColors.line),
                        foregroundColor: AppColors.ink,
                      ),
                      child: const Text('Back to Cultivation Cycle'),
                    ),
                    const SizedBox(height: 8),
                    OutlinedButton(
                      onPressed: onAddAnother,
                      style: OutlinedButton.styleFrom(
                        padding: const EdgeInsets.symmetric(vertical: 13),
                        side: const BorderSide(color: AppColors.forest),
                        foregroundColor: AppColors.forest,
                      ),
                      child: const Text('Add Another Activity'),
                    ),
                  ],
                ],
              ),
            ],
          ),
        ),
      ),
    );
  }

  Widget _buildDetailRow(String label, String? value) {
    if (value == null || value.trim().isEmpty) return const SizedBox.shrink();
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 3),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          SizedBox(
            width: 110,
            child: Text(
              label,
              style: const TextStyle(
                fontSize: 12,
                color: AppColors.inkSoft,
              ),
            ),
          ),
          const SizedBox(width: 8),
          Expanded(
            child: Text(
              value,
              style: const TextStyle(
                fontSize: 12,
                fontWeight: FontWeight.w600,
                color: AppColors.ink,
              ),
            ),
          ),
        ],
      ),
    );
  }
}
