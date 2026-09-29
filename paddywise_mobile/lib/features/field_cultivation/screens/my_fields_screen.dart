import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

import '../../../theme/app_theme.dart';
import '../models/cycle_models.dart';
import '../models/field_models.dart';
import '../services/field_cultivation_service.dart';
import '../widgets/fc_common.dart';
import '../widgets/status_badges.dart';

/// The farmer's registered paddy fields, each with its current season.
class MyFieldsScreen extends StatefulWidget {
  const MyFieldsScreen({super.key});

  @override
  State<MyFieldsScreen> createState() => _MyFieldsScreenState();
}

class _MyFieldsScreenState extends State<MyFieldsScreen> {
  List<Field>? _fields;
  Map<int, CultivationCycle> _openCycleByField = {};
  String? _errorMessage;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    try {
      final results = await Future.wait([
        FieldCultivationService.getMyFields(),
        FieldCultivationService.getMyCycles(),
      ]);
      final fields = results[0] as List<Field>;
      final cycles = results[1] as List<CultivationCycle>;
      if (!mounted) return;
      setState(() {
        _fields = fields..sort((a, b) => a.name.compareTo(b.name));
        _openCycleByField = {
          for (final c in cycles.where((c) => c.status.isOpen)) c.fieldId: c,
        };
        _errorMessage = null;
      });
    } catch (e) {
      if (!mounted) return;
      setState(() => _errorMessage = e.toString());
    }
  }

  Future<void> _addField() async {
    final created = await context.push<Field>('/fields/new');
    if (created != null && mounted) {
      showFcSnack(context, '${created.name} registered.');
      _load();
    }
  }

  @override
  Widget build(BuildContext context) {
    final fields = _fields;

    return Scaffold(
      backgroundColor: AppColors.cream,
      floatingActionButton: fields != null && fields.isNotEmpty
          ? FloatingActionButton.extended(
              onPressed: _addField,
              backgroundColor: AppColors.gold,
              foregroundColor: AppColors.forestDeep,
              icon: const Icon(Icons.add),
              label: const Text('Add field'),
            )
          : null,
      body: RefreshIndicator(
        color: AppColors.gold,
        onRefresh: _load,
        child: ListView(
          padding: const EdgeInsets.fromLTRB(16, 16, 16, 96),
          children: [
            const FcPageHeader(
              eyebrow: 'Field & Cultivation',
              title: 'My Fields',
              subtitle: 'Every plot you have registered. Tap one to start or follow a season.',
            ),
            const SizedBox(height: 16),
            if (fields == null && _errorMessage == null)
              const FcLoading(message: 'Loading your fields…')
            else if (fields == null)
              FcStateView.error(
                message: _errorMessage!,
                onAction: () {
                  setState(() => _errorMessage = null);
                  _load();
                },
              )
            else if (fields.isEmpty)
              FcStateView(
                icon: Icons.grass_outlined,
                title: 'No fields yet',
                message: 'Register your first paddy field to start a cultivation cycle '
                    'and get an AI cultivation plan.',
                actionLabel: 'Add your first field',
                onAction: _addField,
              )
            else
              for (final field in fields)
                Padding(
                  padding: const EdgeInsets.only(bottom: 12),
                  child: _FieldCard(
                    field: field,
                    openCycle: _openCycleByField[field.id],
                    onTap: () async {
                      await context.push('/fields/${field.id}');
                      if (mounted) _load();
                    },
                  ),
                ),
          ],
        ),
      ),
    );
  }
}

class _FieldCard extends StatelessWidget {
  final Field field;
  final CultivationCycle? openCycle;
  final VoidCallback onTap;

  const _FieldCard({required this.field, required this.openCycle, required this.onTap});

  @override
  Widget build(BuildContext context) {
    final cycle = openCycle;

    return FcCard(
      onTap: onTap,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Expanded(
                child: Text(
                  field.name,
                  style: const TextStyle(
                    fontSize: 16,
                    fontWeight: FontWeight.bold,
                    color: AppColors.ink,
                    fontFamily: 'serif',
                  ),
                ),
              ),
              Text(field.areaLabel,
                  style: const TextStyle(
                      fontSize: 13, fontWeight: FontWeight.w600, color: AppColors.forest)),
            ],
          ),
          const SizedBox(height: 6),
          Row(
            children: [
              const Icon(Icons.place_outlined, size: 14, color: AppColors.inkSoft),
              const SizedBox(width: 4),
              Expanded(
                child: Text(
                  [
                    if (field.divisionName.isNotEmpty) field.divisionName,
                    if (field.irrigationType.isNotEmpty) field.irrigationType,
                  ].join(' · '),
                  style: const TextStyle(fontSize: 12, color: AppColors.inkSoft),
                ),
              ),
            ],
          ),
          const Divider(height: 22, color: AppColors.line),
          if (cycle == null)
            const Row(
              children: [
                Icon(Icons.add_circle_outline, size: 16, color: AppColors.goldDeep),
                SizedBox(width: 6),
                Text('No season running · tap to start one',
                    style: TextStyle(fontSize: 13, color: AppColors.goldDeep)),
              ],
            )
          else
            Row(
              children: [
                const Icon(Icons.eco_outlined, size: 16, color: AppColors.shoot),
                const SizedBox(width: 6),
                Expanded(
                  child: Text(
                    '${cycle.title} · ${cycle.varietyName} · ${cycle.currentStage.label}',
                    style: const TextStyle(fontSize: 13, color: AppColors.ink),
                  ),
                ),
                CycleStatusBadge(cycle.status),
              ],
            ),
        ],
      ),
    );
  }
}
