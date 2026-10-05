import 'dart:io';
import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:image_picker/image_picker.dart';
import '../../../theme/app_theme.dart';
import '../models/cultivation_cycle_summary.dart';
import '../models/observation.dart';
import '../services/observation_api_service.dart';

/// Maps a picked file's extension to the content-type the backend accepts —
/// ObservationService.ImageExtensionsByContentType, inverted. Anything else is rejected
/// client-side before ever reaching the network, mirroring the server's own check
/// (`file.ContentType` must start with "image/").
const Map<String, String> _imageContentTypesByExtension = {
  'jpg': 'image/jpeg',
  'jpeg': 'image/jpeg',
  'png': 'image/png',
  'webp': 'image/webp',
  'gif': 'image/gif',
};

/// 10MB — ObservationService.MaxPhotoBytes.
const int _maxPhotoBytes = 10 * 1024 * 1024;

/// Create/edit form for a pest or disease observation. Pass [editing] to correct an
/// existing observation in place (only while it has no diagnosis yet, matching the
/// backend's edit lock) — omit it to submit a new one. Mirrors
/// paddywise-web/src/features/pest-disease/components/ObservationForm.tsx.
class NewObservationScreen extends StatefulWidget {
  final Observation? editing;

  /// Overridable for widget tests (a mocked service instead of the real network); the app
  /// itself never passes this, always getting the default real [ObservationApiService].
  final ObservationApiService? service;

  const NewObservationScreen({super.key, this.editing, this.service});

  @override
  State<NewObservationScreen> createState() => _NewObservationScreenState();
}

class _NewObservationScreenState extends State<NewObservationScreen> {
  final _formKey = GlobalKey<FormState>();
  final _symptomsController = TextEditingController();
  late final ObservationApiService _service = widget.service ?? ObservationApiService();
  final _imagePicker = ImagePicker();

  bool get _isEditing => widget.editing != null;

  int? _selectedCycleId;
  String _observationType = 'Unknown';
  String _severity = 'Low';

  List<CultivationCycleSummary> _cycles = [];
  bool _isLoadingCycles = false;
  String? _cyclesError;

  XFile? _photoFile;
  String? _photoError;

  bool _isSubmitting = false;
  String? _submitError;

  @override
  void initState() {
    super.initState();
    final editing = widget.editing;
    if (editing != null) {
      _selectedCycleId = editing.cultivationCycleId;
      _observationType = editing.observationType;
      _severity = editing.severity;
      _symptomsController.text = editing.symptoms;
    } else {
      _loadCycles();
    }
  }

  @override
  void dispose() {
    _symptomsController.dispose();
    super.dispose();
  }

  Future<void> _loadCycles() async {
    setState(() {
      _isLoadingCycles = true;
      _cyclesError = null;
    });
    try {
      final cycles = await _service.getCycles();
      if (!mounted) return;
      setState(() {
        _cycles = cycles;
        _isLoadingCycles = false;
      });
    } on ApiException catch (e) {
      if (!mounted) return;
      setState(() {
        _cyclesError = e.message;
        _isLoadingCycles = false;
      });
    } catch (_) {
      if (!mounted) return;
      setState(() {
        _cyclesError = 'Could not load your cultivation cycles. Please try again.';
        _isLoadingCycles = false;
      });
    }
  }

  Future<void> _pickPhoto(ImageSource source) async {
    // Phone camera photos are often 5-10MB; downscaling on pick keeps uploads fast on
    // mobile data and comfortably under the backend's 10MB cap. The backend downscales to
    // 1024px on its own side regardless, so this doesn't need to match that exactly.
    final picked = await _imagePicker.pickImage(
      source: source,
      maxWidth: 1600,
      imageQuality: 85,
    );
    if (picked == null) return;

    final extension = picked.path.split('.').last.toLowerCase();
    if (!_imageContentTypesByExtension.containsKey(extension)) {
      setState(() {
        _photoFile = null;
        _photoError = 'Unsupported photo format. Use JPG, PNG, WEBP or GIF.';
      });
      return;
    }

    final size = await picked.length();
    if (size > _maxPhotoBytes) {
      setState(() {
        _photoFile = null;
        _photoError = 'Photo cannot exceed 10MB.';
      });
      return;
    }

    setState(() {
      _photoFile = picked;
      _photoError = null;
    });
  }

  void _removePhoto() {
    setState(() {
      _photoFile = null;
      _photoError = null;
    });
  }

  Future<void> _submit() async {
    setState(() => _submitError = null);

    if (!_isEditing && _selectedCycleId == null) {
      setState(() => _cyclesError ??= 'Select the affected cultivation cycle.');
    }
    final formValid = _formKey.currentState?.validate() ?? false;
    if (!formValid || (!_isEditing && _selectedCycleId == null)) return;

    setState(() => _isSubmitting = true);

    Observation saved;
    try {
      if (_isEditing) {
        saved = await _service.updateObservation(
          widget.editing!.id,
          UpdateObservationRequest(
            observationType: _observationType,
            symptoms: _symptomsController.text.trim(),
            severity: _severity,
          ),
        );
      } else {
        saved = await _service.createObservation(
          CreateObservationRequest(
            cultivationCycleId: _selectedCycleId!,
            observationType: _observationType,
            symptoms: _symptomsController.text.trim(),
            severity: _severity,
          ),
        );
      }
    } on ApiException catch (e) {
      if (!mounted) return;
      setState(() {
        _submitError = e.message;
        _isSubmitting = false;
      });
      return;
    } catch (_) {
      if (!mounted) return;
      setState(() {
        _submitError = 'Could not save this report. Please try again.';
        _isSubmitting = false;
      });
      return;
    }

    // The report above already saved successfully — a photo failure here is a distinct,
    // narrower problem than "could not save this report", so it's surfaced as a warning on
    // the saved observation rather than as submitError, matching the fix already applied on
    // web's ObservationForm.tsx (a failure here must not look like the report was lost).
    String? photoWarning;
    if (_photoFile != null) {
      try {
        final extension = _photoFile!.path.split('.').last.toLowerCase();
        saved = await _service.uploadPhoto(
          observationId: saved.id,
          filePath: _photoFile!.path,
          contentType: _imageContentTypesByExtension[extension]!,
        );
      } on ApiException catch (e) {
        photoWarning = 'The report was saved, but the photo could not be uploaded: ${e.message}';
      } catch (_) {
        photoWarning =
            'The report was saved, but the photo could not be uploaded. Please try again.';
      }
    }

    if (!mounted) return;
    setState(() => _isSubmitting = false);
    context.pop({'observation': saved, 'warning': photoWarning});
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.cream,
      appBar: AppBar(
        title: Text(_isEditing ? 'Edit report' : 'Report a pest or disease problem'),
      ),
      body: SafeArea(
        child: Form(
          key: _formKey,
          child: ListView(
            padding: const EdgeInsets.all(16),
            children: [
              if (_submitError != null) _ErrorBanner(message: _submitError!),

              if (!_isEditing) ...[
                Text('Cultivation cycle', style: _labelStyle),
                const SizedBox(height: 6),
                if (_isLoadingCycles)
                  const Padding(
                    padding: EdgeInsets.symmetric(vertical: 8),
                    child: LinearProgressIndicator(),
                  )
                else
                  DropdownButtonFormField<int>(
                    key: const ValueKey('cycle-field'),
                    initialValue: _selectedCycleId,
                    hint: const Text('Select the affected cycle'),
                    items: _cycles
                        .map((cycle) => DropdownMenuItem(
                              value: cycle.id,
                              child: Text(cycle.label, overflow: TextOverflow.ellipsis),
                            ))
                        .toList(),
                    onChanged: (value) => setState(() {
                      _selectedCycleId = value;
                      _cyclesError = null;
                    }),
                  ),
                if (_cyclesError != null)
                  Padding(
                    padding: const EdgeInsets.only(top: 4),
                    child: Text(_cyclesError!,
                        style: const TextStyle(color: AppColors.errorText, fontSize: 12)),
                  ),
                const SizedBox(height: 16),
              ],

              Text('What are you seeing?', style: _labelStyle),
              const SizedBox(height: 6),
              DropdownButtonFormField<String>(
                key: const ValueKey('observation-type-field'),
                initialValue: _observationType,
                items: observationTypes
                    .map((type) => DropdownMenuItem(
                          value: type,
                          child: Text(observationTypeLabels[type] ?? type),
                        ))
                    .toList(),
                onChanged: (value) => setState(() => _observationType = value ?? 'Unknown'),
              ),
              const SizedBox(height: 16),

              Text('Severity', style: _labelStyle),
              const SizedBox(height: 6),
              DropdownButtonFormField<String>(
                key: const ValueKey('severity-field'),
                initialValue: _severity,
                items: observationSeverities
                    .map((severity) => DropdownMenuItem(
                          value: severity,
                          child: Text(severityLabels[severity] ?? severity),
                        ))
                    .toList(),
                onChanged: (value) => setState(() => _severity = value ?? 'Low'),
              ),
              const SizedBox(height: 16),

              Text('Describe what you see', style: _labelStyle),
              const SizedBox(height: 6),
              TextFormField(
                key: const ValueKey('symptoms-field'),
                controller: _symptomsController,
                maxLength: symptomsMaxLength,
                maxLines: 4,
                decoration: const InputDecoration(
                  hintText: 'Yellowing leaf tips, stunted growth, curling leaves…',
                ),
                validator: _validateSymptoms,
              ),
              const SizedBox(height: 8),

              Text('Photo (optional)', style: _labelStyle),
              const SizedBox(height: 6),
              Row(
                children: [
                  Expanded(
                    child: OutlinedButton.icon(
                      onPressed: _isSubmitting ? null : () => _pickPhoto(ImageSource.camera),
                      icon: const Icon(Icons.photo_camera_outlined, size: 18),
                      label: const Text('Take photo'),
                    ),
                  ),
                  const SizedBox(width: 12),
                  Expanded(
                    child: OutlinedButton.icon(
                      onPressed: _isSubmitting ? null : () => _pickPhoto(ImageSource.gallery),
                      icon: const Icon(Icons.photo_library_outlined, size: 18),
                      label: const Text('Choose from gallery'),
                    ),
                  ),
                ],
              ),
              if (_photoError != null)
                Padding(
                  padding: const EdgeInsets.only(top: 6),
                  child: Text(_photoError!,
                      style: const TextStyle(color: AppColors.errorText, fontSize: 12)),
                ),
              if (_photoFile != null) ...[
                const SizedBox(height: 12),
                Stack(
                  alignment: Alignment.topRight,
                  children: [
                    ClipRRect(
                      borderRadius: BorderRadius.circular(8),
                      child: Image.file(
                        File(_photoFile!.path),
                        height: 160,
                        width: double.infinity,
                        fit: BoxFit.cover,
                      ),
                    ),
                    IconButton(
                      onPressed: _isSubmitting ? null : _removePhoto,
                      icon: const Icon(Icons.close_rounded),
                      style: IconButton.styleFrom(
                        backgroundColor: Colors.black.withValues(alpha: 0.5),
                        foregroundColor: Colors.white,
                      ),
                      tooltip: 'Remove photo',
                    ),
                  ],
                ),
              ],
              const SizedBox(height: 24),

              Row(
                key: const ValueKey('form-actions'),
                children: [
                  Expanded(
                    child: OutlinedButton(
                      onPressed: _isSubmitting ? null : () => context.pop(),
                      child: const Text('Cancel'),
                    ),
                  ),
                  const SizedBox(width: 12),
                  Expanded(
                    child: ElevatedButton(
                      onPressed: _isSubmitting ? null : _submit,
                      child: _isSubmitting
                          ? const SizedBox(
                              height: 18,
                              width: 18,
                              child: CircularProgressIndicator(strokeWidth: 2),
                            )
                          : Text(_isEditing ? 'Save changes' : 'Submit report'),
                    ),
                  ),
                ],
              ),
            ],
          ),
        ),
      ),
    );
  }

  static String? _validateSymptoms(String? value) {
    final trimmed = value?.trim() ?? '';
    if (trimmed.isEmpty) return 'Describe what you see.';
    if (trimmed.length > symptomsMaxLength) {
      return 'Symptoms cannot exceed $symptomsMaxLength characters.';
    }
    return null;
  }

  static const _labelStyle = TextStyle(
    fontSize: 13,
    fontWeight: FontWeight.w600,
    color: AppColors.ink,
  );
}

class _ErrorBanner extends StatelessWidget {
  final String message;

  const _ErrorBanner({required this.message});

  @override
  Widget build(BuildContext context) {
    return Container(
      margin: const EdgeInsets.only(bottom: 16),
      padding: const EdgeInsets.all(12),
      decoration: BoxDecoration(
        color: AppColors.errorBg,
        borderRadius: BorderRadius.circular(8),
      ),
      child: Row(
        children: [
          const Icon(Icons.error_outline, color: AppColors.errorText, size: 18),
          const SizedBox(width: 8),
          Expanded(
            child: Text(message, style: const TextStyle(color: AppColors.errorText)),
          ),
        ],
      ),
    );
  }
}
