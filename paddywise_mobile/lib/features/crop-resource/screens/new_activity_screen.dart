import 'dart:convert';
import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import '../../../theme/app_theme.dart';
import '../models/crop_activity_models.dart';
import '../services/crop_activity_service.dart';
import '../widgets/activity_type_selector.dart';
import '../widgets/activity_form.dart';
import '../widgets/activity_success_dialog.dart';

class NewActivityScreen extends StatefulWidget {
  final int? cycleId;
  final CultivationCycleSummary? initialCycle;

  const NewActivityScreen({
    super.key,
    this.cycleId,
    this.initialCycle,
  });

  @override
  State<NewActivityScreen> createState() => _NewActivityScreenState();
}

class _NewActivityScreenState extends State<NewActivityScreen> {
  final GlobalKey<ActivityFormState> _formKey = GlobalKey<ActivityFormState>();

  ActivityType _activeTab = ActivityType.fertilizer;
  bool _isLoadingCycle = true;
  bool _isSubmitting = false;

  CultivationCycleSummary? _selectedCycle;
  List<CultivationCycleSummary> _availableCycles = [];

  @override
  void initState() {
    super.initState();
    _loadCycles();
  }

  Future<void> _loadCycles() async {
    setState(() => _isLoadingCycle = true);

    try {
      if (widget.initialCycle != null) {
        _selectedCycle = widget.initialCycle;
      }

      final cycles = await CropActivityService.getCycles();
      _availableCycles = cycles;

      if (_selectedCycle == null) {
        if (widget.cycleId != null) {
          _selectedCycle = cycles.firstWhere(
            (c) => c.id == widget.cycleId,
            orElse: () => cycles.isNotEmpty ? cycles.first : CropActivityService.defaultDemoCycle,
          );
        } else if (cycles.isNotEmpty) {
          _selectedCycle = cycles.first;
        } else {
          _selectedCycle = CropActivityService.defaultDemoCycle;
        }
      }
    } catch (_) {
      _selectedCycle = CropActivityService.defaultDemoCycle;
    } finally {
      if (mounted) {
        setState(() => _isLoadingCycle = false);
      }
    }
  }

  Future<void> _handleActivitySubmit(Map<String, dynamic> data) async {
    if (_selectedCycle == null) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('No cultivation cycle selected. Please select a valid cycle before saving.'),
          backgroundColor: AppColors.errorText,
        ),
      );
      return;
    }

    setState(() => _isSubmitting = true);

    try {
      final requestPayload = CreateCropActivityRequest(
        activityType: data['activityType'] as String,
        date: data['date'] as String,
        detailsJson: jsonEncode(data),
      );

      await CropActivityService.createActivity(
        cycleId: _selectedCycle!.id,
        request: requestPayload,
      );

      if (!mounted) return;

      // Show success modal status matching web ActivityPanel
      await ActivitySuccessDialog.show(
        context: context,
        isSuccess: true,
        title: 'Data Saved Successfully',
        message: '${data['activityType']} activity data was saved successfully for ${_selectedCycle!.displayName}!',
        data: data,
        onBackToDashboard: () {
          context.go('/dashboard');
        },
        onBackToCycle: () {
          _navigateBack();
        },
        onAddAnother: () {
          _formKey.currentState?.resetForm();
        },
      );
    } catch (e) {
      if (!mounted) return;
      final rawMsg = e.toString().replaceFirst('Exception: ', '');
      await ActivitySuccessDialog.show(
        context: context,
        isSuccess: false,
        title: 'Failed to Save Data',
        message: rawMsg,
        onBackToCycle: () {
          _navigateBack();
        },
        onAddAnother: () {},
      );
    } finally {
      if (mounted) {
        setState(() => _isSubmitting = false);
      }
    }
  }

  void _navigateBack() {
    if (context.canPop()) {
      context.pop();
    } else {
      context.go('/dashboard');
    }
  }

  void _showCyclePicker() {
    if (_availableCycles.isEmpty) return;

    showModalBottomSheet(
      context: context,
      backgroundColor: Colors.white,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(16)),
      ),
      builder: (ctx) => SafeArea(
        child: Padding(
          padding: const EdgeInsets.symmetric(vertical: 16),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              const Padding(
                padding: EdgeInsets.symmetric(horizontal: 20, vertical: 8),
                child: Text(
                  'Select Cultivation Cycle',
                  style: TextStyle(
                    fontSize: 18,
                    fontWeight: FontWeight.bold,
                    fontFamily: 'serif',
                    color: AppColors.ink,
                  ),
                ),
              ),
              const Divider(color: AppColors.line),
              ..._availableCycles.map(
                (cycle) {
                  final isCurrent = cycle.id == _selectedCycle?.id;
                  return ListTile(
                    leading: Icon(
                      Icons.grass_rounded,
                      color: isCurrent ? AppColors.shoot : AppColors.inkSoft,
                    ),
                    title: Text(
                      cycle.displayName,
                      style: TextStyle(
                        fontWeight: isCurrent ? FontWeight.bold : FontWeight.w500,
                        color: isCurrent ? AppColors.forest : AppColors.ink,
                      ),
                    ),
                    subtitle: Text(
                      '${cycle.fieldName} • Stage: ${cycle.currentStage}',
                      style: const TextStyle(fontSize: 12, color: AppColors.inkSoft),
                    ),
                    trailing: isCurrent
                        ? const Icon(Icons.check_circle, color: AppColors.shoot, size: 20)
                        : null,
                    onTap: () {
                      Navigator.pop(ctx);
                      setState(() {
                        _selectedCycle = cycle;
                      });
                      _formKey.currentState?.resetForm();
                    },
                  );
                },
              ),
            ],
          ),
        ),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.cream,
      body: SafeArea(
        child: _isLoadingCycle
            ? const Center(
                child: CircularProgressIndicator(color: AppColors.forest),
              )
            : SingleChildScrollView(
                padding: const EdgeInsets.all(16),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    // Breadcrumb navigation linking to Dashboard & Cultivation Cycle
                    Row(
                      children: [
                        InkWell(
                          onTap: () => context.go('/dashboard'),
                          borderRadius: BorderRadius.circular(6),
                          child: Padding(
                            padding: const EdgeInsets.symmetric(vertical: 4, horizontal: 2),
                            child: Row(
                              mainAxisSize: MainAxisSize.min,
                              children: const [
                                Icon(Icons.dashboard_outlined, size: 16, color: AppColors.forest),
                                SizedBox(width: 4),
                                Text(
                                  'Dashboard',
                                  style: TextStyle(
                                    fontSize: 13,
                                    fontWeight: FontWeight.bold,
                                    color: AppColors.forest,
                                  ),
                                ),
                              ],
                            ),
                          ),
                        ),
                        const Padding(
                          padding: EdgeInsets.symmetric(horizontal: 6),
                          child: Text('/', style: TextStyle(color: AppColors.inkSoft, fontSize: 13)),
                        ),
                        InkWell(
                          onTap: _navigateBack,
                          borderRadius: BorderRadius.circular(6),
                          child: Padding(
                            padding: const EdgeInsets.symmetric(vertical: 4, horizontal: 2),
                            child: Row(
                              mainAxisSize: MainAxisSize.min,
                              children: const [
                                Icon(Icons.arrow_back_rounded, size: 16, color: AppColors.forest),
                                SizedBox(width: 4),
                                Text(
                                  'Back to Cultivation Cycle',
                                  style: TextStyle(
                                    fontSize: 13,
                                    fontWeight: FontWeight.bold,
                                    color: AppColors.forest,
                                  ),
                                ),
                              ],
                            ),
                          ),
                        ),
                      ],
                    ),
                    const SizedBox(height: 12),

                    // Eyebrow and Page Heading
                    const Text(
                      'RECORD ACTIVITY',
                      style: TextStyle(
                        fontSize: 11,
                        fontWeight: FontWeight.bold,
                        letterSpacing: 1.2,
                        color: AppColors.shoot,
                      ),
                    ),
                    const SizedBox(height: 4),

                    // Title with cycle switcher
                    Row(
                      mainAxisAlignment: MainAxisAlignment.spaceBetween,
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Expanded(
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Text(
                                _selectedCycle != null
                                    ? _selectedCycle!.displayName
                                    : 'Loading Cycle...',
                                style: const TextStyle(
                                  fontSize: 22,
                                  fontWeight: FontWeight.bold,
                                  color: AppColors.ink,
                                  fontFamily: 'serif',
                                ),
                              ),
                              const SizedBox(height: 4),
                              Text(
                                _selectedCycle?.fieldName ?? '',
                                style: const TextStyle(
                                  fontSize: 14,
                                  color: AppColors.inkSoft,
                                ),
                              ),
                            ],
                          ),
                        ),
                        if (_availableCycles.length > 1)
                          OutlinedButton.icon(
                            onPressed: _showCyclePicker,
                            icon: const Icon(Icons.sync_alt, size: 14),
                            label: const Text('Switch', style: TextStyle(fontSize: 12)),
                            style: OutlinedButton.styleFrom(
                              padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
                              visualDensity: VisualDensity.compact,
                              side: const BorderSide(color: AppColors.line),
                            ),
                          ),
                      ],
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
                            '${_activeTab.label} Activity Form',
                            style: const TextStyle(
                              fontSize: 16,
                              fontWeight: FontWeight.bold,
                              fontFamily: 'serif',
                              color: AppColors.ink,
                            ),
                          ),
                          const SizedBox(height: 16),

                          // 3. Activity Dynamic Form
                          ActivityForm(
                            key: _formKey,
                            activityType: _activeTab,
                            cycle: _selectedCycle,
                            onSubmit: _handleActivitySubmit,
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
