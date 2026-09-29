import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import '../../../theme/app_theme.dart';
import '../models/cultivation_cycle_summary.dart';
import '../models/observation.dart';
import '../services/observation_api_service.dart';
import '../widgets/observation_status_badge.dart';

/// Farmer's "My Observations" list — GET /api/observations, newest first (already sorted
/// server-side). Mirrors paddywise-web's ObservationsPage.tsx: pull-to-refresh, a
/// filter-by-cultivation-cycle dropdown, and loading/empty/error states.
class ObservationsListScreen extends StatefulWidget {
  const ObservationsListScreen({super.key});

  @override
  State<ObservationsListScreen> createState() => _ObservationsListScreenState();
}

class _ObservationsListScreenState extends State<ObservationsListScreen> {
  final _service = ObservationApiService();

  List<Observation>? _observations;
  String? _error;

  List<CultivationCycleSummary> _cycles = [];
  int? _cycleFilter; // null = all cycles

  @override
  void initState() {
    super.initState();
    _loadCycles();
    _load();
  }

  Future<void> _loadCycles() async {
    try {
      final cycles = await _service.getCycles();
      if (!mounted) return;
      setState(() => _cycles = cycles);
    } catch (_) {
      // The filter is a convenience, not core to the list — a failure here just means the
      // filter dropdown stays empty; the list itself still loads independently below.
    }
  }

  Future<void> _load() async {
    setState(() {
      _observations = null;
      _error = null;
    });
    try {
      final observations = await _service.getObservations(cultivationCycleId: _cycleFilter);
      if (!mounted) return;
      setState(() => _observations = observations);
    } on ApiException catch (e) {
      if (!mounted) return;
      setState(() => _error = e.message);
    } catch (_) {
      if (!mounted) return;
      setState(() => _error = 'Could not load your reports. Please try again.');
    }
  }

  Future<void> _openNewObservation() async {
    final result = await context.push<Map<String, dynamic>>('/farmer/observations/new');
    if (result == null) return;

    final saved = result['observation'] as Observation;
    final warning = result['warning'] as String?;

    setState(() {
      final current = List<Observation>.from(_observations ?? []);
      current.removeWhere((o) => o.id == saved.id);
      current.insert(0, saved);
      _observations = current;
    });

    if (warning != null && mounted) {
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(warning)));
    }
  }

  Future<void> _openDetail(Observation observation) async {
    await context.push('/farmer/observations/${observation.id}');
    // The detail screen may have triggered analysis or an edit — refresh on return so the
    // list reflects any change rather than showing stale state.
    if (mounted) _load();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.cream,
      floatingActionButton: FloatingActionButton.extended(
        onPressed: _openNewObservation,
        icon: const Icon(Icons.add),
        label: const Text('Report a problem'),
      ),
      body: SafeArea(
        child: RefreshIndicator(
          onRefresh: _load,
          child: CustomScrollView(
            slivers: [
              SliverToBoxAdapter(child: _buildHeader(context)),
              if (_observations == null && _error == null)
                const SliverFillRemaining(
                  child: Center(child: CircularProgressIndicator()),
                )
              else if (_error != null)
                SliverFillRemaining(
                  child: _ErrorState(message: _error!, onRetry: _load),
                )
              else if (_observations!.isEmpty)
                SliverFillRemaining(
                  child: _EmptyState(onReport: _openNewObservation),
                )
              else
                SliverPadding(
                  padding: const EdgeInsets.fromLTRB(16, 0, 16, 96),
                  sliver: SliverList.builder(
                    itemCount: _observations!.length,
                    itemBuilder: (context, index) {
                      final observation = _observations![index];
                      return Padding(
                        padding: const EdgeInsets.only(bottom: 12),
                        child: _ObservationCard(
                          observation: observation,
                          onTap: () => _openDetail(observation),
                        ),
                      );
                    },
                  ),
                ),
            ],
          ),
        ),
      ),
    );
  }

  Widget _buildHeader(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.fromLTRB(16, 16, 16, 8),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const Text(
            'PEST & DISEASE MONITORING',
            style: TextStyle(
              fontSize: 11,
              fontWeight: FontWeight.bold,
              letterSpacing: 1.2,
              color: AppColors.shoot,
            ),
          ),
          const SizedBox(height: 4),
          const Text(
            'My Reports',
            style: TextStyle(
              fontSize: 22,
              fontWeight: FontWeight.bold,
              color: AppColors.ink,
              fontFamily: 'serif',
            ),
          ),
          const SizedBox(height: 6),
          const Text(
            'Report symptoms you notice on a cultivation cycle and ask the diagnosis agent '
            'for a read — every candidate it names is checked against the department\'s own '
            'pest and disease reference data before an officer signs off.',
            style: TextStyle(fontSize: 13, color: AppColors.inkSoft),
          ),
          if (_cycles.isNotEmpty) ...[
            const SizedBox(height: 12),
            DropdownButtonFormField<int?>(
              initialValue: _cycleFilter,
              decoration: const InputDecoration(labelText: 'Filter by cultivation cycle'),
              items: [
                const DropdownMenuItem<int?>(value: null, child: Text('All cycles')),
                ..._cycles.map((cycle) => DropdownMenuItem<int?>(
                      value: cycle.id,
                      child: Text(cycle.label, overflow: TextOverflow.ellipsis),
                    )),
              ],
              onChanged: (value) {
                setState(() => _cycleFilter = value);
                _load();
              },
            ),
          ],
        ],
      ),
    );
  }
}

class _ObservationCard extends StatelessWidget {
  final Observation observation;
  final VoidCallback onTap;

  const _ObservationCard({required this.observation, required this.onTap});

  @override
  Widget build(BuildContext context) {
    return Material(
      color: Colors.white,
      borderRadius: BorderRadius.circular(12),
      child: InkWell(
        onTap: onTap,
        borderRadius: BorderRadius.circular(12),
        child: Container(
          padding: const EdgeInsets.all(14),
          decoration: BoxDecoration(
            borderRadius: BorderRadius.circular(12),
            border: Border.all(color: AppColors.line),
          ),
          child: Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              if (observation.imageUrl != null) ...[
                ClipRRect(
                  borderRadius: BorderRadius.circular(8),
                  child: Image.network(
                    observation.imageUrl!,
                    width: 56,
                    height: 56,
                    fit: BoxFit.cover,
                    errorBuilder: (context, error, stackTrace) => Container(
                      width: 56,
                      height: 56,
                      color: AppColors.creamDeep,
                      child: const Icon(Icons.image_not_supported_outlined,
                          color: AppColors.inkSoft, size: 20),
                    ),
                  ),
                ),
                const SizedBox(width: 12),
              ],
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      '${observationTypeLabels[observation.observationType] ?? observation.observationType} · ${observation.fieldName}',
                      style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 15),
                    ),
                    const SizedBox(height: 2),
                    Text(
                      '${observation.cropStage} stage · ${severityLabels[observation.severity] ?? observation.severity} severity',
                      style: const TextStyle(fontSize: 12, color: AppColors.inkSoft),
                    ),
                    const SizedBox(height: 8),
                    ObservationStatusBadge(observation: observation),
                  ],
                ),
              ),
              const Icon(Icons.chevron_right_rounded, color: AppColors.inkSoft),
            ],
          ),
        ),
      ),
    );
  }
}

class _EmptyState extends StatelessWidget {
  final VoidCallback onReport;

  const _EmptyState({required this.onReport});

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(24),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            const Icon(Icons.bug_report_outlined, size: 40, color: AppColors.inkSoft),
            const SizedBox(height: 12),
            const Text('No reports yet',
                style: TextStyle(fontWeight: FontWeight.bold, fontSize: 16)),
            const SizedBox(height: 6),
            const Text(
              'Noticed something odd on a crop? Report it and ask the agent for a diagnosis.',
              textAlign: TextAlign.center,
              style: TextStyle(color: AppColors.inkSoft),
            ),
            const SizedBox(height: 16),
            ElevatedButton.icon(
              onPressed: onReport,
              icon: const Icon(Icons.add),
              label: const Text('Report your first problem'),
            ),
          ],
        ),
      ),
    );
  }
}

class _ErrorState extends StatelessWidget {
  final String message;
  final VoidCallback onRetry;

  const _ErrorState({required this.message, required this.onRetry});

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(24),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            const Icon(Icons.error_outline, size: 36, color: AppColors.errorText),
            const SizedBox(height: 12),
            Text(message, textAlign: TextAlign.center),
            const SizedBox(height: 16),
            OutlinedButton(onPressed: onRetry, child: const Text('Try again')),
          ],
        ),
      ),
    );
  }
}
