import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

import '../../../theme/app_theme.dart';
import '../models/cycle_models.dart';
import '../models/field_models.dart';
import '../services/field_cultivation_service.dart';
import '../widgets/fc_common.dart';

/// Start a cultivation cycle on a field. Pops with the created cycle.
class StartCycleScreen extends StatefulWidget {
  final int fieldId;
  final Field? field;

  const StartCycleScreen({super.key, required this.fieldId, this.field});

  @override
  State<StartCycleScreen> createState() => _StartCycleScreenState();
}

/// Maha runs roughly September–March, Yala April–August.
Season _seasonFor(DateTime date) =>
    (date.month >= 4 && date.month <= 8) ? Season.yala : Season.maha;

/// A Maha season sown in Jan–Mar belongs to the Maha that began the year before.
int _seasonYearFor(DateTime date, Season season) =>
    season == Season.maha && date.month <= 3 ? date.year - 1 : date.year;

class _StartCycleScreenState extends State<StartCycleScreen> {
  final _formKey = GlobalKey<FormState>();
  final _notesController = TextEditingController();

  List<Variety>? _varieties;
  int? _varietyId;
  late DateTime _sowingDate;
  late Season _season;
  late int _year;
  CultivationMethod _method = CultivationMethod.transplanting;

  bool _isSaving = false;
  String? _loadError;
  String? _errorMessage;

  @override
  void initState() {
    super.initState();
    _sowingDate = today;
    _season = _seasonFor(_sowingDate);
    _year = _seasonYearFor(_sowingDate, _season);
    _loadVarieties();
  }

  @override
  void dispose() {
    _notesController.dispose();
    super.dispose();
  }

  Future<void> _loadVarieties() async {
    setState(() => _loadError = null);
    try {
      final varieties = await FieldCultivationService.getVarieties();
      if (!mounted) return;
      setState(() => _varieties = varieties..sort((a, b) => a.name.compareTo(b.name)));
    } catch (e) {
      if (!mounted) return;
      setState(() => _loadError = e.toString());
    }
  }

  Variety? get _selectedVariety =>
      _varieties?.where((v) => v.id == _varietyId).firstOrNull;

  Future<void> _pickSowingDate() async {
    final picked = await showDatePicker(
      context: context,
      initialDate: _sowingDate,
      firstDate: today.subtract(const Duration(days: CycleRules.maxBackdateDays)),
      lastDate: today.add(const Duration(days: CycleRules.maxLookaheadDays)),
      helpText: 'Sowing date',
    );
    if (picked == null) return;
    setState(() {
      _sowingDate = picked;
      _season = _seasonFor(picked);
      _year = _seasonYearFor(picked, _season);
    });
  }

  Future<void> _save() async {
    FocusScope.of(context).unfocus();
    if (!_formKey.currentState!.validate()) return;

    setState(() {
      _isSaving = true;
      _errorMessage = null;
    });

    final notes = _notesController.text.trim();
    try {
      final cycle = await FieldCultivationService.startCultivation(
        widget.fieldId,
        StartCycleRequest(
          varietyId: _varietyId!,
          season: _season,
          year: _year,
          method: _method,
          sowingDate: _sowingDate,
          notes: notes.isEmpty ? null : notes,
        ),
      );
      if (mounted) context.pop(cycle);
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
      appBar: fcFormAppBar('Start cultivation'),
      body: SafeArea(child: _buildBody()),
    );
  }

  Widget _buildBody() {
    if (_loadError != null) {
      return FcStateView.error(message: _loadError!, onAction: _loadVarieties);
    }
    if (_varieties == null) return const FcLoading();

    final variety = _selectedVariety;
    final isFuture = _sowingDate.isAfter(today);

    return Form(
      key: _formKey,
      child: ListView(
        padding: const EdgeInsets.all(16),
        children: [
          if (widget.field != null)
            Text(
              '${widget.field!.name} · ${widget.field!.areaLabel}',
              style: const TextStyle(
                fontSize: 15,
                fontWeight: FontWeight.bold,
                color: AppColors.forest,
                fontFamily: 'serif',
              ),
            ),
          const SizedBox(height: 4),
          const Text(
            'Your growth-stage timeline is worked out from the sowing date and the '
            'variety\'s age.',
            style: TextStyle(fontSize: 13, color: AppColors.inkSoft, height: 1.4),
          ),
          const FcFieldLabel('Paddy variety'),
          DropdownButtonFormField<int>(
            initialValue: _varietyId,
            isExpanded: true,
            decoration: const InputDecoration(hintText: 'Choose a variety'),
            items: [
              for (final v in _varieties!)
                DropdownMenuItem(
                  value: v.id,
                  child: Text('${v.name} · ${v.durationDays} days'),
                ),
            ],
            onChanged: (value) => setState(() => _varietyId = value),
            validator: (value) => value == null ? 'Choose the variety you are sowing.' : null,
          ),
          if (variety != null && (variety.notes?.isNotEmpty ?? false))
            Padding(
              padding: const EdgeInsets.only(top: 6),
              child: Text(
                '${variety.ageGroup.isEmpty ? '' : '${variety.ageGroup} · '}${variety.notes}',
                style: const TextStyle(fontSize: 12, color: AppColors.inkSoft),
              ),
            ),
          const FcFieldLabel('Sowing date'),
          FcCard(
            onTap: _pickSowingDate,
            padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 14),
            child: Row(
              children: [
                const Icon(Icons.event_outlined, size: 20, color: AppColors.shoot),
                const SizedBox(width: 10),
                Expanded(
                  child: Text(formatDate(_sowingDate),
                      style: const TextStyle(fontSize: 15, color: AppColors.ink)),
                ),
                const Text('Change',
                    style: TextStyle(
                        fontSize: 13,
                        fontWeight: FontWeight.w600,
                        color: AppColors.goldDeep)),
              ],
            ),
          ),
          if (variety != null)
            Padding(
              padding: const EdgeInsets.only(top: 6),
              child: Text(
                'Expected harvest around '
                '${formatDate(_sowingDate.add(Duration(days: variety.durationDays)))}.',
                style: const TextStyle(fontSize: 12, color: AppColors.forest),
              ),
            ),
          if (isFuture)
            const Padding(
              padding: EdgeInsets.only(top: 6),
              child: Text(
                'Sowing is in the future, so the season starts as Planned and becomes '
                'Active once your officer approves a plan.',
                style: TextStyle(fontSize: 12, color: AppColors.inkSoft),
              ),
            ),
          const FcFieldLabel('Season'),
          Row(
            children: [
              Expanded(
                child: SegmentedButton<Season>(
                  segments: [
                    for (final s in Season.values)
                      ButtonSegment(value: s, label: Text(s.label)),
                  ],
                  selected: {_season},
                  onSelectionChanged: (value) => setState(() => _season = value.first),
                  style: _segmentStyle,
                ),
              ),
              const SizedBox(width: 12),
              SizedBox(
                width: 110,
                child: DropdownButtonFormField<int>(
                  key: ValueKey(_year),
                  initialValue: _year,
                  items: [
                    for (var y = today.year - 1; y <= today.year + 1; y++)
                      DropdownMenuItem(value: y, child: Text('$y')),
                  ],
                  onChanged: (value) => setState(() => _year = value ?? _year),
                ),
              ),
            ],
          ),
          const FcFieldLabel('Cultivation method'),
          SegmentedButton<CultivationMethod>(
            segments: [
              for (final m in CultivationMethod.values)
                ButtonSegment(value: m, label: Text(m.label, textAlign: TextAlign.center)),
            ],
            selected: {_method},
            onSelectionChanged: (value) => setState(() => _method = value.first),
            showSelectedIcon: false,
            style: _segmentStyle,
          ),
          const FcFieldLabel('Notes (optional)'),
          TextFormField(
            controller: _notesController,
            maxLines: 3,
            maxLength: CycleRules.notesMaxLength,
            textCapitalization: TextCapitalization.sentences,
            decoration: const InputDecoration(
              hintText: 'Anything your officer should know, e.g. seed source',
            ),
          ),
          const SizedBox(height: 12),
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
                : const Text('Start season'),
          ),
        ],
      ),
    );
  }

  static final ButtonStyle _segmentStyle = SegmentedButton.styleFrom(
    backgroundColor: Colors.white,
    selectedBackgroundColor: AppColors.shootLight,
    selectedForegroundColor: AppColors.forestDeep,
    foregroundColor: AppColors.inkSoft,
    side: const BorderSide(color: AppColors.line),
  );
}
