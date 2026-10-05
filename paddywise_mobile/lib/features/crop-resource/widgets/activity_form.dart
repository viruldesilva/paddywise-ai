import 'package:flutter/material.dart';
import '../../../theme/app_theme.dart';
import '../models/crop_activity_models.dart';

class ActivityForm extends StatefulWidget {
  final ActivityType activityType;
  final CultivationCycleSummary? cycle;
  final Future<void> Function(Map<String, dynamic> data) onSubmit;
  final bool isSubmitting;
  final Map<String, dynamic>? initialData;
  final String? submitButtonText;

  const ActivityForm({
    super.key,
    required this.activityType,
    required this.cycle,
    required this.onSubmit,
    this.isSubmitting = false,
    this.initialData,
    this.submitButtonText,
  });

  @override
  State<ActivityForm> createState() => ActivityFormState();
}

class ActivityFormState extends State<ActivityForm> {
  DateTime _selectedDate = DateTime.now();

  // Fertilizer
  String? _fertilizerType;
  final TextEditingController _fertQtyCtrl = TextEditingController();
  String? _cropStage;
  String? _region;
  String? _fertMethod;

  // Irrigation
  final TextEditingController _waterLevelCtrl = TextEditingController();
  final TextEditingController _durationCtrl = TextEditingController();
  String? _waterSource;

  // Pesticide
  final TextEditingController _pesticideProductCtrl = TextEditingController();
  final TextEditingController _targetPestCtrl = TextEditingController();
  final TextEditingController _pesticideQtyCtrl = TextEditingController();
  String? _pesticideMethod;

  // Other
  String? _specificActivity;
  final TextEditingController _notesCtrl = TextEditingController();

  // Validation State
  final Map<String, String> _errors = {};
  final Set<String> _touched = {};
  bool _hasSubmitted = false;

  @override
  void initState() {
    super.initState();
    _initDefaults();
  }

  @override
  void didUpdateWidget(covariant ActivityForm oldWidget) {
    super.didUpdateWidget(oldWidget);
    if (oldWidget.activityType != widget.activityType ||
        oldWidget.cycle != widget.cycle ||
        oldWidget.initialData != widget.initialData) {
      _initDefaults();
    }
  }

  @override
  void dispose() {
    _fertQtyCtrl.dispose();
    _waterLevelCtrl.dispose();
    _durationCtrl.dispose();
    _pesticideProductCtrl.dispose();
    _targetPestCtrl.dispose();
    _pesticideQtyCtrl.dispose();
    _notesCtrl.dispose();
    super.dispose();
  }

  void _initDefaults() {
    _errors.clear();
    _touched.clear();
    _hasSubmitted = false;

    final init = widget.initialData;
    if (init != null && init.isNotEmpty) {
      if (init['date'] != null && (init['date'] as String).isNotEmpty) {
        final parsed = DateTime.tryParse(init['date'].toString().split('T').first);
        if (parsed != null) _selectedDate = parsed;
      }
      _fertilizerType = init['type'] as String?;
      if (init['quantity'] != null) {
        final qStr = init['quantity'].toString();
        _fertQtyCtrl.text = qStr;
        _pesticideQtyCtrl.text = qStr;
      }
      _cropStage = init['cropStage'] as String? ??
          (widget.cycle?.currentStage != null ? _growthStageDisplay(widget.cycle!.currentStage) : null);
      _region = init['region'] as String?;
      _fertMethod = init['method'] as String?;

      if (init['waterLevel'] != null) {
        _waterLevelCtrl.text = init['waterLevel'].toString();
      }
      if (init['duration'] != null) {
        _durationCtrl.text = init['duration'].toString();
      }
      _waterSource = init['source'] as String?;

      _pesticideProductCtrl.text = init['product'] as String? ?? '';
      _targetPestCtrl.text = init['targetPest'] as String? ?? '';
      _pesticideMethod = init['method'] as String?;

      _specificActivity = init['specificActivity'] as String?;
      _notesCtrl.text = init['notes'] as String? ?? '';
    } else {
      // Reset date to today or within range
      final today = DateTime.now();
      _selectedDate = today;

      // Set default stage based on cycle
      final cycleStage = widget.cycle?.currentStage;
      if (cycleStage != null && cycleStage.isNotEmpty) {
        // Map cycle growth stages if needed
        _cropStage = _growthStageDisplay(cycleStage);
      }
    }
  }

  void resetForm() {
    setState(() {
      _initDefaults();
      _fertQtyCtrl.clear();
      _waterLevelCtrl.clear();
      _durationCtrl.clear();
      _pesticideProductCtrl.clear();
      _targetPestCtrl.clear();
      _pesticideQtyCtrl.clear();
      _notesCtrl.clear();
      _fertilizerType = null;
      _region = null;
      _fertMethod = null;
      _waterSource = null;
      _pesticideMethod = null;
      _specificActivity = null;
    });
  }

  String _formatDate(DateTime d) {
    return '${d.year.toString().padLeft(4, '0')}-${d.month.toString().padLeft(2, '0')}-${d.day.toString().padLeft(2, '0')}';
  }

  DateTime get _minAllowedDate {
    final oneWeekAgo = DateTime.now().subtract(const Duration(days: 7));
    if (widget.cycle?.sowingDate != null) {
      final sowing = DateTime.tryParse(widget.cycle!.sowingDate!);
      if (sowing != null && sowing.isAfter(oneWeekAgo)) {
        return DateTime(sowing.year, sowing.month, sowing.day);
      }
    }
    return DateTime(oneWeekAgo.year, oneWeekAgo.month, oneWeekAgo.day);
  }

  DateTime get _maxAllowedDate {
    final today = DateTime.now();
    if (widget.cycle?.actualHarvestDate != null) {
      final harvest = DateTime.tryParse(widget.cycle!.actualHarvestDate!);
      if (harvest != null && harvest.isBefore(today)) {
        return DateTime(harvest.year, harvest.month, harvest.day);
      }
    }
    return DateTime(today.year, today.month, today.day);
  }

  String _growthStageDisplay(String raw) {
    switch (raw) {
      case 'Nursery':
        return 'Nursery';
      case 'Tillering':
        return 'Tillering';
      case 'PanicleInitiation':
        return 'Panicle Initiation';
      case 'Flowering':
        return 'Flowering';
      case 'GrainFilling':
        return 'Grain Filling';
      case 'Harvest':
        return 'Harvest';
      default:
        return raw;
    }
  }

  void _validateField(String field) {
    final errs = Map<String, String>.from(_errors);

    // Date
    if (field == 'date') {
      final dateStr = _formatDate(_selectedDate);
      final minStr = _formatDate(_minAllowedDate);
      final maxStr = _formatDate(_maxAllowedDate);

      if (dateStr.compareTo(maxStr) > 0) {
        errs['date'] = 'Activity date cannot be in the future.';
      } else if (dateStr.compareTo(minStr) < 0) {
        errs['date'] = 'Activity date must be within the last week (since $minStr).';
      } else {
        errs.remove('date');
      }
    }

    // Fertilizer
    if (widget.activityType == ActivityType.fertilizer) {
      if (field == 'fertType') {
        if (_fertilizerType == null || _fertilizerType!.isEmpty) {
          errs['fertType'] = 'Please select a fertilizer type.';
        } else {
          errs.remove('fertType');
        }
      }
      if (field == 'fertQty') {
        final val = double.tryParse(_fertQtyCtrl.text.trim());
        if (val == null || val <= 0) {
          errs['fertQty'] = 'Quantity must be greater than 0 kg/ha.';
        } else if (val > 5000) {
          errs['fertQty'] = 'Quantity cannot exceed 5,000 kg/ha.';
        } else {
          errs.remove('fertQty');
        }
      }
      if (field == 'cropStage') {
        if (_cropStage == null || _cropStage!.isEmpty) {
          errs['cropStage'] = 'Please select a crop growth stage.';
        } else {
          errs.remove('cropStage');
        }
      }
      if (field == 'region') {
        if (_region == null || _region!.isEmpty) {
          errs['region'] = 'Please select a climatic zone / region.';
        } else {
          errs.remove('region');
        }
      }
      if (field == 'fertMethod') {
        if (_fertMethod == null || _fertMethod!.isEmpty) {
          errs['fertMethod'] = 'Please select an application method.';
        } else {
          errs.remove('fertMethod');
        }
      }
    }

    // Irrigation
    if (widget.activityType == ActivityType.irrigation) {
      if (field == 'waterLevel') {
        final val = double.tryParse(_waterLevelCtrl.text.trim());
        if (val == null || val < 0) {
          errs['waterLevel'] = 'Water level must be 0 cm or greater.';
        } else if (val > 150) {
          errs['waterLevel'] = 'Water level cannot exceed 150 cm.';
        } else {
          errs.remove('waterLevel');
        }
      }
      if (field == 'duration') {
        final val = double.tryParse(_durationCtrl.text.trim());
        if (val == null || val <= 0) {
          errs['duration'] = 'Duration must be greater than 0 hours.';
        } else if (val > 72) {
          errs['duration'] = 'Duration cannot exceed 72 hours.';
        } else {
          errs.remove('duration');
        }
      }
      if (field == 'waterSource') {
        if (_waterSource == null || _waterSource!.isEmpty) {
          errs['waterSource'] = 'Please select a water source.';
        } else {
          errs.remove('waterSource');
        }
      }
    }

    // Pesticide
    if (widget.activityType == ActivityType.pesticide) {
      if (field == 'pesticideProduct') {
        final text = _pesticideProductCtrl.text.trim();
        if (text.isEmpty) {
          errs['pesticideProduct'] = 'Product name is required.';
        } else if (text.length < 2) {
          errs['pesticideProduct'] = 'Product name must be at least 2 characters.';
        } else if (text.length > 100) {
          errs['pesticideProduct'] = 'Product name cannot exceed 100 characters.';
        } else {
          errs.remove('pesticideProduct');
        }
      }
      if (field == 'targetPest') {
        final text = _targetPestCtrl.text.trim();
        if (text.isEmpty) {
          errs['targetPest'] = 'Target pest / disease is required.';
        } else if (text.length < 2) {
          errs['targetPest'] = 'Target pest/disease must be at least 2 characters.';
        } else if (text.length > 100) {
          errs['targetPest'] = 'Target pest/disease cannot exceed 100 characters.';
        } else {
          errs.remove('targetPest');
        }
      }
      if (field == 'pesticideQty') {
        final val = double.tryParse(_pesticideQtyCtrl.text.trim());
        if (val == null || val <= 0) {
          errs['pesticideQty'] = 'Quantity must be greater than 0 ml/g.';
        } else if (val > 100000) {
          errs['pesticideQty'] = 'Quantity exceeds maximum limit.';
        } else {
          errs.remove('pesticideQty');
        }
      }
      if (field == 'pesticideMethod') {
        if (_pesticideMethod == null || _pesticideMethod!.isEmpty) {
          errs['pesticideMethod'] = 'Please select an application method.';
        } else {
          errs.remove('pesticideMethod');
        }
      }
    }

    // Other
    if (widget.activityType == ActivityType.other) {
      if (field == 'specificActivity') {
        if (_specificActivity == null || _specificActivity!.isEmpty) {
          errs['specificActivity'] = 'Please select an activity type.';
        } else {
          errs.remove('specificActivity');
        }
      }
      if (field == 'notes') {
        if (_notesCtrl.text.length > 500) {
          errs['notes'] = 'Notes cannot exceed 500 characters.';
        } else {
          errs.remove('notes');
        }
      }
    }

    setState(() {
      _errors.clear();
      _errors.addAll(errs);
    });
  }

  bool _validateAll() {
    _touched.add('date');
    _validateField('date');

    switch (widget.activityType) {
      case ActivityType.fertilizer:
        _touched.addAll(['fertType', 'fertQty', 'cropStage', 'region', 'fertMethod']);
        _validateField('fertType');
        _validateField('fertQty');
        _validateField('cropStage');
        _validateField('region');
        _validateField('fertMethod');
        break;
      case ActivityType.irrigation:
        _touched.addAll(['waterLevel', 'duration', 'waterSource']);
        _validateField('waterLevel');
        _validateField('duration');
        _validateField('waterSource');
        break;
      case ActivityType.pesticide:
        _touched.addAll(['pesticideProduct', 'targetPest', 'pesticideQty', 'pesticideMethod']);
        _validateField('pesticideProduct');
        _validateField('targetPest');
        _validateField('pesticideQty');
        _validateField('pesticideMethod');
        break;
      case ActivityType.other:
        _touched.addAll(['specificActivity', 'notes']);
        _validateField('specificActivity');
        _validateField('notes');
        break;
    }

    return _errors.isEmpty;
  }

  Future<void> _handleDatePicker() async {
    final effectiveFirstDate = _selectedDate.isBefore(_minAllowedDate) ? _selectedDate : _minAllowedDate;
    final effectiveLastDate = _selectedDate.isAfter(_maxAllowedDate) ? _selectedDate : _maxAllowedDate;
    final picked = await showDatePicker(
      context: context,
      initialDate: _selectedDate.isBefore(effectiveFirstDate)
          ? effectiveFirstDate
          : (_selectedDate.isAfter(effectiveLastDate) ? effectiveLastDate : _selectedDate),
      firstDate: effectiveFirstDate,
      lastDate: effectiveLastDate,
      builder: (context, child) {
        return Theme(
          data: Theme.of(context).copyWith(
            colorScheme: const ColorScheme.light(
              primary: AppColors.forest,
              onPrimary: AppColors.cream,
              onSurface: AppColors.ink,
            ),
          ),
          child: child!,
        );
      },
    );

    if (picked != null) {
      setState(() {
        _selectedDate = picked;
      });
      _touched.add('date');
      _validateField('date');
    }
  }

  void _handleSubmit() {
    setState(() {
      _hasSubmitted = true;
    });

    if (!_validateAll()) {
      return;
    }

    final dateStr = _formatDate(_selectedDate);
    final Map<String, dynamic> data = {
      'activityType': widget.activityType.label,
      'date': dateStr,
    };

    switch (widget.activityType) {
      case ActivityType.fertilizer:
        data['type'] = _fertilizerType;
        data['quantity'] = double.tryParse(_fertQtyCtrl.text.trim()) ?? 0;
        data['cropStage'] = _cropStage;
        data['region'] = _region;
        data['method'] = _fertMethod;
        break;
      case ActivityType.irrigation:
        data['waterLevel'] = double.tryParse(_waterLevelCtrl.text.trim()) ?? 0;
        data['duration'] = double.tryParse(_durationCtrl.text.trim()) ?? 0;
        data['source'] = _waterSource;
        break;
      case ActivityType.pesticide:
        data['product'] = _pesticideProductCtrl.text.trim();
        data['targetPest'] = _targetPestCtrl.text.trim();
        data['quantity'] = double.tryParse(_pesticideQtyCtrl.text.trim()) ?? 0;
        data['method'] = _pesticideMethod;
        break;
      case ActivityType.other:
        data['specificActivity'] = _specificActivity;
        data['notes'] = _notesCtrl.text.trim();
        break;
    }

    widget.onSubmit(data);
  }

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        // Validation Summary Banner
        if (_hasSubmitted && _errors.isNotEmpty) ...[
          Container(
            padding: const EdgeInsets.all(12),
            decoration: BoxDecoration(
              color: AppColors.errorBg,
              borderRadius: BorderRadius.circular(8),
              border: Border.all(color: AppColors.errorText.withAlpha(80)),
            ),
            child: Row(
              children: const [
                Icon(Icons.warning_amber_rounded, color: AppColors.errorText, size: 20),
                SizedBox(width: 8),
                Expanded(
                  child: Text(
                    'Please correct the highlighted fields before saving the activity.',
                    style: TextStyle(
                      fontSize: 12,
                      fontWeight: FontWeight.w600,
                      color: AppColors.errorText,
                    ),
                  ),
                ),
              ],
            ),
          ),
          const SizedBox(height: 16),
        ],

        // Date field (Universal for all activity types)
        _buildDateField(),
        const SizedBox(height: 16),

        // Activity Type Specific Fields
        if (widget.activityType == ActivityType.fertilizer) ..._buildFertilizerFields(),
        if (widget.activityType == ActivityType.irrigation) ..._buildIrrigationFields(),
        if (widget.activityType == ActivityType.pesticide) ..._buildPesticideFields(),
        if (widget.activityType == ActivityType.other) ..._buildOtherFields(),

        const SizedBox(height: 24),

        // Submit Button
        ElevatedButton(
          onPressed: widget.isSubmitting ? null : _handleSubmit,
          style: ElevatedButton.styleFrom(
            backgroundColor: AppColors.forest,
            foregroundColor: AppColors.cream,
            padding: const EdgeInsets.symmetric(vertical: 16),
            shape: RoundedRectangleBorder(
              borderRadius: BorderRadius.circular(10),
            ),
          ),
          child: widget.isSubmitting
              ? Row(
                  mainAxisAlignment: MainAxisAlignment.center,
                  children: [
                    const SizedBox(
                      width: 18,
                      height: 18,
                      child: CircularProgressIndicator(
                        strokeWidth: 2,
                        valueColor: AlwaysStoppedAnimation<Color>(AppColors.cream),
                      ),
                    ),
                    const SizedBox(width: 12),
                    Text(
                      widget.submitButtonText != null
                          ? 'Updating Activity...'
                          : 'Saving Activity...',
                      style: const TextStyle(fontSize: 15, fontWeight: FontWeight.bold),
                    ),
                  ],
                )
              : Text(
                  widget.submitButtonText ?? 'Save ${widget.activityType.label} Activity',
                  style: const TextStyle(fontSize: 15, fontWeight: FontWeight.bold),
                ),
        ),
      ],
    );
  }

  // Universal Date Picker Field
  Widget _buildDateField() {
    final isInvalid = (_touched.contains('date') || _hasSubmitted) && _errors.containsKey('date');
    final minDateStr = _formatDate(_minAllowedDate);
    final maxDateStr = _formatDate(_maxAllowedDate);

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        _buildLabel('Activity Date', isRequired: true),
        InkWell(
          onTap: _handleDatePicker,
          borderRadius: BorderRadius.circular(8),
          child: Container(
            padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 14),
            decoration: BoxDecoration(
              color: Colors.white,
              borderRadius: BorderRadius.circular(8),
              border: Border.all(
                color: isInvalid ? AppColors.errorText : AppColors.line,
                width: isInvalid ? 1.5 : 1.0,
              ),
            ),
            child: Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                Row(
                  children: [
                    const Icon(Icons.calendar_today_rounded, size: 18, color: AppColors.shoot),
                    const SizedBox(width: 10),
                    Text(
                      _formatDate(_selectedDate),
                      style: const TextStyle(
                        fontSize: 14,
                        fontWeight: FontWeight.w600,
                        color: AppColors.ink,
                      ),
                    ),
                  ],
                ),
                const Icon(Icons.arrow_drop_down, color: AppColors.inkSoft),
              ],
            ),
          ),
        ),
        const SizedBox(height: 4),
        Text(
          'Allowed: Within the last week ($minDateStr to $maxDateStr)',
          style: const TextStyle(fontSize: 11, color: AppColors.inkSoft),
        ),
        if (isInvalid) _buildErrorText(_errors['date']!),
      ],
    );
  }

  // 1. Fertilizer Fields
  List<Widget> _buildFertilizerFields() {
    final typeInvalid = (_touched.contains('fertType') || _hasSubmitted) && _errors.containsKey('fertType');
    final qtyInvalid = (_touched.contains('fertQty') || _hasSubmitted) && _errors.containsKey('fertQty');
    final stageInvalid = (_touched.contains('cropStage') || _hasSubmitted) && _errors.containsKey('cropStage');
    final regionInvalid = (_touched.contains('region') || _hasSubmitted) && _errors.containsKey('region');
    final methodInvalid = (_touched.contains('fertMethod') || _hasSubmitted) && _errors.containsKey('fertMethod');

    return [
      _buildLabel('Fertilizer Type', isRequired: true),
      _buildDropdown(
        value: _fertilizerType,
        hint: 'Select fertilizer type',
        items: const ['Urea', 'TSP', 'MOP', 'Organic Compost'],
        isError: typeInvalid,
        onChanged: (val) {
          setState(() => _fertilizerType = val);
          _touched.add('fertType');
          _validateField('fertType');
        },
      ),
      if (typeInvalid) _buildErrorText(_errors['fertType']!),
      const SizedBox(height: 14),

      _buildLabel('Quantity (kg/ha)', isRequired: true),
      _buildTextInput(
        controller: _fertQtyCtrl,
        hint: 'e.g. 50',
        keyboardType: const TextInputType.numberWithOptions(decimal: true),
        isError: qtyInvalid,
        onChanged: (_) {
          _touched.add('fertQty');
          _validateField('fertQty');
        },
      ),
      if (qtyInvalid) _buildErrorText(_errors['fertQty']!),
      const SizedBox(height: 14),

      _buildLabel('Crop Growth Stage', isRequired: true),
      _buildDropdown(
        value: _cropStage,
        hint: 'Select crop stage',
        items: const [
          'Nursery',
          'Tillering',
          'Panicle Initiation',
          'Flowering',
          'Grain Filling',
          'Harvest',
        ],
        isError: stageInvalid,
        onChanged: (val) {
          setState(() => _cropStage = val);
          _touched.add('cropStage');
          _validateField('cropStage');
        },
      ),
      if (stageInvalid) _buildErrorText(_errors['cropStage']!),
      const SizedBox(height: 14),

      _buildLabel('Climatic Region / Zone', isRequired: true),
      _buildDropdown(
        value: _region,
        hint: 'Select climatic zone',
        items: const ['Wet', 'Intermediate', 'Dry'],
        isError: regionInvalid,
        onChanged: (val) {
          setState(() => _region = val);
          _touched.add('region');
          _validateField('region');
        },
      ),
      if (regionInvalid) _buildErrorText(_errors['region']!),
      const SizedBox(height: 14),

      _buildLabel('Application Method', isRequired: true),
      _buildDropdown(
        value: _fertMethod,
        hint: 'Select application method',
        items: const ['Broadcasting', 'Top Dressing'],
        isError: methodInvalid,
        onChanged: (val) {
          setState(() => _fertMethod = val);
          _touched.add('fertMethod');
          _validateField('fertMethod');
        },
      ),
      if (methodInvalid) _buildErrorText(_errors['fertMethod']!),
    ];
  }

  // 2. Irrigation Fields
  List<Widget> _buildIrrigationFields() {
    final waterInvalid = (_touched.contains('waterLevel') || _hasSubmitted) && _errors.containsKey('waterLevel');
    final durInvalid = (_touched.contains('duration') || _hasSubmitted) && _errors.containsKey('duration');
    final srcInvalid = (_touched.contains('waterSource') || _hasSubmitted) && _errors.containsKey('waterSource');

    return [
      _buildLabel('Water Level (cm)', isRequired: true),
      _buildTextInput(
        controller: _waterLevelCtrl,
        hint: 'e.g. 5',
        keyboardType: const TextInputType.numberWithOptions(decimal: true),
        isError: waterInvalid,
        onChanged: (_) {
          _touched.add('waterLevel');
          _validateField('waterLevel');
        },
      ),
      if (waterInvalid) _buildErrorText(_errors['waterLevel']!),
      const SizedBox(height: 14),

      _buildLabel('Duration (hours)', isRequired: true),
      _buildTextInput(
        controller: _durationCtrl,
        hint: 'e.g. 2',
        keyboardType: const TextInputType.numberWithOptions(decimal: true),
        isError: durInvalid,
        onChanged: (_) {
          _touched.add('duration');
          _validateField('duration');
        },
      ),
      if (durInvalid) _buildErrorText(_errors['duration']!),
      const SizedBox(height: 14),

      _buildLabel('Water Source', isRequired: true),
      _buildDropdown(
        value: _waterSource,
        hint: 'Select water source',
        items: const ['Canal', 'Rain', 'Well'],
        isError: srcInvalid,
        onChanged: (val) {
          setState(() => _waterSource = val);
          _touched.add('waterSource');
          _validateField('waterSource');
        },
      ),
      if (srcInvalid) _buildErrorText(_errors['waterSource']!),
    ];
  }

  // 3. Pesticide Fields
  List<Widget> _buildPesticideFields() {
    final prodInvalid = (_touched.contains('pesticideProduct') || _hasSubmitted) && _errors.containsKey('pesticideProduct');
    final pestInvalid = (_touched.contains('targetPest') || _hasSubmitted) && _errors.containsKey('targetPest');
    final qtyInvalid = (_touched.contains('pesticideQty') || _hasSubmitted) && _errors.containsKey('pesticideQty');
    final methodInvalid = (_touched.contains('pesticideMethod') || _hasSubmitted) && _errors.containsKey('pesticideMethod');

    return [
      _buildLabel('Product Name', isRequired: true),
      _buildTextInput(
        controller: _pesticideProductCtrl,
        hint: 'e.g. Chlorantraniliprole',
        isError: prodInvalid,
        onChanged: (_) {
          _touched.add('pesticideProduct');
          _validateField('pesticideProduct');
        },
      ),
      if (prodInvalid) _buildErrorText(_errors['pesticideProduct']!),
      const SizedBox(height: 14),

      _buildLabel('Target Pest / Disease', isRequired: true),
      _buildTextInput(
        controller: _targetPestCtrl,
        hint: 'e.g. Stem Borer',
        isError: pestInvalid,
        onChanged: (_) {
          _touched.add('targetPest');
          _validateField('targetPest');
        },
      ),
      if (pestInvalid) _buildErrorText(_errors['targetPest']!),
      const SizedBox(height: 14),

      _buildLabel('Quantity (ml/g)', isRequired: true),
      _buildTextInput(
        controller: _pesticideQtyCtrl,
        hint: 'e.g. 100',
        keyboardType: const TextInputType.numberWithOptions(decimal: true),
        isError: qtyInvalid,
        onChanged: (_) {
          _touched.add('pesticideQty');
          _validateField('pesticideQty');
        },
      ),
      if (qtyInvalid) _buildErrorText(_errors['pesticideQty']!),
      const SizedBox(height: 14),

      _buildLabel('Application Method', isRequired: true),
      _buildDropdown(
        value: _pesticideMethod,
        hint: 'Select application method',
        items: const ['Spraying', 'Dusting'],
        isError: methodInvalid,
        onChanged: (val) {
          setState(() => _pesticideMethod = val);
          _touched.add('pesticideMethod');
          _validateField('pesticideMethod');
        },
      ),
      if (methodInvalid) _buildErrorText(_errors['pesticideMethod']!),
    ];
  }

  // 4. Other Fields
  List<Widget> _buildOtherFields() {
    final specInvalid = (_touched.contains('specificActivity') || _hasSubmitted) && _errors.containsKey('specificActivity');
    final notesInvalid = (_touched.contains('notes') || _hasSubmitted) && _errors.containsKey('notes');

    return [
      _buildLabel('Activity Type', isRequired: true),
      _buildDropdown(
        value: _specificActivity,
        hint: 'Select activity type',
        items: const [
          'Land Preparation',
          'Seeding',
          'Transplanting',
          'Weeding',
          'Harvesting',
        ],
        isError: specInvalid,
        onChanged: (val) {
          setState(() => _specificActivity = val);
          _touched.add('specificActivity');
          _validateField('specificActivity');
        },
      ),
      if (specInvalid) _buildErrorText(_errors['specificActivity']!),
      const SizedBox(height: 14),

      _buildLabel('Notes (Optional)'),
      TextField(
        controller: _notesCtrl,
        maxLines: 3,
        maxLength: 500,
        decoration: InputDecoration(
          hintText: 'Add optional activity notes...',
          fillColor: Colors.white,
          filled: true,
          errorText: notesInvalid ? _errors['notes'] : null,
          contentPadding: const EdgeInsets.all(14),
          border: OutlineInputBorder(
            borderRadius: BorderRadius.circular(8),
            borderSide: const BorderSide(color: AppColors.line),
          ),
          enabledBorder: OutlineInputBorder(
            borderRadius: BorderRadius.circular(8),
            borderSide: BorderSide(color: notesInvalid ? AppColors.errorText : AppColors.line),
          ),
        ),
        onChanged: (_) {
          _touched.add('notes');
          _validateField('notes');
        },
      ),
    ];
  }

  // Helpers
  Widget _buildLabel(String text, {bool isRequired = false}) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 6),
      child: RichText(
        text: TextSpan(
          text: text,
          style: const TextStyle(
            fontSize: 13,
            fontWeight: FontWeight.w600,
            color: AppColors.ink,
          ),
          children: [
            if (isRequired)
              const TextSpan(
                text: ' *',
                style: TextStyle(color: AppColors.errorText, fontWeight: FontWeight.bold),
              ),
          ],
        ),
      ),
    );
  }

  Widget _buildDropdown({
    required String? value,
    required String hint,
    required List<String> items,
    required ValueChanged<String?> onChanged,
    required bool isError,
  }) {
    final effectiveItems = List<String>.from(items);
    if (value != null && value.trim().isNotEmpty && !effectiveItems.contains(value)) {
      effectiveItems.insert(0, value);
    }

    return DropdownButtonFormField<String>(
      initialValue: effectiveItems.contains(value) ? value : null,
      decoration: InputDecoration(
        fillColor: Colors.white,
        filled: true,
        contentPadding: const EdgeInsets.symmetric(horizontal: 14, vertical: 12),
        border: OutlineInputBorder(
          borderRadius: BorderRadius.circular(8),
          borderSide: const BorderSide(color: AppColors.line),
        ),
        enabledBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(8),
          borderSide: BorderSide(
            color: isError ? AppColors.errorText : AppColors.line,
            width: isError ? 1.5 : 1.0,
          ),
        ),
      ),
      hint: Text(hint, style: const TextStyle(fontSize: 13, color: AppColors.inkSoft)),
      items: effectiveItems
          .map((item) => DropdownMenuItem<String>(
                value: item,
                child: Text(item, style: const TextStyle(fontSize: 14, color: AppColors.ink)),
              ))
          .toList(),
      onChanged: onChanged,
    );
  }

  Widget _buildTextInput({
    required TextEditingController controller,
    required String hint,
    TextInputType keyboardType = TextInputType.text,
    required bool isError,
    required ValueChanged<String> onChanged,
  }) {
    return TextField(
      controller: controller,
      keyboardType: keyboardType,
      decoration: InputDecoration(
        hintText: hint,
        fillColor: Colors.white,
        filled: true,
        contentPadding: const EdgeInsets.symmetric(horizontal: 14, vertical: 12),
        border: OutlineInputBorder(
          borderRadius: BorderRadius.circular(8),
          borderSide: const BorderSide(color: AppColors.line),
        ),
        enabledBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(8),
          borderSide: BorderSide(
            color: isError ? AppColors.errorText : AppColors.line,
            width: isError ? 1.5 : 1.0,
          ),
        ),
      ),
      onChanged: onChanged,
    );
  }

  Widget _buildErrorText(String error) {
    return Padding(
      padding: const EdgeInsets.only(top: 4),
      child: Row(
        children: [
          const Icon(Icons.error_outline, size: 14, color: AppColors.errorText),
          const SizedBox(width: 4),
          Expanded(
            child: Text(
              error,
              style: const TextStyle(
                fontSize: 11,
                color: AppColors.errorText,
                fontWeight: FontWeight.w500,
              ),
            ),
          ),
        ],
      ),
    );
  }
}
