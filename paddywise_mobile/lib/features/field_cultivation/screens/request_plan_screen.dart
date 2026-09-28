import 'dart:async';

import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

import '../../../theme/app_theme.dart';
import '../models/cycle_models.dart';
import '../models/plan_models.dart';
import '../services/field_cultivation_service.dart';
import '../widgets/fc_common.dart';

/// Starting points so the farmer does not face an empty box.
const List<String> _exampleObjectives = [
  'Get a good yield this season without spending more on fertiliser than last time.',
  'Keep water use low — the tank fills late this year.',
  'Protect the crop from brown planthopper, which hit my field last season.',
];

/// What the agent works through while the request is in flight. This is a
/// description, not progress: it advances on a timer and holds on the last
/// line until the real request settles.
const List<String> _progressMessages = [
  'Reading your field and season…',
  'Lining tasks up with your growth stages…',
  'Checking the plan against safety rules…',
  'Almost there — finishing the checks…',
];

/// Ask the Cultivation Planning Agent for a plan. Pops with the created plan.
class RequestPlanScreen extends StatefulWidget {
  final int cycleId;
  final CultivationCycle? cycle;

  const RequestPlanScreen({super.key, required this.cycleId, this.cycle});

  @override
  State<RequestPlanScreen> createState() => _RequestPlanScreenState();
}

class _RequestPlanScreenState extends State<RequestPlanScreen> {
  final _objectiveController = TextEditingController();
  bool _isSubmitting = false;
  int _progressIndex = 0;
  Timer? _progressTimer;
  String? _errorMessage;

  @override
  void dispose() {
    _progressTimer?.cancel();
    _objectiveController.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    final objective = _objectiveController.text.trim();
    if (objective.isEmpty) {
      setState(() => _errorMessage = 'Tell the assistant what you want from this season.');
      return;
    }
    FocusScope.of(context).unfocus();

    setState(() {
      _isSubmitting = true;
      _progressIndex = 0;
      _errorMessage = null;
    });
    _progressTimer = Timer.periodic(const Duration(seconds: 10), (_) {
      if (_progressIndex < _progressMessages.length - 1) {
        setState(() => _progressIndex++);
      }
    });

    try {
      final plan = await FieldCultivationService.requestPlan(widget.cycleId, objective);
      if (mounted) context.pop(plan);
    } catch (e) {
      if (!mounted) return;
      setState(() {
        _errorMessage = e.toString();
        _isSubmitting = false;
      });
    } finally {
      _progressTimer?.cancel();
    }
  }

  @override
  Widget build(BuildContext context) {
    return PopScope(
      canPop: !_isSubmitting,
      child: Scaffold(
        backgroundColor: AppColors.cream,
        appBar: fcFormAppBar('AI cultivation plan'),
        body: SafeArea(child: _isSubmitting ? _buildWaiting() : _buildForm()),
      ),
    );
  }

  Widget _buildWaiting() {
    return Padding(
      padding: const EdgeInsets.all(32),
      child: Center(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            const SizedBox(
              width: 56,
              height: 56,
              child: CircularProgressIndicator(color: AppColors.gold, strokeWidth: 4),
            ),
            const SizedBox(height: 24),
            AnimatedSwitcher(
              duration: const Duration(milliseconds: 300),
              child: Text(
                _progressMessages[_progressIndex],
                key: ValueKey(_progressIndex),
                textAlign: TextAlign.center,
                style: const TextStyle(
                  fontSize: 17,
                  fontWeight: FontWeight.bold,
                  color: AppColors.ink,
                  fontFamily: 'serif',
                ),
              ),
            ),
            const SizedBox(height: 10),
            const Text(
              'This usually takes under a minute. Please keep the app open.',
              textAlign: TextAlign.center,
              style: TextStyle(fontSize: 13, color: AppColors.inkSoft),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildForm() {
    final cycle = widget.cycle;

    return ListView(
      padding: const EdgeInsets.all(16),
      children: [
        if (cycle != null)
          Text(
            '${cycle.fieldName} · ${cycle.title} · ${cycle.varietyName}',
            style: const TextStyle(
              fontSize: 15,
              fontWeight: FontWeight.bold,
              color: AppColors.forest,
              fontFamily: 'serif',
            ),
          ),
        const SizedBox(height: 6),
        const Text(
          'In your own words, what do you want from this season? The planning assistant '
          'uses your field, variety and sowing date to draft a dated plan for each growth '
          'stage. Your agricultural officer reviews it before you follow it.',
          style: TextStyle(fontSize: 13, color: AppColors.inkSoft, height: 1.4),
        ),
        const FcFieldLabel('Your goal for this season'),
        TextField(
          controller: _objectiveController,
          maxLines: 5,
          minLines: 3,
          maxLength: PlanRules.objectiveMaxLength,
          textCapitalization: TextCapitalization.sentences,
          decoration: const InputDecoration(
            hintText: 'e.g. A good yield while keeping fertiliser costs down',
          ),
          onChanged: (_) {
            if (_errorMessage != null) setState(() => _errorMessage = null);
          },
        ),
        const Text('Or start from an example:',
            style: TextStyle(fontSize: 12, color: AppColors.inkSoft)),
        const SizedBox(height: 8),
        for (final example in _exampleObjectives)
          Padding(
            padding: const EdgeInsets.only(bottom: 8),
            child: FcCard(
              padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 10),
              onTap: () => setState(() {
                _objectiveController.text = example;
                _errorMessage = null;
              }),
              child: Row(
                children: [
                  const Icon(Icons.lightbulb_outline, size: 16, color: AppColors.goldDeep),
                  const SizedBox(width: 8),
                  Expanded(
                    child: Text(example,
                        style: const TextStyle(fontSize: 13, color: AppColors.ink)),
                  ),
                ],
              ),
            ),
          ),
        const SizedBox(height: 12),
        if (_errorMessage != null) ...[
          FcBanner(message: _errorMessage!),
          const SizedBox(height: 12),
        ],
        ElevatedButton.icon(
          onPressed: _submit,
          icon: const Icon(Icons.auto_awesome),
          label: const Text('Generate my plan'),
        ),
      ],
    );
  }
}
