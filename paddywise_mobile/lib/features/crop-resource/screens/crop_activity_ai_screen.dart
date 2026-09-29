import 'package:flutter/material.dart';
import '../../../theme/app_theme.dart';
import '../models/crop_activity_models.dart';
import '../models/crop_analysis_models.dart';
import '../services/crop_activity_service.dart';
import '../services/crop_analysis_service.dart';

/// Screen hosting the Crop Activity Agentic AI Advisor & Recommendations
/// based on Sri Lankan Department of Agriculture (DOA) & RRDI guidelines.
class CropActivityAiScreen extends StatefulWidget {
  final int? cycleId;

  const CropActivityAiScreen({
    super.key,
    this.cycleId,
  });

  @override
  State<CropActivityAiScreen> createState() => _CropActivityAiScreenState();
}

class _CropActivityAiScreenState extends State<CropActivityAiScreen> {
  bool _isLoading = true;
  bool _isAnalyzing = false;
  String? _errorMessage;

  List<CultivationCycleSummary> _cycles = [];
  CultivationCycleSummary? _selectedCycle;
  CropActivityAnalysisOutput? _analysis;
  final Set<String> _executingRecIds = {};

  // Chat Console States
  final TextEditingController _chatController = TextEditingController();
  final ScrollController _chatScrollController = ScrollController();
  final List<AiChatMessage> _chatMessages = [];
  bool _isChatLoading = false;

  final List<String> _suggestedQuestions = [
    'When is my next fertilizer split due?',
    'Is 5 cm water depth optimal for tillering?',
    'Are my pesticide applications safe?',
    'How do I identify panicle initiation in Bg 352?',
  ];

  @override
  void initState() {
    super.initState();
    _loadCyclesAndRunAnalysis();
  }

  @override
  void dispose() {
    _chatController.dispose();
    _chatScrollController.dispose();
    super.dispose();
  }

  Future<void> _loadCyclesAndRunAnalysis() async {
    setState(() {
      _isLoading = true;
      _errorMessage = null;
    });

    try {
      final cycles = await CropActivityService.getCycles();
      _cycles = cycles;

      if (cycles.isNotEmpty) {
        if (widget.cycleId != null) {
          _selectedCycle = cycles.firstWhere(
            (c) => c.id == widget.cycleId,
            orElse: () => cycles.first,
          );
        } else {
          _selectedCycle = cycles.first;
        }
      } else {
        _selectedCycle = CropActivityService.defaultDemoCycle;
      }

      await _fetchAnalysis();
    } catch (e) {
      if (mounted) {
        setState(() {
          _isLoading = false;
          _errorMessage = 'Failed to load cultivation cycles: $e';
        });
      }
    }
  }

  Future<void> _fetchAnalysis() async {
    if (_selectedCycle == null) return;

    setState(() {
      _isAnalyzing = true;
      _errorMessage = null;
    });

    try {
      final analysis = await CropAnalysisService.runAnalysis(_selectedCycle!.id);
      if (mounted) {
        setState(() {
          _analysis = analysis;
          _isLoading = false;
          _isAnalyzing = false;
        });
      }
    } catch (e) {
      if (mounted) {
        setState(() {
          _isLoading = false;
          _isAnalyzing = false;
          _errorMessage = 'Failed to analyze crop activities: $e';
        });
      }
    }
  }

  Future<void> _handleExecuteRecommendation(ActivityRecommendation rec) async {
    if (_selectedCycle == null || _executingRecIds.contains(rec.id)) return;

    setState(() {
      _executingRecIds.add(rec.id);
    });

    try {
      final success = await CropAnalysisService.executeRecommendation(
        _selectedCycle!.id,
        rec.id,
        payloadJson: rec.executionPayloadJson,
      );

      if (!mounted) return;

      if (success) {
        setState(() {
          final updatedRecs = _analysis!.recommendations.map((r) {
            if (r.id == rec.id) {
              return r.copyWith(
                status: 'EXECUTED',
                executedAt: DateTime.now(),
              );
            }
            return r;
          }).toList();

          _analysis = CropActivityAnalysisOutput(
            fieldOverview: _analysis!.fieldOverview,
            diagnostics: _analysis!.diagnostics,
            recommendations: updatedRecs,
            warnings: _analysis!.warnings,
            requiresOfficerReview: _analysis!.requiresOfficerReview,
            executiveSummary: _analysis!.executiveSummary,
            analyzedAt: _analysis!.analyzedAt,
          );
        });

        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            backgroundColor: AppColors.forest,
            content: Row(
              children: const [
                Icon(Icons.check_circle_outline, color: Colors.white, size: 20),
                SizedBox(width: 8),
                Expanded(
                  child: Text(
                    'Recommendation committed to database field activity ledger!',
                    style: TextStyle(color: Colors.white, fontSize: 13),
                  ),
                ),
              ],
            ),
          ),
        );
      } else {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(
            backgroundColor: Colors.redAccent,
            content: Text('Failed to execute recommendation. Please check network connection.'),
          ),
        );
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            backgroundColor: Colors.redAccent,
            content: Text('Error executing recommendation: $e'),
          ),
        );
      }
    } finally {
      if (mounted) {
        setState(() {
          _executingRecIds.remove(rec.id);
        });
      }
    }
  }

  Future<void> _handleAskQuestion([String? presetQuestion]) async {
    final text = (presetQuestion ?? _chatController.text).trim();
    if (text.isEmpty || _selectedCycle == null || _isChatLoading) return;

    final userMessage = AiChatMessage(role: 'user', content: text);
    setState(() {
      _chatMessages.add(userMessage);
      _isChatLoading = true;
      _chatController.clear();
    });

    _scrollToBottom();

    try {
      final response = await CropAnalysisService.chatWithAi(
        _selectedCycle!.id,
        text,
        history: _chatMessages,
      );

      if (mounted) {
        setState(() {
          _chatMessages.add(
            AiChatMessage(role: 'assistant', content: response.answer),
          );
          _isChatLoading = false;
        });
        _scrollToBottom();
      }
    } catch (e) {
      if (mounted) {
        setState(() {
          _chatMessages.add(
            const AiChatMessage(
              role: 'assistant',
              content:
                  'Unable to complete your request at this time. Please check your connectivity and try again.',
            ),
          );
          _isChatLoading = false;
        });
        _scrollToBottom();
      }
    }
  }

  void _scrollToBottom() {
    WidgetsBinding.instance.addPostFrameCallback((_) {
      if (_chatScrollController.hasClients) {
        _chatScrollController.animateTo(
          _chatScrollController.position.maxScrollExtent,
          duration: const Duration(milliseconds: 300),
          curve: Curves.easeOut,
        );
      }
    });
  }

  void _showCyclePicker() {
    if (_cycles.isEmpty) return;

    showModalBottomSheet(
      context: context,
      backgroundColor: Colors.white,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(20)),
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
              ..._cycles.map((cycle) {
                final isCurrent = cycle.id == _selectedCycle?.id;
                return ListTile(
                  leading: Icon(
                    Icons.grass_rounded,
                    color: isCurrent ? AppColors.forest : AppColors.inkSoft,
                  ),
                  title: Text(
                    cycle.displayName,
                    style: TextStyle(
                      fontWeight:
                          isCurrent ? FontWeight.bold : FontWeight.w500,
                      color: isCurrent ? AppColors.forest : AppColors.ink,
                    ),
                  ),
                  subtitle: Text(
                    '${cycle.fieldName} • Stage: ${cycle.currentStage}',
                    style:
                        const TextStyle(fontSize: 12, color: AppColors.inkSoft),
                  ),
                  trailing: isCurrent
                      ? const Icon(Icons.check_circle_rounded,
                          color: AppColors.forest, size: 20)
                      : null,
                  onTap: () {
                    Navigator.pop(ctx);
                    setState(() {
                      _selectedCycle = cycle;
                    });
                    _fetchAnalysis();
                  },
                );
              }),
            ],
          ),
        ),
      ),
    );
  }

  Color _getStatusColor(String status) {
    final s = status.toLowerCase().replaceAll(RegExp(r'[\s_]+'), '');
    switch (s) {
      case 'adequate':
      case 'balanced':
      case 'safe':
      case 'optimal':
        return const Color(0xFF2E7D32);
      case 'due':
      case 'duesoon':
      case 'monitoringrequired':
      case 'warning':
        return const Color(0xFFD97706);
      case 'flooded':
      case 'droughtrisk':
      case 'excessive':
      case 'highrisk':
      case 'safetyalert':
      case 'deficient':
        return const Color(0xFFDC2626);
      default:
        return AppColors.forest;
    }
  }

  Color _getPriorityColor(String priority) {
    switch (priority.toUpperCase()) {
      case 'HIGH':
        return const Color(0xFFDC2626);
      case 'MEDIUM':
        return const Color(0xFFD97706);
      default:
        return const Color(0xFF2E7D32);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.cream,
      appBar: AppBar(
        backgroundColor: AppColors.forest,
        foregroundColor: AppColors.cream,
        elevation: 0,
        title: Row(
          children: const [
            Icon(Icons.auto_awesome_rounded, size: 20, color: AppColors.gold),
            SizedBox(width: 8),
            Text(
              'AI Field Advisor',
              style: TextStyle(
                fontSize: 18,
                fontWeight: FontWeight.bold,
                fontFamily: 'serif',
              ),
            ),
          ],
        ),
        actions: [
          IconButton(
            icon: _isAnalyzing
                ? const SizedBox(
                    width: 18,
                    height: 18,
                    child: CircularProgressIndicator(
                      strokeWidth: 2,
                      valueColor:
                          AlwaysStoppedAnimation<Color>(AppColors.cream),
                    ),
                  )
                : const Icon(Icons.refresh_rounded),
            onPressed: _isAnalyzing ? null : _fetchAnalysis,
            tooltip: 'Re-Analyze Activities',
          ),
        ],
      ),
      body: SafeArea(
        child: _isLoading
            ? const Center(
                child: Column(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    CircularProgressIndicator(color: AppColors.forest),
                    SizedBox(height: 16),
                    Text(
                      'Synthesizing Agronomic Intelligence...',
                      style: TextStyle(
                        fontSize: 14,
                        color: AppColors.inkSoft,
                        fontWeight: FontWeight.w600,
                      ),
                    ),
                  ],
                ),
              )
            : RefreshIndicator(
                color: AppColors.forest,
                onRefresh: _fetchAnalysis,
                child: ListView(
                  padding: const EdgeInsets.all(16),
                  children: [
                    // Top Hero Banner
                    _buildHeroHeader(),
                    const SizedBox(height: 16),

                    if (_errorMessage != null) ...[
                      _buildErrorBanner(_errorMessage!),
                      const SizedBox(height: 16),
                    ],

                    if (_analysis != null) ...[
                      // 1. Executive Summary
                      _buildExecutiveSummaryCard(_analysis!),
                      const SizedBox(height: 16),

                      // 2. Critical Safety Warnings (if present)
                      if (_analysis!.warnings.isNotEmpty ||
                          _analysis!.requiresOfficerReview) ...[
                        _buildWarningsBanner(_analysis!),
                        const SizedBox(height: 16),
                      ],

                      // 3. Diagnostics Telemetry Grid
                      _buildSectionHeader(
                        title: 'Real-Time Field Diagnostics',
                        subtitle:
                            'Audited against stage-specific DOA & RRDI guidelines',
                        icon: Icons.monitor_heart_outlined,
                      ),
                      const SizedBox(height: 12),
                      _buildDiagnosticsGrid(_analysis!.diagnostics),
                      const SizedBox(height: 20),

                      // 4. Actionable Agronomic Recommendations
                      _buildSectionHeader(
                        title: 'Agronomic Recommendations',
                        subtitle:
                            'Prioritized interventions • Saved to database & routed to Officer',
                        icon: Icons.recommend_outlined,
                      ),
                      const SizedBox(height: 12),
                      ..._analysis!.recommendations.map(
                        (rec) => _buildRecommendationCard(rec),
                      ),
                      const SizedBox(height: 20),

                      // 5. Interactive AI Q&A Console
                      _buildSectionHeader(
                        title: 'Ask Field Advisor',
                        subtitle:
                            'Get instant agronomic clarification for this cycle',
                        icon: Icons.chat_bubble_outline_rounded,
                      ),
                      const SizedBox(height: 12),
                      _buildChatConsole(),
                      const SizedBox(height: 28),
                    ],
                  ],
                ),
              ),
      ),
    );
  }

  Widget _buildHeroHeader() {
    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: AppColors.forestDeep,
        borderRadius: BorderRadius.circular(16),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withAlpha(25),
            blurRadius: 10,
            offset: const Offset(0, 4),
          ),
        ],
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Container(
                padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
                decoration: BoxDecoration(
                  color: AppColors.gold.withAlpha(40),
                  borderRadius: BorderRadius.circular(20),
                  border: Border.all(color: AppColors.gold.withAlpha(120)),
                ),
                child: Row(
                  mainAxisSize: MainAxisSize.min,
                  children: const [
                    Icon(Icons.auto_awesome_rounded,
                        size: 13, color: AppColors.gold),
                    SizedBox(width: 5),
                    Text(
                      'AGENTIC AI ADVISOR',
                      style: TextStyle(
                        fontSize: 10,
                        fontWeight: FontWeight.bold,
                        letterSpacing: 0.8,
                        color: AppColors.gold,
                      ),
                    ),
                  ],
                ),
              ),
              const Spacer(),
              if (_cycles.length > 1)
                InkWell(
                  onTap: _showCyclePicker,
                  borderRadius: BorderRadius.circular(8),
                  child: Container(
                    padding: const EdgeInsets.symmetric(
                        horizontal: 8, vertical: 4),
                    decoration: BoxDecoration(
                      color: Colors.white.withAlpha(25),
                      borderRadius: BorderRadius.circular(8),
                    ),
                    child: Row(
                      mainAxisSize: MainAxisSize.min,
                      children: const [
                        Icon(Icons.sync_rounded,
                            size: 14, color: AppColors.cream),
                        SizedBox(width: 4),
                        Text(
                          'Switch Cycle',
                          style: TextStyle(
                            fontSize: 11,
                            fontWeight: FontWeight.bold,
                            color: AppColors.cream,
                          ),
                        ),
                      ],
                    ),
                  ),
                ),
            ],
          ),
          const SizedBox(height: 12),
          Text(
            _selectedCycle?.fieldName ?? 'Maha Kumbura (Plot 04)',
            style: const TextStyle(
              fontSize: 20,
              fontWeight: FontWeight.bold,
              color: AppColors.cream,
              fontFamily: 'serif',
            ),
          ),
          const SizedBox(height: 6),
          Row(
            children: [
              _buildMetaPill(
                Icons.calendar_month_outlined,
                '${_selectedCycle?.season ?? "Yala"} ${_selectedCycle?.year ?? DateTime.now().year}',
              ),
              const SizedBox(width: 8),
              _buildMetaPill(
                Icons.grass_rounded,
                _selectedCycle?.varietyName ?? 'Bg 352',
              ),
              const SizedBox(width: 8),
              _buildMetaPill(
                Icons.timeline_rounded,
                _selectedCycle?.currentStage ?? 'Tillering',
                isHighlight: true,
              ),
            ],
          ),
        ],
      ),
    );
  }

  Widget _buildMetaPill(IconData icon, String text,
      {bool isHighlight = false}) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
      decoration: BoxDecoration(
        color: isHighlight
            ? AppColors.shoot.withAlpha(40)
            : Colors.white.withAlpha(20),
        borderRadius: BorderRadius.circular(6),
        border: Border.all(
          color: isHighlight
              ? AppColors.shoot.withAlpha(100)
              : Colors.white.withAlpha(30),
        ),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(icon, size: 12, color: isHighlight ? AppColors.shootLight : AppColors.cream),
          const SizedBox(width: 4),
          Text(
            text,
            style: TextStyle(
              fontSize: 11,
              fontWeight: FontWeight.w600,
              color: isHighlight ? AppColors.shootLight : AppColors.cream,
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildExecutiveSummaryCard(CropActivityAnalysisOutput analysis) {
    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(14),
        border: Border.all(color: AppColors.line),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withAlpha(8),
            blurRadius: 8,
            offset: const Offset(0, 2),
          ),
        ],
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Container(
                padding: const EdgeInsets.all(8),
                decoration: BoxDecoration(
                  color: AppColors.forest.withAlpha(20),
                  borderRadius: BorderRadius.circular(8),
                ),
                child: const Icon(Icons.verified_rounded,
                    size: 20, color: AppColors.forest),
              ),
              const SizedBox(width: 10),
              const Expanded(
                child: Text(
                  'Agronomic Executive Summary',
                  style: TextStyle(
                    fontSize: 16,
                    fontWeight: FontWeight.bold,
                    color: AppColors.ink,
                    fontFamily: 'serif',
                  ),
                ),
              ),
            ],
          ),
          const SizedBox(height: 10),
          const Divider(height: 1, color: AppColors.line),
          const SizedBox(height: 10),
          Text(
            analysis.executiveSummary,
            style: const TextStyle(
              fontSize: 13,
              color: AppColors.ink,
              height: 1.5,
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildWarningsBanner(CropActivityAnalysisOutput analysis) {
    return Container(
      padding: const EdgeInsets.all(14),
      decoration: BoxDecoration(
        color: const Color(0xFFFEF2F2),
        borderRadius: BorderRadius.circular(12),
        border: Border.all(color: const Color(0xFFFCA5A5)),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              const Icon(Icons.warning_amber_rounded,
                  size: 20, color: Color(0xFFDC2626)),
              const SizedBox(width: 8),
              Expanded(
                child: Text(
                  'Critical Agronomic Warnings (${analysis.warnings.length})',
                  style: const TextStyle(
                    fontSize: 14,
                    fontWeight: FontWeight.bold,
                    color: Color(0xFFB91C1C),
                  ),
                ),
              ),
            ],
          ),
          const SizedBox(height: 8),
          ...analysis.warnings.map(
            (w) => Padding(
              padding: const EdgeInsets.only(bottom: 4),
              child: Row(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  const Text('• ',
                      style: TextStyle(
                          color: Color(0xFFDC2626),
                          fontWeight: FontWeight.bold)),
                  Expanded(
                    child: Text(
                      w,
                      style: const TextStyle(
                          fontSize: 12, color: Color(0xFF7F1D1D), height: 1.3),
                    ),
                  ),
                ],
              ),
            ),
          ),
          if (analysis.requiresOfficerReview) ...[
            const SizedBox(height: 8),
            Container(
              padding: const EdgeInsets.all(8),
              decoration: BoxDecoration(
                color: Colors.white,
                borderRadius: BorderRadius.circular(8),
                border: Border.all(color: const Color(0xFFF87171)),
              ),
              child: Row(
                children: const [
                  Icon(Icons.phone_in_talk_rounded,
                      size: 16, color: Color(0xFFDC2626)),
                  SizedBox(width: 6),
                  Expanded(
                    child: Text(
                      'Officer Review Advised: Please contact your local ASC Agricultural Instructor (AI).',
                      style: TextStyle(
                        fontSize: 11,
                        fontWeight: FontWeight.bold,
                        color: Color(0xFF991B1B),
                      ),
                    ),
                  ),
                ],
              ),
            ),
          ],
        ],
      ),
    );
  }

  Widget _buildSectionHeader({
    required String title,
    required String subtitle,
    required IconData icon,
  }) {
    return Row(
      children: [
        Container(
          padding: const EdgeInsets.all(6),
          decoration: BoxDecoration(
            color: AppColors.forest.withAlpha(20),
            borderRadius: BorderRadius.circular(6),
          ),
          child: Icon(icon, size: 18, color: AppColors.forest),
        ),
        const SizedBox(width: 10),
        Expanded(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                title,
                style: const TextStyle(
                  fontSize: 15,
                  fontWeight: FontWeight.bold,
                  color: AppColors.ink,
                  fontFamily: 'serif',
                ),
              ),
              Text(
                subtitle,
                style: const TextStyle(fontSize: 11, color: AppColors.inkSoft),
              ),
            ],
          ),
        ),
      ],
    );
  }

  Widget _buildDiagnosticsGrid(DiagnosticsSummary diag) {
    return Column(
      children: [
        Row(
          children: [
            Expanded(
              child: _buildDiagnosticCard(
                title: 'Water Hydrology',
                icon: Icons.water_drop_outlined,
                iconColor: const Color(0xFF0288D1),
                status: diag.water.status,
                heroValue: diag.water.latestWaterLevelCm != null
                    ? '${diag.water.latestWaterLevelCm} cm'
                    : '—',
                heroLabel: 'Current Depth',
                subMetrics: [
                  'Events: ${diag.water.totalIrrigationEvents}',
                  'Last: ${diag.water.daysSinceLastIrrigation}d ago',
                ],
                assessment: diag.water.assessment,
              ),
            ),
            const SizedBox(width: 12),
            Expanded(
              child: _buildDiagnosticCard(
                title: 'Nutrient Balance',
                icon: Icons.eco_outlined,
                iconColor: const Color(0xFF2E7D32),
                status: diag.fertilizer.status,
                heroValue: '${diag.fertilizer.totalUreaKgPerHa.round()} kg',
                heroLabel: 'Urea Applied',
                subMetrics: [
                  'Splits: ${diag.fertilizer.applicationsCount}',
                  diag.fertilizer.splitCompliance,
                ],
                assessment: diag.fertilizer.assessment,
              ),
            ),
          ],
        ),
        const SizedBox(height: 12),
        Row(
          children: [
            Expanded(
              child: _buildDiagnosticCard(
                title: 'Crop Protection',
                icon: Icons.shield_outlined,
                iconColor: const Color(0xFFD97706),
                status: diag.pest.status,
                heroValue: '${diag.pest.treatmentsCount}',
                heroLabel: 'Sprays Logged',
                subMetrics: [
                  'Safety: ${diag.pest.bannedChemicalDetected ? "Alert" : "Safe"}',
                  'Last: ${diag.pest.daysSinceLastTreatment}d ago',
                ],
                assessment: diag.pest.assessment,
              ),
            ),
            const SizedBox(width: 12),
            Expanded(
              child: _buildDiagnosticCard(
                title: 'Field Operations',
                icon: Icons.task_alt_rounded,
                iconColor: AppColors.forest,
                status: diag.other.weedingStatus,
                heroValue: '${diag.other.activitiesCount}',
                heroLabel: 'Field Tasks',
                subMetrics: [
                  'Weeding: ${diag.other.weedingStatus}',
                ],
                assessment: diag.other.assessment,
              ),
            ),
          ],
        ),
      ],
    );
  }

  Widget _buildDiagnosticCard({
    required String title,
    required IconData icon,
    required Color iconColor,
    required String status,
    required String heroValue,
    required String heroLabel,
    required List<String> subMetrics,
    required String assessment,
  }) {
    final statusColor = _getStatusColor(status);

    return Container(
      padding: const EdgeInsets.all(12),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(12),
        border: Border.all(color: AppColors.line),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Icon(icon, size: 16, color: iconColor),
              const SizedBox(width: 4),
              Expanded(
                child: Text(
                  title,
                  style: const TextStyle(
                    fontSize: 12,
                    fontWeight: FontWeight.bold,
                    color: AppColors.ink,
                  ),
                ),
              ),
            ],
          ),
          const SizedBox(height: 8),
          Container(
            padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 2),
            decoration: BoxDecoration(
              color: statusColor.withAlpha(20),
              borderRadius: BorderRadius.circular(4),
              border: Border.all(color: statusColor.withAlpha(60)),
            ),
            child: Text(
              status,
              style: TextStyle(
                fontSize: 10,
                fontWeight: FontWeight.bold,
                color: statusColor,
              ),
            ),
          ),
          const SizedBox(height: 8),
          Text(
            heroValue,
            style: const TextStyle(
              fontSize: 18,
              fontWeight: FontWeight.bold,
              color: AppColors.ink,
            ),
          ),
          Text(
            heroLabel,
            style: const TextStyle(fontSize: 10, color: AppColors.inkSoft),
          ),
          const SizedBox(height: 6),
          const Divider(height: 1, color: AppColors.line),
          const SizedBox(height: 6),
          ...subMetrics.map(
            (m) => Text(
              m,
              maxLines: 1,
              overflow: TextOverflow.ellipsis,
              style: const TextStyle(fontSize: 10, color: AppColors.inkSoft),
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildRecommendationCard(ActivityRecommendation rec) {
    final priorityColor = _getPriorityColor(rec.priority);
    final isExecuting = _executingRecIds.contains(rec.id);

    return Container(
      margin: const EdgeInsets.only(bottom: 12),
      padding: const EdgeInsets.all(14),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(12),
        border: Border.all(color: AppColors.line),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Container(
                padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 2),
                decoration: BoxDecoration(
                  color: priorityColor.withAlpha(20),
                  borderRadius: BorderRadius.circular(4),
                  border: Border.all(color: priorityColor.withAlpha(80)),
                ),
                child: Text(
                  '${rec.priority} PRIORITY',
                  style: TextStyle(
                    fontSize: 10,
                    fontWeight: FontWeight.bold,
                    color: priorityColor,
                  ),
                ),
              ),
              const SizedBox(width: 8),
              Container(
                padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 2),
                decoration: BoxDecoration(
                  color: AppColors.forest.withAlpha(15),
                  borderRadius: BorderRadius.circular(4),
                ),
                child: Text(
                  rec.category,
                  style: const TextStyle(
                    fontSize: 10,
                    fontWeight: FontWeight.w600,
                    color: AppColors.forest,
                  ),
                ),
              ),
              const Spacer(),
              Text(
                '${(rec.confidenceScore * 100).round()}% confidence',
                style: const TextStyle(
                  fontSize: 11,
                  fontWeight: FontWeight.w500,
                  color: AppColors.inkSoft,
                ),
              ),
            ],
          ),
          const SizedBox(height: 10),
          Text(
            rec.action,
            style: const TextStyle(
              fontSize: 14,
              fontWeight: FontWeight.bold,
              color: AppColors.ink,
              height: 1.3,
            ),
          ),
          const SizedBox(height: 6),
          Text(
            rec.reason,
            style: const TextStyle(
              fontSize: 12,
              color: AppColors.inkSoft,
              height: 1.4,
            ),
          ),
          const SizedBox(height: 8),
          Container(
            padding: const EdgeInsets.all(8),
            decoration: BoxDecoration(
              color: AppColors.cream,
              borderRadius: BorderRadius.circular(6),
            ),
            child: Row(
              children: [
                const Icon(Icons.bolt_rounded,
                    size: 14, color: AppColors.goldDeep),
                const SizedBox(width: 4),
                Expanded(
                  child: Text(
                    rec.evidence,
                    style: const TextStyle(
                      fontSize: 11,
                      fontStyle: FontStyle.italic,
                      color: AppColors.inkSoft,
                    ),
                  ),
                ),
              ],
            ),
          ),
          if (rec.citations.isNotEmpty) ...[
            const SizedBox(height: 8),
            Row(
              children: [
                const Icon(Icons.menu_book_rounded,
                    size: 13, color: AppColors.forest),
                const SizedBox(width: 4),
                Expanded(
                  child: Text(
                    rec.citations.first.document,
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                    style: const TextStyle(
                      fontSize: 10,
                      fontWeight: FontWeight.w600,
                      color: AppColors.forest,
                    ),
                  ),
                ),
              ],
            ),
          ],
          const SizedBox(height: 12),
          _buildRecommendationStatusBox(rec, isExecuting),
        ],
      ),
    );
  }

  Widget _buildRecommendationStatusBox(ActivityRecommendation rec, bool isExecuting) {
    final status = rec.status.toUpperCase();
    final isApproved = status == 'APPROVED';
    final isExecuted = status == 'EXECUTED';
    final isRejected = status == 'REJECTED';

    if (isApproved) {
      return Container(
        padding: const EdgeInsets.all(10),
        decoration: BoxDecoration(
          color: const Color(0xFFF0FDF4),
          borderRadius: BorderRadius.circular(8),
          border: Border.all(color: const Color(0xFFBBF7D0)),
        ),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                const Icon(Icons.check_circle_rounded, size: 16, color: Color(0xFF16A34A)),
                const SizedBox(width: 6),
                const Expanded(
                  child: Text(
                    'Verified & Approved by Agricultural Officer',
                    style: TextStyle(
                      fontSize: 12,
                      fontWeight: FontWeight.bold,
                      color: Color(0xFF15803D),
                    ),
                  ),
                ),
                if (rec.dbId != null)
                  Container(
                    padding: const EdgeInsets.symmetric(horizontal: 5, vertical: 2),
                    decoration: BoxDecoration(
                      color: Colors.white,
                      borderRadius: BorderRadius.circular(4),
                      border: Border.all(color: const Color(0xFF86EFAC)),
                    ),
                    child: Text(
                      'DB #${rec.dbId}',
                      style: const TextStyle(fontSize: 9, fontWeight: FontWeight.bold, color: Color(0xFF15803D)),
                    ),
                  ),
              ],
            ),
            if (rec.reviewedBy != null || rec.reviewNotes != null) ...[
              const SizedBox(height: 5),
              Text(
                '${rec.reviewedBy ?? "Officer"}: "${rec.reviewNotes ?? "Approved as per DOA guidelines."}"',
                style: const TextStyle(
                  fontSize: 11,
                  fontStyle: FontStyle.italic,
                  color: Color(0xFF166534),
                ),
              ),
            ],
            const SizedBox(height: 10),
            SizedBox(
              width: double.infinity,
              height: 38,
              child: ElevatedButton.icon(
                onPressed: isExecuting ? null : () => _handleExecuteRecommendation(rec),
                style: ElevatedButton.styleFrom(
                  backgroundColor: AppColors.forest,
                  foregroundColor: Colors.white,
                  elevation: 0,
                  shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(8)),
                  padding: const EdgeInsets.symmetric(horizontal: 12),
                ),
                icon: isExecuting
                    ? const SizedBox(
                        width: 14,
                        height: 14,
                        child: CircularProgressIndicator(strokeWidth: 2, color: Colors.white),
                      )
                    : const Icon(Icons.playlist_add_check_circle_rounded, size: 18),
                label: Text(
                  isExecuting ? 'Recording to Ledger...' : 'Approve & Execute into Field Ledger',
                  style: const TextStyle(fontSize: 12, fontWeight: FontWeight.bold),
                ),
              ),
            ),
            const SizedBox(height: 4),
            const Center(
              child: Text(
                'Commits directly to your official field activity log in database',
                style: TextStyle(fontSize: 10, color: Color(0xFF15803D)),
              ),
            ),
          ],
        ),
      );
    }

    if (isExecuted) {
      return Container(
        padding: const EdgeInsets.all(10),
        decoration: BoxDecoration(
          color: const Color(0xFFFAF5FF),
          borderRadius: BorderRadius.circular(8),
          border: Border.all(color: const Color(0xFFE9D5FF)),
        ),
        child: Row(
          children: [
            const Icon(Icons.task_alt_rounded, size: 16, color: Color(0xFF9333EA)),
            const SizedBox(width: 8),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  const Text(
                    'Executed & Logged in Field Ledger',
                    style: TextStyle(
                      fontSize: 12,
                      fontWeight: FontWeight.bold,
                      color: Color(0xFF7E22CE),
                    ),
                  ),
                  Text(
                    rec.executedAt != null
                        ? 'Recorded on ${rec.executedAt!.toLocal().toString().split(" ")[0]} in database.'
                        : 'Saved & committed to official field activities in database.',
                    style: const TextStyle(fontSize: 11, color: Color(0xFF6B21A8)),
                  ),
                ],
              ),
            ),
            if (rec.dbId != null)
              Container(
                padding: const EdgeInsets.symmetric(horizontal: 5, vertical: 2),
                decoration: BoxDecoration(
                  color: Colors.white,
                  borderRadius: BorderRadius.circular(4),
                  border: Border.all(color: const Color(0xFFD8B4FE)),
                ),
                child: Text(
                  'DB #${rec.dbId}',
                  style: const TextStyle(fontSize: 9, fontWeight: FontWeight.bold, color: Color(0xFF7E22CE)),
                ),
              ),
          ],
        ),
      );
    }

    if (isRejected) {
      return Container(
        padding: const EdgeInsets.all(10),
        decoration: BoxDecoration(
          color: const Color(0xFFFEF2F2),
          borderRadius: BorderRadius.circular(8),
          border: Border.all(color: const Color(0xFFFECACA)),
        ),
        child: Row(
          children: [
            const Icon(Icons.cancel_outlined, size: 16, color: Color(0xFFDC2626)),
            const SizedBox(width: 8),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  const Text(
                    'Agricultural Officer Declined',
                    style: TextStyle(
                      fontSize: 12,
                      fontWeight: FontWeight.bold,
                      color: Color(0xFFB91C1C),
                    ),
                  ),
                  Text(
                    rec.reviewNotes ?? 'Not recommended for application at this stage.',
                    style: const TextStyle(fontSize: 11, color: Color(0xFF991B1B)),
                  ),
                ],
              ),
            ),
          ],
        ),
      );
    }

    // Default: PENDING_OFFICER_REVIEW
    return Container(
      padding: const EdgeInsets.all(10),
      decoration: BoxDecoration(
        color: const Color(0xFFFFFBEB),
        borderRadius: BorderRadius.circular(8),
        border: Border.all(color: const Color(0xFFFDE68A)),
      ),
      child: Row(
        children: [
          const Icon(Icons.cloud_done_rounded, size: 16, color: Color(0xFFD97706)),
          const SizedBox(width: 8),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  children: [
                    const Text(
                      'Saved to Database',
                      style: TextStyle(
                        fontSize: 12,
                        fontWeight: FontWeight.bold,
                        color: Color(0xFFB45309),
                      ),
                    ),
                    const SizedBox(width: 6),
                    Container(
                      padding: const EdgeInsets.symmetric(horizontal: 5, vertical: 1.5),
                      decoration: BoxDecoration(
                        color: const Color(0xFFFEF3C7),
                        borderRadius: BorderRadius.circular(4),
                        border: Border.all(color: const Color(0xFFF59E0B)),
                      ),
                      child: Text(
                        rec.dbId != null ? 'DB #${rec.dbId} • PENDING REVIEW' : 'PENDING REVIEW',
                        style: const TextStyle(
                          fontSize: 9,
                          fontWeight: FontWeight.w700,
                          color: Color(0xFF92400E),
                        ),
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: 2),
                const Text(
                  'Queued for Agrarian Services Officer verification. Field execution will unlock once approved.',
                  style: TextStyle(fontSize: 11, color: Color(0xFF92400E)),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildChatConsole() {
    return Container(
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(14),
        border: Border.all(color: AppColors.line),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          // Suggested Prompt Chips
          Padding(
            padding: const EdgeInsets.fromLTRB(12, 12, 12, 6),
            child: Wrap(
              spacing: 6,
              runSpacing: 6,
              children: _suggestedQuestions.map((q) {
                return ActionChip(
                  label: Text(q, style: const TextStyle(fontSize: 11)),
                  backgroundColor: AppColors.cream,
                  side: const BorderSide(color: AppColors.line),
                  onPressed: () => _handleAskQuestion(q),
                );
              }).toList(),
            ),
          ),
          const Divider(height: 1, color: AppColors.line),

          // Message Thread
          Container(
            constraints: const BoxConstraints(minHeight: 100, maxHeight: 260),
            padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
            child: _chatMessages.isEmpty
                ? Center(
                    child: Padding(
                      padding: const EdgeInsets.all(16),
                      child: Text(
                        'Ask questions regarding your recorded activities, split schedules, or water depths.',
                        textAlign: TextAlign.center,
                        style: TextStyle(
                          fontSize: 12,
                          color: AppColors.inkSoft.withAlpha(180),
                        ),
                      ),
                    ),
                  )
                : ListView.builder(
                    controller: _chatScrollController,
                    shrinkWrap: true,
                    itemCount: _chatMessages.length,
                    itemBuilder: (ctx, i) {
                      final msg = _chatMessages[i];
                      final isUser = msg.role == 'user';
                      return Container(
                        margin: const EdgeInsets.symmetric(vertical: 4),
                        alignment: isUser
                            ? Alignment.centerRight
                            : Alignment.centerLeft,
                        child: Container(
                          constraints: BoxConstraints(
                            maxWidth:
                                MediaQuery.of(context).size.width * 0.75,
                          ),
                          padding: const EdgeInsets.all(10),
                          decoration: BoxDecoration(
                            color: isUser
                                ? AppColors.forest
                                : AppColors.creamDeep,
                            borderRadius: BorderRadius.circular(10),
                          ),
                          child: Text(
                            msg.content,
                            style: TextStyle(
                              fontSize: 12,
                              color: isUser ? AppColors.cream : AppColors.ink,
                              height: 1.4,
                            ),
                          ),
                        ),
                      );
                    },
                  ),
          ),

          if (_isChatLoading)
            const Padding(
              padding: EdgeInsets.symmetric(horizontal: 14, vertical: 4),
              child: Row(
                children: [
                  SizedBox(
                    width: 14,
                    height: 14,
                    child: CircularProgressIndicator(
                      strokeWidth: 2,
                      valueColor:
                          AlwaysStoppedAnimation<Color>(AppColors.forest),
                    ),
                  ),
                  SizedBox(width: 8),
                  Text(
                    'Advisor is analyzing...',
                    style: TextStyle(fontSize: 11, color: AppColors.inkSoft),
                  ),
                ],
              ),
            ),

          const Divider(height: 1, color: AppColors.line),

          // Chat Input Row
          Padding(
            padding: const EdgeInsets.all(8),
            child: Row(
              children: [
                Expanded(
                  child: TextField(
                    controller: _chatController,
                    decoration: const InputDecoration(
                      hintText: 'Ask advisor a question...',
                      hintStyle:
                          TextStyle(fontSize: 12, color: AppColors.inkSoft),
                      border: InputBorder.none,
                      contentPadding: EdgeInsets.symmetric(horizontal: 8),
                    ),
                    onSubmitted: (_) => _handleAskQuestion(),
                  ),
                ),
                IconButton(
                  icon: const Icon(Icons.send_rounded,
                      color: AppColors.forest, size: 20),
                  onPressed: _handleAskQuestion,
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildErrorBanner(String message) {
    return Container(
      padding: const EdgeInsets.all(12),
      decoration: BoxDecoration(
        color: const Color(0xFFFEF2F2),
        borderRadius: BorderRadius.circular(8),
        border: Border.all(color: const Color(0xFFFCA5A5)),
      ),
      child: Row(
        children: [
          const Icon(Icons.error_outline_rounded,
              color: Color(0xFFDC2626), size: 18),
          const SizedBox(width: 8),
          Expanded(
            child: Text(
              message,
              style: const TextStyle(fontSize: 12, color: Color(0xFF991B1B)),
            ),
          ),
        ],
      ),
    );
  }
}
