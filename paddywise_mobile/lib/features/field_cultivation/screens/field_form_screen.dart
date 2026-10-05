import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

import '../../../theme/app_theme.dart';
import '../models/field_models.dart';
import '../services/field_cultivation_service.dart';
import '../widgets/fc_common.dart';

/// Register a new field, or edit one when [fieldId] is given. Pops with the
/// saved [Field].
class FieldFormScreen extends StatefulWidget {
  final int? fieldId;
  final Field? initial;

  const FieldFormScreen({super.key, this.fieldId, this.initial});

  bool get isEditing => fieldId != null;

  @override
  State<FieldFormScreen> createState() => _FieldFormScreenState();
}

class _FieldFormScreenState extends State<FieldFormScreen> {
  final _formKey = GlobalKey<FormState>();
  final _nameController = TextEditingController();
  final _areaController = TextEditingController();
  final _soilController = TextEditingController();
  final _irrigationController = TextEditingController();
  final _latitudeController = TextEditingController();
  final _longitudeController = TextEditingController();

  List<Division>? _divisions;
  int? _divisionId;
  bool _isLoading = true;
  bool _isSaving = false;
  String? _loadError;
  String? _errorMessage;

  @override
  void initState() {
    super.initState();
    _load();
  }

  @override
  void dispose() {
    _nameController.dispose();
    _areaController.dispose();
    _soilController.dispose();
    _irrigationController.dispose();
    _latitudeController.dispose();
    _longitudeController.dispose();
    super.dispose();
  }

  Future<void> _load() async {
    setState(() {
      _isLoading = true;
      _loadError = null;
    });
    try {
      final divisions = await FieldCultivationService.getDivisions();
      Field? field = widget.initial;
      if (widget.isEditing && field == null) {
        field = await FieldCultivationService.getField(widget.fieldId!);
      }
      if (!mounted) return;
      setState(() {
        _divisions = divisions;
        if (field != null) _fill(field);
        _isLoading = false;
      });
    } catch (e) {
      if (!mounted) return;
      setState(() {
        _loadError = e.toString();
        _isLoading = false;
      });
    }
  }

  void _fill(Field field) {
    _nameController.text = field.name;
    _areaController.text = field.area == field.area.roundToDouble()
        ? field.area.toStringAsFixed(0)
        : '${field.area}';
    _soilController.text = field.soilType;
    _irrigationController.text = field.irrigationType;
    _latitudeController.text = field.latitude?.toString() ?? '';
    _longitudeController.text = field.longitude?.toString() ?? '';
    _divisionId = field.divisionId;
  }

  String? _validateRange(String? value, double min, double max, String label) {
    final text = value?.trim() ?? '';
    if (text.isEmpty) return null;
    final number = double.tryParse(text);
    if (number == null || number < min || number > max) {
      return '$label must be between $min and $max.';
    }
    return null;
  }

  Future<void> _save() async {
    FocusScope.of(context).unfocus();
    if (!_formKey.currentState!.validate()) return;

    setState(() {
      _isSaving = true;
      _errorMessage = null;
    });

    final latitude = _latitudeController.text.trim();
    final longitude = _longitudeController.text.trim();
    final request = FieldRequest(
      name: _nameController.text.trim(),
      area: double.parse(_areaController.text.trim()),
      soilType: _soilController.text.trim(),
      irrigationType: _irrigationController.text.trim(),
      divisionId: _divisionId!,
      latitude: latitude.isEmpty ? null : double.parse(latitude),
      longitude: longitude.isEmpty ? null : double.parse(longitude),
    );

    try {
      final saved = widget.isEditing
          ? await FieldCultivationService.updateField(widget.fieldId!, request)
          : await FieldCultivationService.createField(request);
      if (mounted) context.pop(saved);
    } catch (e) {
      if (!mounted) return;
      setState(() => _errorMessage = e.toString());
    } finally {
      if (mounted) setState(() => _isSaving = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.cream,
      appBar: fcFormAppBar(widget.isEditing ? 'Edit field' : 'Add a field'),
      body: SafeArea(child: _buildBody()),
    );
  }

  Widget _buildBody() {
    if (_isLoading) return const FcLoading();
    if (_loadError != null) {
      return FcStateView.error(message: _loadError!, onAction: _load);
    }

    return Form(
      key: _formKey,
      child: ListView(
        padding: const EdgeInsets.all(16),
        children: [
          const Text(
            'Tell us about the plot. The division decides which agricultural officer '
            'reviews your cultivation plans.',
            style: TextStyle(fontSize: 13, color: AppColors.inkSoft, height: 1.4),
          ),
          const FcFieldLabel('Field name'),
          TextFormField(
            controller: _nameController,
            textCapitalization: TextCapitalization.sentences,
            maxLength: FieldRules.nameMaxLength,
            decoration: const InputDecoration(
              hintText: 'e.g. Lower paddy, north bund',
              counterText: '',
            ),
            validator: (value) =>
                (value == null || value.trim().isEmpty) ? 'Field name is required.' : null,
          ),
          const FcFieldLabel('Area (acres)'),
          TextFormField(
            controller: _areaController,
            keyboardType: const TextInputType.numberWithOptions(decimal: true),
            decoration: const InputDecoration(hintText: 'e.g. 2.5', suffixText: 'acres'),
            validator: (value) {
              if (value == null || value.trim().isEmpty) return 'Area is required.';
              return _validateRange(value, FieldRules.areaMin, FieldRules.areaMax, 'Area');
            },
          ),
          const FcFieldLabel('Agrarian division'),
          DropdownButtonFormField<int>(
            initialValue: _divisionId,
            isExpanded: true,
            decoration: const InputDecoration(hintText: 'Choose your division'),
            items: [
              for (final division in _divisions ?? const <Division>[])
                DropdownMenuItem(value: division.id, child: Text(division.label)),
            ],
            onChanged: (value) => setState(() => _divisionId = value),
            validator: (value) => value == null ? 'Division is required.' : null,
          ),
          const FcFieldLabel('Soil type'),
          _SuggestedTextField(
            controller: _soilController,
            hint: 'e.g. Low humic gley',
            suggestions: kSoilTypeSuggestions,
            maxLength: FieldRules.soilTypeMaxLength,
          ),
          const FcFieldLabel('Water source'),
          _SuggestedTextField(
            controller: _irrigationController,
            hint: 'e.g. Major tank',
            suggestions: kIrrigationTypeSuggestions,
            maxLength: FieldRules.irrigationTypeMaxLength,
          ),
          const FcFieldLabel('Location (optional)'),
          Row(
            children: [
              Expanded(
                child: TextFormField(
                  controller: _latitudeController,
                  keyboardType: const TextInputType.numberWithOptions(
                      decimal: true, signed: true),
                  decoration: const InputDecoration(hintText: 'Latitude'),
                  validator: (v) => _validateRange(v, -90, 90, 'Latitude'),
                ),
              ),
              const SizedBox(width: 12),
              Expanded(
                child: TextFormField(
                  controller: _longitudeController,
                  keyboardType: const TextInputType.numberWithOptions(
                      decimal: true, signed: true),
                  decoration: const InputDecoration(hintText: 'Longitude'),
                  validator: (v) => _validateRange(v, -180, 180, 'Longitude'),
                ),
              ),
            ],
          ),
          const SizedBox(height: 20),
          if (_errorMessage != null) ...[
            FcBanner(message: _errorMessage!),
            const SizedBox(height: 12),
          ],
          ElevatedButton(
            onPressed: _isSaving ? null : _save,
            child: _isSaving
                ? const SizedBox(
                    height: 20,
                    width: 20,
                    child: CircularProgressIndicator(
                        strokeWidth: 2, color: AppColors.forestDeep),
                  )
                : Text(widget.isEditing ? 'Save changes' : 'Register field'),
          ),
        ],
      ),
    );
  }
}

/// Free-text input with tap-to-fill suggestion chips underneath.
class _SuggestedTextField extends StatelessWidget {
  final TextEditingController controller;
  final String hint;
  final List<String> suggestions;
  final int maxLength;

  const _SuggestedTextField({
    required this.controller,
    required this.hint,
    required this.suggestions,
    required this.maxLength,
  });

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        TextFormField(
          controller: controller,
          maxLength: maxLength,
          textCapitalization: TextCapitalization.sentences,
          decoration: InputDecoration(hintText: hint, counterText: ''),
        ),
        const SizedBox(height: 8),
        Wrap(
          spacing: 6,
          runSpacing: 6,
          children: [
            for (final suggestion in suggestions)
              ActionChip(
                label: Text(suggestion, style: const TextStyle(fontSize: 12)),
                backgroundColor: Colors.white,
                side: const BorderSide(color: AppColors.line),
                visualDensity: VisualDensity.compact,
                onPressed: () => controller.text = suggestion,
              ),
          ],
        ),
      ],
    );
  }
}
