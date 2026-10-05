import 'dart:convert';
import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import '../../../theme/app_theme.dart';
import '../models/crop_activity_models.dart';
import '../services/crop_activity_service.dart';
import '../widgets/activity_type_selector.dart';
import '../widgets/activity_form.dart';
import '../widgets/activity_success_dialog.dart';

/// Screen allowing farmers to edit and update a previously recorded crop activity.
class EditActivityScreen extends StatefulWidget {
  final int? activityId;
  final CropActivityDto? initialActivity;

  const EditActivityScreen({
    super.key,
    this.activityId,
    this.initialActivity,
  });

  @override
  State<EditActivityScreen> createState() => _EditActivityScreenState();
}

class _EditActivityScreenState extends State<EditActivityScreen> {
  final GlobalKey<ActivityFormState> _formKey = GlobalKey<ActivityFormState>();

  CropActivityDto? _activity;
  CultivationCycleSummary? _cycle;
  ActivityType _activeTab = ActivityType.fertilizer;
  bool _isLoading = true;
  bool _isSubmitting = false;
  String? _loadError;

  @override
  void initState() {
    super.initState();
    _loadActivityAndCycle();
  }

  Future<void> _loadActivityAndCycle() async {
    setState(() {
      _isLoading = true;
      _loadError = null;
    });

    try {
      CropActivityDto? act = widget.initialActivity;
      if (act == null && widget.activityId != null) {
        act = await CropActivityService.getActivityById(widget.activityId!);
      }

      if (act == null) {
        setState(() {
          _isLoading = false;
          _loadError = 'Crop activity not found. It may have been removed.';
        });
        return;
      }

      _activity = act;
      _activeTab = ActivityType.fromString(act.activityType);

      // Load associated cultivation cycle
      CultivationCycleSummary? cycle;
      try {
        cycle = await CropActivityService.getCycleById(act.cultivationCycleId);
      } catch (_) {
        cycle = null;
      }

      if (mounted) {
        setState(() {
          _activity = act;
          _cycle = cycle;
          _isLoading = false;
        });
      }
    } catch (e) {
      if (mounted) {
        setState(() {
          _isLoading = false;
          _loadError = 'Failed to load activity details: $e';
        });
      }
    }
  }

  Future<void> _handleUpdateSubmit(Map<String, dynamic> data) async {
    if (_activity == null) return;

    setState(() => _isSubmitting = true);

    try {
      final requestPayload = UpdateCropActivityRequest(
        activityType: data['activityType'] as String,
        date: data['date'] as String,
        detailsJson: jsonEncode(data),
      );

      final updatedDto = await CropActivityService.updateActivity(
        activityId: _activity!.id,
        request: requestPayload,
      );

      if (!mounted) return;

      // Show success modal status matching web ActivityPanel
      await ActivitySuccessDialog.show(
        context: context,
        isSuccess: true,
        title: 'Activity Updated Successfully',
        message: '${data['activityType']} activity record has been updated successfully.',
        data: data,
        onBackToDashboard: () {
          context.go('/dashboard');
        },
        onBackToCycle: () {
          _navigateBack(updatedDto);
        },
        onAddAnother: () {
          _navigateBack(updatedDto);
        },
      );
    } catch (e) {
      if (!mounted) return;
      final rawMsg = e.toString().replaceFirst('Exception: ', '');
      await ActivitySuccessDialog.show(
        context: context,
        isSuccess: false,
        title: 'Failed to Update Activity',
        message: rawMsg,
        onBackToCycle: () {
          Navigator.of(context).pop();
        },
        onAddAnother: () {
          Navigator.of(context).pop();
        },
      );
    } finally {
      if (mounted) {
        setState(() => _isSubmitting = false);
      }
    }
  }

  void _navigateBack([CropActivityDto? result]) {
    if (context.canPop()) {
      context.pop(result);
    } else {
      context.go('/activities');
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
    return Scaffold(
      backgroundColor: AppColors.cream,
      appBar: AppBar(
        backgroundColor: AppColors.forest,
        foregroundColor: AppColors.cream,
        elevation: 0,
        title: const Text(
          'Edit Crop Activity',
          style: TextStyle(
            fontSize: 18,
            fontWeight: FontWeight.bold,
            fontFamily: 'serif',
          ),
        ),
        leading: IconButton(
          icon: const Icon(Icons.arrow_back_rounded),
          onPressed: () => _navigateBack(),
          tooltip: 'Back',
        ),
      ),
      body: SafeArea(
        child: _isLoading
            ? const Center(
                child: CircularProgressIndicator(color: AppColors.forest),
              )
            : _loadError != null
                ? Center(
                    child: Padding(
                      padding: const EdgeInsets.all(24),
                      child: Column(
                        mainAxisSize: MainAxisSize.min,
                        children: [
                          const Icon(Icons.error_outline_rounded,
                              size: 48, color: AppColors.errorText),
                          const SizedBox(height: 16),
                          Text(
                            _loadError!,
                            textAlign: TextAlign.center,
                            style: const TextStyle(
                              fontSize: 15,
                              color: AppColors.ink,
                              fontWeight: FontWeight.w600,
                            ),
                          ),
                          const SizedBox(height: 20),
                          ElevatedButton(
                            onPressed: () => _navigateBack(),
                            style: ElevatedButton.styleFrom(
                              backgroundColor: AppColors.forest,
                              foregroundColor: AppColors.cream,
                            ),
                            child: const Text('Return to Activities'),
                          ),
                        ],
                      ),
                    ),
                  )
                : SingleChildScrollView(
                    padding: const EdgeInsets.all(16),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        // Eyebrow and Page Heading
                        const Text(
                          'UPDATE OPERATION',
                          style: TextStyle(
                            fontSize: 11,
                            fontWeight: FontWeight.bold,
                            letterSpacing: 1.2,
                            color: AppColors.shoot,
                          ),
                        ),
                        const SizedBox(height: 4),

                        // Title with activity ID and field name
                        Row(
                          mainAxisAlignment: MainAxisAlignment.spaceBetween,
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Expanded(
                              child: Column(
                                crossAxisAlignment: CrossAxisAlignment.start,
                                children: [
                                  Text(
                                    'Edit ${_activeTab.label} Record',
                                    style: const TextStyle(
                                      fontSize: 22,
                                      fontWeight: FontWeight.bold,
                                      color: AppColors.ink,
                                      fontFamily: 'serif',
                                    ),
                                  ),
                                  const SizedBox(height: 4),
                                  Text(
                                    [
                                      if (_activity?.fieldName != null)
                                        _activity!.fieldName!,
                                      if (_activity?.cycleName != null)
                                        _activity!.cycleName!
                                      else if (_cycle != null)
                                        _cycle!.displayName,
                                    ].join(' · '),
                                    style: const TextStyle(
                                      fontSize: 13,
                                      color: AppColors.inkSoft,
                                    ),
                                  ),
                                ],
                              ),
                            ),
                            Container(
                              padding: const EdgeInsets.symmetric(
                                  horizontal: 10, vertical: 4),
                              decoration: BoxDecoration(
                                color: AppColors.forest.withAlpha(20),
                                borderRadius: BorderRadius.circular(12),
                                border: Border.all(
                                    color: AppColors.forest.withAlpha(60)),
                              ),
                              child: Text(
                                '#${_activity?.id ?? ""}',
                                style: const TextStyle(
                                  fontSize: 12,
                                  fontWeight: FontWeight.bold,
                                  color: AppColors.forest,
                                ),
                              ),
                            ),
                          ],
                        ),
                        const SizedBox(height: 12),

                        // Context Info Card
                        Container(
                          padding: const EdgeInsets.symmetric(
                              horizontal: 14, vertical: 10),
                          decoration: BoxDecoration(
                            color: AppColors.forest.withAlpha(12),
                            borderRadius: BorderRadius.circular(10),
                            border: Border.all(
                                color: AppColors.forest.withAlpha(35)),
                          ),
                          child: Row(
                            children: [
                              const Icon(Icons.info_outline_rounded,
                                  size: 16, color: AppColors.forest),
                              const SizedBox(width: 8),
                              Expanded(
                                child: Text(
                                  'Originally logged on ${_formatDisplayDate(_activity?.date)} by ${_activity?.loggedByUserName.isNotEmpty == true ? _activity!.loggedByUserName : "Farmer"}.',
                                  style: const TextStyle(
                                    fontSize: 12,
                                    color: AppColors.forestDeep,
                                    fontWeight: FontWeight.w500,
                                  ),
                                ),
                              ),
                            ],
                          ),
                        ),

                        const SizedBox(height: 16),

                        // Activity Panel Container
                        Container(
                          padding: const EdgeInsets.all(16),
                          decoration: BoxDecoration(
                            color: Colors.white,
                            borderRadius: BorderRadius.circular(14),
                            border: Border.all(color: AppColors.line),
                            boxShadow: [
                              BoxShadow(
                                color: Colors.black.withAlpha(8),
                                blurRadius: 10,
                                offset: const Offset(0, 4),
                              ),
                            ],
                          ),
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              // 1. Activity Type Selector Tabs
                              ActivityTypeSelector(
                                selectedType: _activeTab,
                                onTypeChanged: (newType) {
                                  setState(() {
                                    _activeTab = newType;
                                  });
                                },
                              ),
                              const SizedBox(height: 18),

                              // 2. Form Heading
                              Text(
                                '${_activeTab.label} Activity Details',
                                style: const TextStyle(
                                  fontSize: 16,
                                  fontWeight: FontWeight.bold,
                                  fontFamily: 'serif',
                                  color: AppColors.ink,
                                ),
                              ),
                              const SizedBox(height: 4),
                              const Text(
                                'Modify activity parameters and save changes to update crop records.',
                                style: TextStyle(
                                  fontSize: 12,
                                  color: AppColors.inkSoft,
                                ),
                              ),
                              const SizedBox(height: 16),

                              // 3. Activity Dynamic Form populated with existing data
                              ActivityForm(
                                key: _formKey,
                                activityType: _activeTab,
                                cycle: _cycle,
                                initialData: _activeTab.label.toLowerCase() ==
                                        _activity!.activityType.toLowerCase()
                                    ? {
                                        ..._activity!.parsedDetails,
                                        'date': _activity!.date,
                                      }
                                    : null,
                                submitButtonText:
                                    'Update ${_activeTab.label} Activity',
                                onSubmit: _handleUpdateSubmit,
                                isSubmitting: _isSubmitting,
                              ),
                            ],
                          ),
                        ),
                        const SizedBox(height: 24),
                      ],
                    ),
                  ),
      ),
    );
  }
}
