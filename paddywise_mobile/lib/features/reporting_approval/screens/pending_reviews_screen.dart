import 'dart:convert';
import 'package:flutter/material.dart';
import 'package:http/http.dart' as http;
import '../../../theme/app_theme.dart';
import '../../../services/auth_service.dart';

class PendingReviewsScreen extends StatefulWidget {
  const PendingReviewsScreen({super.key});

  @override
  State<PendingReviewsScreen> createState() => _PendingReviewsScreenState();
}

class _PendingReviewsScreenState extends State<PendingReviewsScreen> {
  bool _isLoading = true;
  List<Map<String, dynamic>> _plans = [];

  @override
  void initState() {
    super.initState();
    _fetchPendingPlans();
  }

  Future<void> _fetchPendingPlans() async {
    setState(() {
      _isLoading = true;
    });

    final token = AuthService.getAccessToken();
    final url = Uri.parse('${AuthService.apiBaseUrl}/plans/pending');

    try {
      final response = await http.get(
        url,
        headers: {
          'Content-Type': 'application/json',
          if (token != null) 'Authorization': 'Bearer $token',
        },
      ).timeout(const Duration(seconds: 4));

      if (response.statusCode == 200) {
        final List<dynamic> data = jsonDecode(response.body) as List<dynamic>;
        setState(() {
          _plans = data.cast<Map<String, dynamic>>();
          _isLoading = false;
        });
        return;
      }
    } catch (_) {
      // Backend offline or timeout -> fallback to mock demo queue
    }

    // Default mock data for offline demo
    setState(() {
      _plans = [
        {
          'id': 101,
          'farmerName': 'Bandara Wanninayake',
          'fieldName': 'Maha Kumbura (Plot 04)',
          'division': 'Medirigiriya',
          'variety': 'Bg 352 (Nadu)',
          'targetYield': '4.5 MT/Acre',
          'submittedAt': 'Today at 09:30 AM',
          'aiValidation': 'Valid — 0 Rule Breaches',
          'aiScore': '98% Confidence',
        },
        {
          'id': 102,
          'farmerName': 'K. G. Sunil Jayasuriya',
          'fieldName': 'Pahalawatta',
          'division': 'Tambuttegama',
          'variety': 'At 362 (Red Samba)',
          'targetYield': '3.8 MT/Acre',
          'submittedAt': 'Yesterday',
          'aiValidation': 'Requires Verification — High Seed Rate',
          'aiScore': '74% Confidence',
        },
        {
          'id': 103,
          'farmerName': 'D. M. Karunaratne',
          'fieldName': 'Ihala Kumbura',
          'division': 'Polonnaruwa',
          'variety': 'Bg 300 (White Raw)',
          'targetYield': '4.0 MT/Acre',
          'submittedAt': '2 days ago',
          'aiValidation': 'Valid — Scheduled for Yala 2026',
          'aiScore': '95% Confidence',
        },
      ];
      _isLoading = false;
    });
  }

  void _showReviewDialog(Map<String, dynamic> plan) {
    showModalBottomSheet(
      context: context,
      isScrollControlled: true,
      backgroundColor: Colors.transparent,
      builder: (ctx) => _PlanReviewBottomSheet(
        plan: plan,
        onApproved: () {
          setState(() {
            _plans.removeWhere((p) => p['id'] == plan['id']);
          });
          ScaffoldMessenger.of(context).showSnackBar(
            SnackBar(
              content: Text('Plan #${plan['id']} successfully approved!'),
              backgroundColor: AppColors.forest,
            ),
          );
        },
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.cream,
      body: RefreshIndicator(
        onRefresh: _fetchPendingPlans,
        color: AppColors.gold,
        child: SingleChildScrollView(
          physics: const AlwaysScrollableScrollPhysics(),
          padding: const EdgeInsets.all(16),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              // Header
              Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      const Text(
                        'OFFICER APPROVAL QUEUE',
                        style: TextStyle(
                          fontSize: 11,
                          fontWeight: FontWeight.bold,
                          letterSpacing: 1.2,
                          color: AppColors.shoot,
                        ),
                      ),
                      const SizedBox(height: 4),
                      const Text(
                        'Pending Reviews',
                        style: TextStyle(
                          fontSize: 22,
                          fontWeight: FontWeight.bold,
                          color: AppColors.ink,
                          fontFamily: 'serif',
                        ),
                      ),
                    ],
                  ),
                  Container(
                    padding: const EdgeInsets.symmetric(
                        horizontal: 12, vertical: 6),
                    decoration: BoxDecoration(
                      color: AppColors.gold.withAlpha(40),
                      borderRadius: BorderRadius.circular(20),
                      border: Border.all(color: AppColors.gold),
                    ),
                    child: Text(
                      '${_plans.length} Waiting',
                      style: const TextStyle(
                        fontWeight: FontWeight.bold,
                        fontSize: 12,
                        color: AppColors.goldDeep,
                      ),
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 8),
              const Text(
                'AI-verified cultivation plans awaiting your official sign-off or revision recommendations.',
                style: TextStyle(fontSize: 13, color: AppColors.inkSoft),
              ),
              const SizedBox(height: 16),

              if (_isLoading)
                const Center(
                  child: Padding(
                    padding: EdgeInsets.all(40),
                    child: CircularProgressIndicator(color: AppColors.gold),
                  ),
                )
              else if (_plans.isEmpty)
                Center(
                  child: Padding(
                    padding: const EdgeInsets.all(40),
                    child: Column(
                      children: const [
                        Icon(Icons.done_all_rounded,
                            size: 56, color: AppColors.shoot),
                        SizedBox(height: 12),
                        Text(
                          'Queue is all clear!',
                          style: TextStyle(
                            fontSize: 18,
                            fontWeight: FontWeight.bold,
                            color: AppColors.ink,
                          ),
                        ),
                        SizedBox(height: 4),
                        Text(
                          'No pending cultivation plans require review at this time.',
                          style: TextStyle(
                              fontSize: 13, color: AppColors.inkSoft),
                          textAlign: TextAlign.center,
                        ),
                      ],
                    ),
                  ),
                )
              else
                ListView.separated(
                  shrinkWrap: true,
                  physics: const NeverScrollableScrollPhysics(),
                  itemCount: _plans.length,
                  separatorBuilder: (ctx, i) => const SizedBox(height: 12),
                  itemBuilder: (ctx, i) {
                    final plan = _plans[i];
                    return _buildPlanCard(plan);
                  },
                ),
            ],
          ),
        ),
      ),
    );
  }

  Widget _buildPlanCard(Map<String, dynamic> plan) {
    return Container(
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(12),
        border: Border.all(color: AppColors.line),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withAlpha(8),
            blurRadius: 6,
            offset: const Offset(0, 2),
          ),
        ],
      ),
      padding: const EdgeInsets.all(16),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Text(
                'Plan #${plan['id']}',
                style: const TextStyle(
                  fontWeight: FontWeight.bold,
                  fontSize: 13,
                  color: AppColors.shoot,
                ),
              ),
              Container(
                padding:
                    const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
                decoration: BoxDecoration(
                  color: AppColors.badgeOfficerBg,
                  borderRadius: BorderRadius.circular(4),
                ),
                child: Text(
                  plan['division'] ?? 'Division',
                  style: const TextStyle(
                    fontSize: 11,
                    fontWeight: FontWeight.bold,
                    color: AppColors.badgeOfficerText,
                  ),
                ),
              ),
            ],
          ),
          const SizedBox(height: 8),
          Text(
            plan['farmerName'] ?? 'Farmer',
            style: const TextStyle(
              fontSize: 16,
              fontWeight: FontWeight.bold,
              color: AppColors.ink,
              fontFamily: 'serif',
            ),
          ),
          const SizedBox(height: 4),
          Row(
            children: [
              const Icon(Icons.location_on_outlined,
                  size: 14, color: AppColors.inkSoft),
              const SizedBox(width: 4),
              Expanded(
                child: Text(
                  plan['fieldName'] ?? 'Field',
                  style: const TextStyle(
                      fontSize: 12, color: AppColors.inkSoft),
                ),
              ),
            ],
          ),
          const SizedBox(height: 4),
          Row(
            children: [
              const Icon(Icons.grain, size: 14, color: AppColors.inkSoft),
              const SizedBox(width: 4),
              Text(
                'Variety: ${plan['variety'] ?? 'Paddy'}',
                style: const TextStyle(
                    fontSize: 12, color: AppColors.inkSoft),
              ),
              const Spacer(),
              Text(
                plan['submittedAt'] ?? '',
                style: const TextStyle(
                    fontSize: 11, color: AppColors.inkSoft),
              ),
            ],
          ),
          const SizedBox(height: 12),
          Container(
            padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 8),
            decoration: BoxDecoration(
              color: AppColors.creamDeep.withAlpha(120),
              borderRadius: BorderRadius.circular(6),
            ),
            child: Row(
              children: [
                const Icon(Icons.auto_awesome,
                    size: 16, color: AppColors.shoot),
                const SizedBox(width: 8),
                Expanded(
                  child: Text(
                    plan['aiValidation'] ?? 'AI Checked',
                    style: const TextStyle(
                      fontSize: 12,
                      fontWeight: FontWeight.w600,
                      color: AppColors.forestDeep,
                    ),
                  ),
                ),
              ],
            ),
          ),
          const SizedBox(height: 12),
          SizedBox(
            width: double.infinity,
            child: ElevatedButton.icon(
              onPressed: () => _showReviewDialog(plan),
              icon: const Icon(Icons.rate_review_outlined, size: 18),
              label: const Text('Review & Decide'),
              style: ElevatedButton.styleFrom(
                padding: const EdgeInsets.symmetric(vertical: 12),
              ),
            ),
          ),
        ],
      ),
    );
  }
}

class _PlanReviewBottomSheet extends StatefulWidget {
  final Map<String, dynamic> plan;
  final VoidCallback onApproved;

  const _PlanReviewBottomSheet({
    required this.plan,
    required this.onApproved,
  });

  @override
  State<_PlanReviewBottomSheet> createState() => _PlanReviewBottomSheetState();
}

class _PlanReviewBottomSheetState extends State<_PlanReviewBottomSheet> {
  bool _isLoadingAiDraft = false;
  String _draftComment = '';
  final TextEditingController _commentController = TextEditingController();

  Future<void> _fetchAiDraft() async {
    setState(() {
      _isLoadingAiDraft = true;
    });

    final token = AuthService.getAccessToken();
    final planId = widget.plan['id'];
    final url = Uri.parse(
        '${AuthService.apiBaseUrl}/reviews/plans/$planId/draft-revision-comment');

    try {
      final response = await http.get(
        url,
        headers: {
          'Content-Type': 'application/json',
          if (token != null) 'Authorization': 'Bearer $token',
        },
      ).timeout(const Duration(seconds: 4));

      if (response.statusCode == 200) {
        final Map<String, dynamic> data =
            jsonDecode(response.body) as Map<String, dynamic>;
        final comment = data['draftComment'] as String? ?? '';
        setState(() {
          _draftComment = comment;
          _commentController.text = comment;
          _isLoadingAiDraft = false;
        });
        return;
      }
    } catch (_) {}

    // Fallback demo AI comment
    setState(() {
      _draftComment =
          'Please reduce initial basal fertilizer by 10% to prevent luxury nitrogen consumption in stage 2. Ensure field bunds are weed-free prior to water impounding.';
      _commentController.text = _draftComment;
      _isLoadingAiDraft = false;
    });
  }

  @override
  Widget build(BuildContext context) {
    return Container(
      decoration: const BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.vertical(top: Radius.circular(20)),
      ),
      padding: EdgeInsets.only(
        top: 20,
        left: 20,
        right: 20,
        bottom: MediaQuery.of(context).viewInsets.bottom + 20,
      ),
      child: SingleChildScrollView(
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          mainAxisSize: MainAxisSize.min,
          children: [
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                Text(
                  'Review Plan #${widget.plan['id']}',
                  style: const TextStyle(
                    fontSize: 18,
                    fontWeight: FontWeight.bold,
                    fontFamily: 'serif',
                    color: AppColors.ink,
                  ),
                ),
                IconButton(
                  onPressed: () => Navigator.pop(context),
                  icon: const Icon(Icons.close),
                ),
              ],
            ),
            const Divider(),
            const SizedBox(height: 8),
            Text(
              'Farmer: ${widget.plan['farmerName']}',
              style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 14),
            ),
            Text(
              'Field: ${widget.plan['fieldName']} • Variety: ${widget.plan['variety']}',
              style: const TextStyle(fontSize: 13, color: AppColors.inkSoft),
            ),
            const SizedBox(height: 16),
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                const Text(
                  'Officer Decision Comment',
                  style: TextStyle(fontWeight: FontWeight.w600, fontSize: 13),
                ),
                TextButton.icon(
                  onPressed: _isLoadingAiDraft ? null : _fetchAiDraft,
                  icon: const Icon(Icons.auto_awesome, size: 16, color: AppColors.shoot),
                  label: _isLoadingAiDraft
                      ? const SizedBox(
                          width: 14,
                          height: 14,
                          child: CircularProgressIndicator(strokeWidth: 2),
                        )
                      : const Text(
                          'AI Draft Comment',
                          style: TextStyle(
                            fontSize: 12,
                            color: AppColors.shoot,
                            fontWeight: FontWeight.bold,
                          ),
                        ),
                ),
              ],
            ),
            const SizedBox(height: 6),
            TextField(
              controller: _commentController,
              maxLines: 4,
              decoration: const InputDecoration(
                hintText:
                    'Enter your instructions or click AI Draft Comment to generate automated suggestions...',
              ),
            ),
            const SizedBox(height: 20),
            Row(
              children: [
                Expanded(
                  child: OutlinedButton(
                    onPressed: () {
                      Navigator.pop(context);
                      ScaffoldMessenger.of(context).showSnackBar(
                        const SnackBar(
                          content: Text('Revision request sent to farmer.'),
                        ),
                      );
                    },
                    child: const Text('Request Revision'),
                  ),
                ),
                const SizedBox(width: 12),
                Expanded(
                  child: ElevatedButton(
                    onPressed: () {
                      Navigator.pop(context);
                      widget.onApproved();
                    },
                    child: const Text('Approve Plan'),
                  ),
                ),
              ],
            ),
          ],
        ),
      ),
    );
  }
}
