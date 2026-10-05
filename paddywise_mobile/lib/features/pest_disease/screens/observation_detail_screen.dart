import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import '../../../theme/app_theme.dart';
import '../models/observation.dart';
import '../models/pest_disease_report.dart';
import '../services/observation_api_service.dart';

/// One observation's full detail: symptoms, photo, crop stage, severity, and the diagnosis
/// results — including the "Ask the diagnosis agent" flow, which can take up to ~240s
/// server-side. Mirrors the detail portion of paddywise-web's ObservationsPage.tsx.
class ObservationDetailScreen extends StatefulWidget {
  final int observationId;

  const ObservationDetailScreen({super.key, required this.observationId});

  @override
  State<ObservationDetailScreen> createState() => _ObservationDetailScreenState();
}

class _ObservationDetailScreenState extends State<ObservationDetailScreen> {
  final _service = ObservationApiService();

  Observation? _observation;
  String? _loadError;

  bool _isAnalyzing = false;
  String? _analysisError;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    setState(() {
      _observation = null;
      _loadError = null;
    });
    try {
      final observation = await _service.getObservationById(widget.observationId);
      if (!mounted) return;
      setState(() => _observation = observation);
    } on ApiException catch (e) {
      if (!mounted) return;
      setState(() => _loadError = e.message);
    } catch (_) {
      if (!mounted) return;
      setState(() => _loadError = 'Could not load this report. Please try again.');
    }
  }

  Future<void> _requestAnalysis() async {
    // Guards re-entrancy from a double tap arriving before the button is rebuilt away —
    // _isAnalyzing gates the button's onPressed too, but that gate only takes effect on the
    // next frame, not synchronously within the same event.
    if (_isAnalyzing) return;

    setState(() {
      _isAnalyzing = true;
      _analysisError = null;
    });
    try {
      final updated = await _service.requestAnalysis(widget.observationId);
      if (!mounted) return;
      setState(() {
        _observation = updated;
        _isAnalyzing = false;
      });
    } on ApiException catch (e) {
      if (!mounted) return;
      setState(() {
        _analysisError = e.message;
        _isAnalyzing = false;
      });
    } catch (_) {
      if (!mounted) return;
      setState(() {
        _analysisError = 'The diagnosis assistant could not be reached. Please try again.';
        _isAnalyzing = false;
      });
    }
  }

  Future<void> _edit() async {
    final observation = _observation;
    if (observation == null) return;

    final result = await context.push<Map<String, dynamic>>(
      '/farmer/observations/new',
      extra: observation,
    );
    if (result == null) return;

    final saved = result['observation'] as Observation;
    final warning = result['warning'] as String?;
    setState(() => _observation = saved);

    if (warning != null && mounted) {
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(warning)));
    }
  }

  @override
  Widget build(BuildContext context) {
    final observation = _observation;

    return PopScope(
      // Blocks the AppBar back arrow, the system back gesture/button, and go_router's own
      // pop — a farmer leaving mid-analysis and reopening this screen would otherwise be
      // able to tap "Ask the diagnosis agent" a second time while the first run (including
      // its poll fallback) is still in flight server-side.
      canPop: !_isAnalyzing,
      onPopInvokedWithResult: (didPop, result) {
        if (didPop) return;
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(
            content: Text('Please wait for the diagnosis agent to finish before leaving.'),
          ),
        );
      },
      child: Scaffold(
        backgroundColor: AppColors.cream,
        appBar: AppBar(
          title: const Text('Report detail'),
          actions: [
            if (observation != null && observation.reports.isEmpty)
              IconButton(
                onPressed: _isAnalyzing ? null : _edit,
                icon: const Icon(Icons.edit_outlined),
                tooltip: 'Edit report',
              ),
          ],
        ),
        body: SafeArea(
          child: Builder(builder: (context) {
            if (_loadError != null) {
              return Center(
                child: Padding(
                  padding: const EdgeInsets.all(24),
                  child: Column(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      const Icon(Icons.error_outline, size: 36, color: AppColors.errorText),
                      const SizedBox(height: 12),
                      Text(_loadError!, textAlign: TextAlign.center),
                      const SizedBox(height: 16),
                      OutlinedButton(onPressed: _load, child: const Text('Try again')),
                    ],
                  ),
                ),
              );
            }

            if (observation == null) {
              return const Center(child: CircularProgressIndicator());
            }

            if (_isAnalyzing) {
              return const _AnalyzingState();
            }

            return _DetailBody(
              observation: observation,
              analysisError: _analysisError,
              onRequestAnalysis: _requestAnalysis,
            );
          }),
        ),
      ),
    );
  }
}

class _AnalyzingState extends StatelessWidget {
  const _AnalyzingState();

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(32),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            const CircularProgressIndicator(),
            const SizedBox(height: 20),
            const Text(
              'Analysing your crop…',
              style: TextStyle(fontWeight: FontWeight.bold, fontSize: 16),
            ),
            const SizedBox(height: 8),
            const Text(
              'The diagnosis agent is checking your symptoms against the department\'s pest '
              'and disease reference data. This can take a few minutes — please keep this '
              'screen open.',
              textAlign: TextAlign.center,
              style: TextStyle(color: AppColors.inkSoft),
            ),
          ],
        ),
      ),
    );
  }
}

class _DetailBody extends StatelessWidget {
  final Observation observation;
  final String? analysisError;
  final VoidCallback onRequestAnalysis;

  const _DetailBody({
    required this.observation,
    required this.analysisError,
    required this.onRequestAnalysis,
  });

  @override
  Widget build(BuildContext context) {
    return ListView(
      padding: const EdgeInsets.all(16),
      children: [
        Text(
          '${observationTypeLabels[observation.observationType] ?? observation.observationType} · ${observation.fieldName}',
          style: const TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
        ),
        const SizedBox(height: 4),
        Text(
          '${observation.cropStage} stage · ${severityLabels[observation.severity] ?? observation.severity} severity',
          style: const TextStyle(fontSize: 13, color: AppColors.inkSoft),
        ),
        const SizedBox(height: 16),
        if (observation.imageUrl != null) ...[
          ClipRRect(
            borderRadius: BorderRadius.circular(12),
            child: Image.network(
              observation.imageUrl!,
              height: 200,
              width: double.infinity,
              fit: BoxFit.cover,
              errorBuilder: (context, error, stackTrace) => Container(
                height: 120,
                color: AppColors.creamDeep,
                alignment: Alignment.center,
                child: const Icon(Icons.image_not_supported_outlined, color: AppColors.inkSoft),
              ),
            ),
          ),
          const SizedBox(height: 16),
        ],
        const Text('Symptoms', style: TextStyle(fontWeight: FontWeight.bold)),
        const SizedBox(height: 6),
        Text(observation.symptoms),
        const SizedBox(height: 24),

        if (analysisError != null) ...[
          Container(
            padding: const EdgeInsets.all(12),
            decoration: BoxDecoration(
              color: AppColors.errorBg,
              borderRadius: BorderRadius.circular(8),
            ),
            child: Text(analysisError!, style: const TextStyle(color: AppColors.errorText)),
          ),
          const SizedBox(height: 16),
        ],

        if (observation.reports.isEmpty) ...[
          if (observation.lastAnalyzedAt != null)
            Padding(
              padding: const EdgeInsets.only(bottom: 12),
              child: Text(
                'The diagnosis agent found no likely match in the knowledge base as of '
                '${_formatDate(observation.lastAnalyzedAt!)}. You can ask again if you have '
                'more to add.',
                style: const TextStyle(color: AppColors.inkSoft),
              ),
            ),
          ElevatedButton.icon(
            onPressed: onRequestAnalysis,
            icon: const Icon(Icons.auto_awesome_outlined, size: 18),
            label: Text(observation.lastAnalyzedAt == null
                ? 'Ask the diagnosis agent'
                : 'Ask the diagnosis agent again'),
          ),
        ] else ...[
          const Text('Possible matches', style: TextStyle(fontWeight: FontWeight.bold)),
          const SizedBox(height: 8),
          ...observation.reports.map((report) => _ReportCard(report: report)),
        ],
      ],
    );
  }

  String _formatDate(DateTime date) {
    final local = date.toLocal();
    return '${local.day}/${local.month}/${local.year}';
  }
}

class _ReportCard extends StatelessWidget {
  final PestDiseaseReport report;

  const _ReportCard({required this.report});

  @override
  Widget build(BuildContext context) {
    return Container(
      margin: const EdgeInsets.only(bottom: 10),
      padding: const EdgeInsets.all(12),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(10),
        border: Border.all(color: AppColors.line),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(report.possibleIssue,
              style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 15)),
          const SizedBox(height: 4),
          Text(
            '${report.confidencePercent} possible match',
            style: const TextStyle(fontSize: 12, color: AppColors.inkSoft),
          ),
          const SizedBox(height: 8),
          Container(
            padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
            decoration: BoxDecoration(
              color: AppColors.badgeBuyerBg,
              borderRadius: BorderRadius.circular(9999),
            ),
            child: Text(
              reportStatusLabel(report.status),
              style: const TextStyle(fontSize: 11, fontWeight: FontWeight.w600),
            ),
          ),
          if (report.officerComment != null && report.officerComment!.isNotEmpty) ...[
            const SizedBox(height: 8),
            Text(report.officerComment!,
                style: const TextStyle(fontSize: 13, fontStyle: FontStyle.italic)),
          ],
        ],
      ),
    );
  }
}
