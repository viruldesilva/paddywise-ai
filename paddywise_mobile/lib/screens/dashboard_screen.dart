import 'package:flutter/material.dart';
import '../theme/app_theme.dart';
import '../models/user.dart';
import '../models/notification_item.dart';
import '../services/auth_service.dart';
import '../services/notification_service.dart';
import 'login_screen.dart';

class DashboardScreen extends StatefulWidget {
  final User? user;
  final bool embeddedInShell;

  const DashboardScreen({
    super.key,
    this.user,
    this.embeddedInShell = false,
  });

  @override
  State<DashboardScreen> createState() => _DashboardScreenState();
}

class _DashboardScreenState extends State<DashboardScreen> {
  late User _currentUser;
  List<User> _usersList = [];
  List<NotificationItem> _farmerNotifications = [];

  @override
  void initState() {
    super.initState();
    _currentUser = widget.user ?? AuthService.getCurrentUser()!;
    if (_currentUser.role == UserRole.admin) {
      _usersList = AuthService.getAllUsers();
    }
    _loadNotifications();
    NotificationService.unreadCountNotifier.addListener(_onNotificationCountChanged);
  }

  @override
  void dispose() {
    NotificationService.unreadCountNotifier.removeListener(_onNotificationCountChanged);
    super.dispose();
  }

  void _onNotificationCountChanged() {
    _loadNotifications();
  }

  void _loadNotifications() {
    if (mounted) {
      setState(() {
        _farmerNotifications =
            NotificationService.getNotificationsForUser(_currentUser.id);
      });
    }
  }

  Future<void> _handleLogout() async {
    await AuthService.logout();
    if (!mounted) return;
    Navigator.pushAndRemoveUntil(
      context,
      MaterialPageRoute(builder: (context) => const LoginScreen()),
      (route) => false,
    );
  }

  void _resetDefaults() async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('Reset Demo Users?'),
        content: const Text(
          'This will reset stored users back to the default 4 role accounts.',
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(ctx, false),
            child: const Text('Cancel'),
          ),
          TextButton(
            onPressed: () => Navigator.pop(ctx, true),
            child: const Text('Reset', style: TextStyle(color: Colors.red)),
          ),
        ],
      ),
    );

    if (confirmed == true) {
      await AuthService.resetToDefaults();
      setState(() {
        _usersList = AuthService.getAllUsers();
      });
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Reset to initial seed accounts.')),
        );
      }
    }
  }

  Color _getBadgeBg(UserRole role) {
    switch (role) {
      case UserRole.farmer:
        return AppColors.badgeFarmerBg;
      case UserRole.extensionOfficer:
        return AppColors.badgeOfficerBg;
      case UserRole.buyer:
        return AppColors.badgeBuyerBg;
      case UserRole.admin:
        return AppColors.badgeAdminBg;
    }
  }

  Color _getBadgeText(UserRole role) {
    switch (role) {
      case UserRole.farmer:
        return AppColors.badgeFarmerText;
      case UserRole.extensionOfficer:
        return AppColors.badgeOfficerText;
      case UserRole.buyer:
        return AppColors.badgeBuyerText;
      case UserRole.admin:
        return AppColors.badgeAdminText;
    }
  }

  void _showNotificationsSheet(BuildContext context) {
    showModalBottomSheet(
      context: context,
      isScrollControlled: true,
      backgroundColor: Colors.transparent,
      builder: (ctx) => _NotificationsBottomSheet(
        userId: _currentUser.id,
        onUpdated: _loadNotifications,
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final bodyContent = SingleChildScrollView(
      padding: const EdgeInsets.all(18),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          // Welcome Banner
          _buildWelcomeBanner(),
          const SizedBox(height: 20),

          // Metrics Cards
          _buildMetrics(),
          const SizedBox(height: 20),

          // Role Specific Functional Panel
          if (_currentUser.role == UserRole.farmer) _buildFarmerPanel(),
          if (_currentUser.role == UserRole.extensionOfficer)
            _buildOfficerPanel(),
          if (_currentUser.role == UserRole.buyer) _buildBuyerPanel(),
          if (_currentUser.role == UserRole.admin) _buildAdminPanel(),
        ],
      ),
    );

    if (widget.embeddedInShell) {
      return bodyContent;
    }

    return Scaffold(
      backgroundColor: AppColors.cream,
      appBar: AppBar(
        backgroundColor: AppColors.forest,
        title: Row(
          children: [
            const Icon(Icons.grass_rounded, color: AppColors.shoot, size: 24),
            const SizedBox(width: 8),
            const Text(
              'Kumburu',
              style: TextStyle(
                fontFamily: 'serif',
                fontWeight: FontWeight.bold,
                fontSize: 20,
              ),
            ),
          ],
        ),
        actions: [
          // Notification Bell with Badge
          ValueListenableBuilder<int>(
            valueListenable: NotificationService.unreadCountNotifier,
            builder: (context, unreadCount, child) {
              return Stack(
                alignment: Alignment.center,
                children: [
                  IconButton(
                    icon: const Icon(Icons.notifications_outlined, size: 24),
                    tooltip: 'Crop Activity Notifications',
                    onPressed: () => _showNotificationsSheet(context),
                  ),
                  if (unreadCount > 0)
                    Positioned(
                      top: 8,
                      right: 8,
                      child: Container(
                        padding: const EdgeInsets.symmetric(
                            horizontal: 5, vertical: 2),
                        decoration: BoxDecoration(
                          color: const Color(0xFFDC2626),
                          borderRadius: BorderRadius.circular(10),
                          border:
                              Border.all(color: AppColors.forest, width: 1.5),
                        ),
                        constraints: const BoxConstraints(
                          minWidth: 16,
                          minHeight: 16,
                        ),
                        child: Text(
                          unreadCount > 9 ? '9+' : '$unreadCount',
                          textAlign: TextAlign.center,
                          style: const TextStyle(
                            color: Colors.white,
                            fontSize: 9,
                            fontWeight: FontWeight.bold,
                          ),
                        ),
                      ),
                    ),
                ],
              );
            },
          ),
          const SizedBox(width: 4),
          Container(
            padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
            decoration: BoxDecoration(
              color: _getBadgeBg(_currentUser.role),
              borderRadius: BorderRadius.circular(9999),
              border: Border.all(
                color: _getBadgeText(_currentUser.role).withAlpha(100),
              ),
            ),
            child: Text(
              _currentUser.role.displayName.toUpperCase(),
              style: TextStyle(
                fontSize: 11,
                fontWeight: FontWeight.bold,
                color: _getBadgeText(_currentUser.role),
                letterSpacing: 0.6,
              ),
            ),
          ),
          const SizedBox(width: 8),
          IconButton(
            icon: const Icon(Icons.logout_rounded, size: 20),
            tooltip: 'Sign Out',
            onPressed: _handleLogout,
          ),
          const SizedBox(width: 8),
        ],
      ),
      body: SafeArea(
        child: bodyContent,
      ),
    );
  }

  Widget _buildWelcomeBanner() {
    String eyebrow = '';
    String title = '';
    String desc = '';

    switch (_currentUser.role) {
      case UserRole.farmer:
        eyebrow = 'FIELD DECISION WORKSPACE';
        title = 'Ayubowan, ${_currentUser.fullName.split(' ')[0]}!';
        desc =
            'Monitor cultivation cycles, diagnose field symptoms, and view approved advisory.';
        break;
      case UserRole.extensionOfficer:
        eyebrow = 'AGRICULTURAL SERVICE DIVISION';
        title = 'Officer: ${_currentUser.fullName}';
        desc =
            'Review AI-flagged pest & disease reports and authorize safe pesticide treatments.';
        break;
      case UserRole.buyer:
        eyebrow = 'HARVEST PROCUREMENT PORTAL';
        title = 'Procurement Hub: ${_currentUser.fullName}';
        desc =
            'Discover upcoming paddy harvests across divisions and connect directly with farmers.';
        break;
      case UserRole.admin:
        eyebrow = 'SYSTEM ADMINISTRATION';
        title = 'Console & User Management';
        desc =
            'Manage accounts saved in local storage and monitor PostgreSQL migration readiness.';
        break;
    }

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          eyebrow,
          style: const TextStyle(
            fontSize: 11,
            fontWeight: FontWeight.bold,
            letterSpacing: 1.2,
            color: AppColors.shoot,
          ),
        ),
        const SizedBox(height: 4),
        Text(
          title,
          style: const TextStyle(
            fontSize: 22,
            fontWeight: FontWeight.bold,
            color: AppColors.ink,
            fontFamily: 'serif',
          ),
        ),
        const SizedBox(height: 4),
        Text(
          desc,
          style: const TextStyle(
            fontSize: 13,
            color: AppColors.inkSoft,
          ),
        ),
      ],
    );
  }

  Widget _buildMetrics() {
    List<Widget> cards = [];

    switch (_currentUser.role) {
      case UserRole.farmer:
        cards = [
          _MetricCard(
            label: 'Cultivation Season',
            value: 'Yala 2026',
            sub: 'Day 45 (Tillering)',
          ),
          _MetricCard(
            label: 'Registered Field',
            value: '3.5 Acres',
            sub: 'Bg 352 (Nadu)',
          ),
          _MetricCard(
            label: 'Crop Health',
            value: 'Normal',
            sub: 'Last scan: 3d ago',
            valueColor: Colors.green[800],
          ),
          _MetricCard(
            label: 'Target Harvest',
            value: '14.0 MT',
            sub: 'Late August',
          ),
        ];
        break;
      case UserRole.extensionOfficer:
        cards = [
          _MetricCard(
            label: 'Division',
            value: _currentUser.division?.split(' - ').first ?? 'North Central',
            sub: 'Agrarian Centre',
          ),
          _MetricCard(
            label: 'Pending Reviews',
            value: '3',
            sub: 'Requires sign-off',
            valueColor: Colors.orange[800],
          ),
          _MetricCard(
            label: 'Approved Treatments',
            value: '42',
            sub: 'Current season',
          ),
          _MetricCard(
            label: 'Registered Farmers',
            value: '186',
            sub: 'Across 6 GN divisions',
          ),
        ];
        break;
      case UserRole.buyer:
        cards = [
          _MetricCard(
            label: 'Live Listings',
            value: '12 Lots',
            sub: 'North Central & East',
          ),
          _MetricCard(
            label: 'Active Offers',
            value: '3',
            sub: 'Pending confirmation',
          ),
          _MetricCard(
            label: 'Total Procured',
            value: '85 MT',
            sub: 'This season',
          ),
          _MetricCard(
            label: 'Avg Rate',
            value: 'LKR 115/kg',
            sub: 'Samba & Nadu',
          ),
        ];
        break;
      case UserRole.admin:
        cards = [
          _MetricCard(
            label: 'Stored Users',
            value: '${_usersList.length}',
            sub: 'In local storage',
          ),
          _MetricCard(
            label: 'Target Database',
            value: 'PostgreSQL',
            sub: 'database/schema.sql',
          ),
          _MetricCard(
            label: 'Backend Engine',
            value: 'ASP.NET Core',
            sub: 'PaddyWise.Api',
          ),
          _MetricCard(
            label: 'Storage Mode',
            value: 'Local/SharedPreferences',
            sub: 'Ready for API switch',
            valueColor: Colors.green[800],
          ),
        ];
        break;
    }

    return LayoutBuilder(
      builder: (context, constraints) {
        final isNarrow = constraints.maxWidth < 600;
        return Wrap(
          spacing: 12,
          runSpacing: 12,
          children: cards
              .map(
                (c) => SizedBox(
                  width: isNarrow
                      ? (constraints.maxWidth - 12) / 2
                      : (constraints.maxWidth - 36) / 4,
                  child: c,
                ),
              )
              .toList(),
        );
      },
    );
  }

  Widget _buildFarmerPanel() {
    final approvedList = _farmerNotifications
        .where((n) => n.status == NotificationStatus.approved)
        .toList();
    final rejectedList = _farmerNotifications
        .where((n) => n.status == NotificationStatus.rejected)
        .toList();
    final latestApproved = approvedList.isNotEmpty ? approvedList.first : null;
    final latestRejected = rejectedList.isNotEmpty ? rejectedList.first : null;

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        // AI Advisory & Officer Decision Card
        Container(
          padding: const EdgeInsets.all(18),
          decoration: BoxDecoration(
            color: Colors.white,
            borderRadius: BorderRadius.circular(12),
            border: Border.all(color: AppColors.line),
          ),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  Row(
                    children: const [
                      Icon(Icons.psychology_alt_rounded,
                          color: AppColors.forest, size: 24),
                      SizedBox(width: 8),
                      Text(
                        'Crop Activity AI Advisory',
                        style: TextStyle(
                          fontSize: 16,
                          fontWeight: FontWeight.bold,
                          color: AppColors.ink,
                          fontFamily: 'serif',
                        ),
                      ),
                    ],
                  ),
                  Container(
                    padding:
                        const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
                    decoration: BoxDecoration(
                      color: AppColors.badgeFarmerBg,
                      borderRadius: BorderRadius.circular(12),
                    ),
                    child: Row(
                      mainAxisSize: MainAxisSize.min,
                      children: const [
                        Icon(Icons.verified,
                            size: 12, color: AppColors.badgeFarmerText),
                        SizedBox(width: 4),
                        Text(
                          'DOA Official',
                          style: TextStyle(
                            fontSize: 10,
                            fontWeight: FontWeight.bold,
                            color: AppColors.badgeFarmerText,
                          ),
                        ),
                      ],
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 6),
              const Text(
                'AI-recommended field operations officially reviewed by your Agricultural Extension Officer.',
                style: TextStyle(fontSize: 12, color: AppColors.inkSoft),
              ),
              const SizedBox(height: 14),

              // Approved Recommendation Notice
              if (latestApproved != null) ...[
                Container(
                  padding: const EdgeInsets.all(14),
                  decoration: BoxDecoration(
                    color: const Color(0xFFF0FDF4),
                    borderRadius: BorderRadius.circular(10),
                    border: Border.all(color: const Color(0xFF86EFAC)),
                  ),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Row(
                        mainAxisAlignment: MainAxisAlignment.spaceBetween,
                        children: [
                          Container(
                            padding: const EdgeInsets.symmetric(
                                horizontal: 8, vertical: 3),
                            decoration: BoxDecoration(
                              color: const Color(0xFFDCFCE7),
                              borderRadius: BorderRadius.circular(6),
                            ),
                            child: Row(
                              mainAxisSize: MainAxisSize.min,
                              children: const [
                                Icon(Icons.check_circle_rounded,
                                    size: 14, color: Color(0xFF166534)),
                                SizedBox(width: 4),
                                Text(
                                  'OFFICER APPROVED',
                                  style: TextStyle(
                                    fontSize: 11,
                                    fontWeight: FontWeight.bold,
                                    color: Color(0xFF166534),
                                    letterSpacing: 0.5,
                                  ),
                                ),
                              ],
                            ),
                          ),
                          Text(
                            _formatTimeAgo(latestApproved.createdAt),
                            style: const TextStyle(
                              fontSize: 11,
                              color: AppColors.inkSoft,
                            ),
                          ),
                        ],
                      ),
                      const SizedBox(height: 8),
                      Text(
                        latestApproved.actionText ?? latestApproved.title,
                        style: const TextStyle(
                          fontSize: 14,
                          fontWeight: FontWeight.bold,
                          color: AppColors.ink,
                        ),
                      ),
                      const SizedBox(height: 6),
                      Container(
                        padding: const EdgeInsets.all(10),
                        decoration: BoxDecoration(
                          color: Colors.white.withAlpha(200),
                          borderRadius: BorderRadius.circular(6),
                          border: Border.all(
                              color: const Color(0xFF86EFAC).withAlpha(120)),
                        ),
                        child: Row(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            const Text('👨‍🌾 ', style: TextStyle(fontSize: 14)),
                            Expanded(
                              child: RichText(
                                text: TextSpan(
                                  style: const TextStyle(
                                      fontSize: 12, color: AppColors.ink),
                                  children: [
                                    TextSpan(
                                      text:
                                          '${latestApproved.officerName ?? "Agrarian Officer"}: ',
                                      style: const TextStyle(
                                          fontWeight: FontWeight.bold),
                                    ),
                                    TextSpan(
                                      text:
                                          '"${latestApproved.officerComment ?? latestApproved.message}"',
                                      style: const TextStyle(
                                          fontStyle: FontStyle.italic),
                                    ),
                                  ],
                                ),
                              ),
                            ),
                          ],
                        ),
                      ),
                      const SizedBox(height: 10),
                      Row(
                        children: const [
                          Icon(Icons.task_alt,
                              size: 16, color: Color(0xFF166534)),
                          SizedBox(width: 6),
                          Text(
                            'Authorized for field application',
                            style: TextStyle(
                              fontSize: 11,
                              fontWeight: FontWeight.w600,
                              color: Color(0xFF166534),
                            ),
                          ),
                        ],
                      ),
                    ],
                  ),
                ),
                const SizedBox(height: 12),
              ],

              // Rejected Recommendation Notice
              if (latestRejected != null) ...[
                Container(
                  padding: const EdgeInsets.all(14),
                  decoration: BoxDecoration(
                    color: const Color(0xFFFEF2F2),
                    borderRadius: BorderRadius.circular(10),
                    border: Border.all(color: const Color(0xFFFECACA)),
                  ),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Row(
                        mainAxisAlignment: MainAxisAlignment.spaceBetween,
                        children: [
                          Container(
                            padding: const EdgeInsets.symmetric(
                                horizontal: 8, vertical: 3),
                            decoration: BoxDecoration(
                              color: const Color(0xFFFEE2E2),
                              borderRadius: BorderRadius.circular(6),
                            ),
                            child: Row(
                              mainAxisSize: MainAxisSize.min,
                              children: const [
                                Icon(Icons.cancel_rounded,
                                    size: 14, color: Color(0xFF991B1B)),
                                SizedBox(width: 4),
                                Text(
                                  'OFFICER REJECTED (DO NOT APPLY)',
                                  style: TextStyle(
                                    fontSize: 11,
                                    fontWeight: FontWeight.bold,
                                    color: Color(0xFF991B1B),
                                    letterSpacing: 0.5,
                                  ),
                                ),
                              ],
                            ),
                          ),
                          Text(
                            _formatTimeAgo(latestRejected.createdAt),
                            style: const TextStyle(
                              fontSize: 11,
                              color: AppColors.inkSoft,
                            ),
                          ),
                        ],
                      ),
                      const SizedBox(height: 8),
                      Text(
                        latestRejected.actionText ?? latestRejected.title,
                        style: const TextStyle(
                          fontSize: 14,
                          fontWeight: FontWeight.bold,
                          color: AppColors.ink,
                        ),
                      ),
                      const SizedBox(height: 6),
                      Container(
                        padding: const EdgeInsets.all(10),
                        decoration: BoxDecoration(
                          color: Colors.white.withAlpha(200),
                          borderRadius: BorderRadius.circular(6),
                          border: Border.all(
                              color: const Color(0xFFFECACA).withAlpha(120)),
                        ),
                        child: Row(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            const Text('⚠️ ', style: TextStyle(fontSize: 14)),
                            Expanded(
                              child: RichText(
                                text: TextSpan(
                                  style: const TextStyle(
                                      fontSize: 12, color: AppColors.ink),
                                  children: [
                                    TextSpan(
                                      text:
                                          '${latestRejected.officerName ?? "Agrarian Officer"}: ',
                                      style: const TextStyle(
                                          fontWeight: FontWeight.bold),
                                    ),
                                    TextSpan(
                                      text:
                                          '"${latestRejected.officerComment ?? latestRejected.message}"',
                                      style: const TextStyle(
                                          fontStyle: FontStyle.italic),
                                    ),
                                  ],
                                ),
                              ),
                            ),
                          ],
                        ),
                      ),
                    ],
                  ),
                ),
                const SizedBox(height: 12),
              ],

              // Open Notifications Sheet Button
              SizedBox(
                width: double.infinity,
                child: OutlinedButton.icon(
                  onPressed: () => _showNotificationsSheet(context),
                  icon:
                      const Icon(Icons.notifications_active_outlined, size: 18),
                  label: Text(
                    'View All Crop Activity Notifications (${_farmerNotifications.length})',
                  ),
                ),
              ),
            ],
          ),
        ),
        const SizedBox(height: 18),

        // Leaf & Pest Symptom Scanner
        Container(
          padding: const EdgeInsets.all(18),
          decoration: BoxDecoration(
            color: Colors.white,
            borderRadius: BorderRadius.circular(12),
            border: Border.all(color: AppColors.line),
          ),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                children: const [
                  Icon(Icons.camera_alt_outlined,
                      color: AppColors.shoot, size: 22),
                  SizedBox(width: 8),
                  Text(
                    'Leaf & Pest Symptom Scanner',
                    style: TextStyle(
                      fontSize: 16,
                      fontWeight: FontWeight.bold,
                      color: AppColors.ink,
                      fontFamily: 'serif',
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 12),
              Container(
                padding: const EdgeInsets.all(12),
                decoration: BoxDecoration(
                  color: AppColors.creamDeep.withAlpha(120),
                  borderRadius: BorderRadius.circular(8),
                  border: Border.all(color: AppColors.line),
                ),
                child: const Text(
                  'Notice discolored leaves or hopper burn? Snap a photo in the field. The 4-agent AI diagnosis pipeline will analyze it and submit to your local Extension Officer for approval.',
                  style: TextStyle(fontSize: 13, color: AppColors.inkSoft),
                ),
              ),
              const SizedBox(height: 16),
              Center(
                child: ElevatedButton.icon(
                  onPressed: () {
                    ScaffoldMessenger.of(context).showSnackBar(
                      const SnackBar(
                        content: Text(
                          'AI Diagnosis Scanner will link to mobile camera model pipeline.',
                        ),
                      ),
                    );
                  },
                  icon: const Icon(Icons.photo_camera),
                  label: const Text('Simulate Symptom Scan'),
                ),
              ),
            ],
          ),
        ),
      ],
    );
  }

  Widget _buildOfficerPanel() {
    return Container(
      padding: const EdgeInsets.all(18),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(12),
        border: Border.all(color: AppColors.line),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: const [
              Icon(Icons.verified_user_outlined,
                  color: AppColors.forest, size: 22),
              SizedBox(width: 8),
              Text(
                'Pending Treatment Authorization',
                style: TextStyle(
                  fontSize: 16,
                  fontWeight: FontWeight.bold,
                  color: AppColors.ink,
                  fontFamily: 'serif',
                ),
              ),
            ],
          ),
          const SizedBox(height: 14),
          Container(
            padding: const EdgeInsets.all(14),
            decoration: BoxDecoration(
              border: Border.all(color: AppColors.line),
              borderRadius: BorderRadius.circular(8),
            ),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    const Text(
                      'Farmer: Bandara Wanninayake',
                      style: TextStyle(
                        fontWeight: FontWeight.bold,
                        fontSize: 14,
                        color: AppColors.ink,
                      ),
                    ),
                    Container(
                      padding: const EdgeInsets.symmetric(
                          horizontal: 8, vertical: 2),
                      decoration: BoxDecoration(
                        color: AppColors.badgeFarmerBg,
                        borderRadius: BorderRadius.circular(4),
                      ),
                      child: const Text(
                        'Medirigiriya',
                        style: TextStyle(
                          fontSize: 11,
                          color: AppColors.badgeFarmerText,
                          fontWeight: FontWeight.bold,
                        ),
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: 8),
                const Text(
                  'Symptom: Hopper burn patches in 0.5 acre paddy plot.',
                  style: TextStyle(fontSize: 12, color: AppColors.inkSoft),
                ),
                const Text(
                  'AI Diagnosis: Brown Plant Hopper (96% confidence).',
                  style: TextStyle(fontSize: 12, color: AppColors.inkSoft),
                ),
                const SizedBox(height: 4),
                const Text(
                  'Proposed: Drain field 3 days; apply approved Thiamethoxam 25% WG.',
                  style: TextStyle(
                    fontSize: 12,
                    fontWeight: FontWeight.w600,
                    color: AppColors.shoot,
                  ),
                ),
                const SizedBox(height: 12),
                Wrap(
                  spacing: 8,
                  runSpacing: 8,
                  children: [
                    ElevatedButton.icon(
                      style: ElevatedButton.styleFrom(
                        backgroundColor: const Color(0xFF16A34A),
                        foregroundColor: Colors.white,
                        padding: const EdgeInsets.symmetric(
                            horizontal: 14, vertical: 8),
                      ),
                      onPressed: () async {
                        await NotificationService.addNotification(
                          NotificationItem(
                            id: 'notif_${DateTime.now().millisecondsSinceEpoch}',
                            userId: 'usr_farmer_01',
                            title: 'Crop Activity Recommendation Approved',
                            message:
                                'Agricultural Officer ${_currentUser.fullName} approved: "Drain field 3 days; apply approved Thiamethoxam 25% WG". Advice: "Treatment authorized as per DOA guidelines. Ensure protective equipment."',
                            type: 'CropActivityReview',
                            status: NotificationStatus.approved,
                            relatedCycleId: 1,
                            relatedRecommendationId: 104,
                            officerName: _currentUser.fullName,
                            officerComment:
                                'Treatment authorized as per DOA guidelines. Drain field 3 days prior; ensure personal protective gear is worn during application.',
                            actionText:
                                'Drain field 3 days; apply approved Thiamethoxam 25% WG',
                            isRead: false,
                            createdAt: DateTime.now(),
                          ),
                        );
                        if (mounted) {
                          ScaffoldMessenger.of(context).showSnackBar(
                            const SnackBar(
                              content: Text(
                                'Treatment approved! Notification delivered to Farmer Bandara Wanninayake.',
                              ),
                            ),
                          );
                        }
                      },
                      icon: const Icon(Icons.check, size: 16),
                      label: const Text('Approve Treatment',
                          style: TextStyle(fontSize: 13)),
                    ),
                    OutlinedButton.icon(
                      style: OutlinedButton.styleFrom(
                        foregroundColor: AppColors.errorText,
                        side: const BorderSide(color: AppColors.errorText),
                        padding: const EdgeInsets.symmetric(
                            horizontal: 14, vertical: 8),
                      ),
                      onPressed: () async {
                        await NotificationService.addNotification(
                          NotificationItem(
                            id: 'notif_${DateTime.now().millisecondsSinceEpoch}',
                            userId: 'usr_farmer_01',
                            title: 'Crop Activity Recommendation Rejected',
                            message:
                                'Agricultural Officer ${_currentUser.fullName} advised not to proceed with: "Drain field 3 days; apply Thiamethoxam 25% WG". Note: "Rejected: Hopper count is below threshold. Do not apply chemical spray; monitor water levels."',
                            type: 'CropActivityReview',
                            status: NotificationStatus.rejected,
                            relatedCycleId: 1,
                            relatedRecommendationId: 105,
                            officerName: _currentUser.fullName,
                            officerComment:
                                'Rejected: Hopper count is below economic threshold. Do not apply chemical spray; maintain 2cm water level and conserve natural predators.',
                            actionText:
                                'Drain field 3 days; apply approved Thiamethoxam 25% WG',
                            isRead: false,
                            createdAt: DateTime.now(),
                          ),
                        );
                        if (mounted) {
                          ScaffoldMessenger.of(context).showSnackBar(
                            const SnackBar(
                              content: Text(
                                'Treatment rejected. IPM warning notification delivered to Farmer.',
                              ),
                            ),
                          );
                        }
                      },
                      icon: const Icon(Icons.close, size: 16),
                      label: const Text('Reject Treatment',
                          style: TextStyle(fontSize: 13)),
                    ),
                    OutlinedButton(
                      style: OutlinedButton.styleFrom(
                        padding: const EdgeInsets.symmetric(
                            horizontal: 14, vertical: 8),
                      ),
                      onPressed: () {
                        ScaffoldMessenger.of(context).showSnackBar(
                          const SnackBar(
                            content: Text(
                              'Requested secondary field sample from farmer.',
                            ),
                          ),
                        );
                      },
                      child: const Text('Request Info',
                          style: TextStyle(fontSize: 13)),
                    ),
                  ],
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildBuyerPanel() {
    return Container(
      padding: const EdgeInsets.all(18),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(12),
        border: Border.all(color: AppColors.line),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: const [
              Icon(Icons.shopping_bag_outlined,
                  color: AppColors.goldDeep, size: 22),
              SizedBox(width: 8),
              Text(
                'Available Harvest Listings Nearby',
                style: TextStyle(
                  fontSize: 16,
                  fontWeight: FontWeight.bold,
                  color: AppColors.ink,
                  fontFamily: 'serif',
                ),
              ),
            ],
          ),
          const SizedBox(height: 14),
          _ListingTile(
            farmer: 'Bandara Wanninayake',
            variety: 'Bg 352 (Nadu)',
            quantity: '14 Metric Tons',
            location: 'Medirigiriya',
            harvestDate: 'Aug 20, 2026',
            onPlaceOffer: () {
              ScaffoldMessenger.of(context).showSnackBar(
                const SnackBar(
                  content: Text('Offer dialog will open in procurement module.'),
                ),
              );
            },
          ),
          const SizedBox(height: 10),
          _ListingTile(
            farmer: 'Sunil Jayasuriya',
            variety: 'At 362 (Red Samba)',
            quantity: '9.5 Metric Tons',
            location: 'Tambuttegama',
            harvestDate: 'Aug 28, 2026',
            onPlaceOffer: () {
              ScaffoldMessenger.of(context).showSnackBar(
                const SnackBar(
                  content: Text('Offer dialog will open in procurement module.'),
                ),
              );
            },
          ),
        ],
      ),
    );
  }

  Widget _buildAdminPanel() {
    return Container(
      padding: const EdgeInsets.all(18),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(12),
        border: Border.all(color: AppColors.line),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Row(
                children: [
                  const Icon(Icons.people_alt_outlined,
                      color: AppColors.forest, size: 22),
                  const SizedBox(width: 8),
                  Text(
                    'Stored User Accounts (${_usersList.length})',
                    style: const TextStyle(
                      fontSize: 16,
                      fontWeight: FontWeight.bold,
                      color: AppColors.ink,
                      fontFamily: 'serif',
                    ),
                  ),
                ],
              ),
              TextButton.icon(
                onPressed: _resetDefaults,
                icon: const Icon(Icons.refresh, size: 16),
                label: const Text('Reset Seeds', style: TextStyle(fontSize: 12)),
              ),
            ],
          ),
          const SizedBox(height: 12),
          ..._usersList.map(
            (u) => Container(
              margin: const EdgeInsets.only(bottom: 8),
              padding: const EdgeInsets.all(12),
              decoration: BoxDecoration(
                color: AppColors.creamDeep.withAlpha(70),
                borderRadius: BorderRadius.circular(8),
                border: Border.all(color: AppColors.line),
              ),
              child: Row(
                children: [
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          u.fullName,
                          style: const TextStyle(
                            fontWeight: FontWeight.w600,
                            fontSize: 13,
                            color: AppColors.ink,
                          ),
                        ),
                        Text(
                          '${u.email} • ${u.division ?? 'No division'}',
                          style: const TextStyle(
                            fontSize: 11,
                            color: AppColors.inkSoft,
                          ),
                        ),
                      ],
                    ),
                  ),
                  Container(
                    padding:
                        const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
                    decoration: BoxDecoration(
                      color: _getBadgeBg(u.role),
                      borderRadius: BorderRadius.circular(4),
                    ),
                    child: Text(
                      u.role.displayName,
                      style: TextStyle(
                        fontSize: 10,
                        fontWeight: FontWeight.bold,
                        color: _getBadgeText(u.role),
                      ),
                    ),
                  ),
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }
}

class _MetricCard extends StatelessWidget {
  final String label;
  final String value;
  final String sub;
  final Color? valueColor;

  const _MetricCard({
    required this.label,
    required this.value,
    required this.sub,
    this.valueColor,
  });

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(14),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(10),
        border: Border.all(color: AppColors.line),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            label.toUpperCase(),
            style: const TextStyle(
              fontSize: 10,
              fontWeight: FontWeight.bold,
              letterSpacing: 0.5,
              color: AppColors.inkSoft,
            ),
          ),
          const SizedBox(height: 6),
          Text(
            value,
            style: TextStyle(
              fontSize: 17,
              fontWeight: FontWeight.bold,
              color: valueColor ?? AppColors.ink,
              fontFamily: 'serif',
            ),
          ),
          const SizedBox(height: 2),
          Text(
            sub,
            style: const TextStyle(
              fontSize: 11,
              color: AppColors.shoot,
            ),
          ),
        ],
      ),
    );
  }
}

class _ListingTile extends StatelessWidget {
  final String farmer;
  final String variety;
  final String quantity;
  final String location;
  final String harvestDate;
  final VoidCallback onPlaceOffer;

  const _ListingTile({
    required this.farmer,
    required this.variety,
    required this.quantity,
    required this.location,
    required this.harvestDate,
    required this.onPlaceOffer,
  });

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(12),
      decoration: BoxDecoration(
        border: Border.all(color: AppColors.line),
        borderRadius: BorderRadius.circular(8),
      ),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.spaceBetween,
        children: [
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  farmer,
                  style: const TextStyle(
                    fontWeight: FontWeight.bold,
                    fontSize: 13,
                    color: AppColors.ink,
                  ),
                ),
                Text(
                  '$variety • $quantity',
                  style: const TextStyle(
                    fontSize: 12,
                    color: AppColors.inkSoft,
                  ),
                ),
                Text(
                  '$location • Harvest: $harvestDate',
                  style: const TextStyle(
                    fontSize: 11,
                    color: AppColors.shoot,
                  ),
                ),
              ],
            ),
          ),
          ElevatedButton(
            style: ElevatedButton.styleFrom(
              padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
            ),
            onPressed: onPlaceOffer,
            child: const Text('Place Offer', style: TextStyle(fontSize: 12)),
          ),
        ],
      ),
    );
  }
}

String _formatTimeAgo(DateTime dt) {
  final diff = DateTime.now().difference(dt);
  if (diff.inMinutes < 1) return 'Just now';
  if (diff.inMinutes < 60) return '${diff.inMinutes}m ago';
  if (diff.inHours < 24) return '${diff.inHours}h ago';
  if (diff.inDays < 7) return '${diff.inDays}d ago';
  return '${dt.day}/${dt.month}/${dt.year}';
}

class _NotificationsBottomSheet extends StatefulWidget {
  final String userId;
  final VoidCallback onUpdated;

  const _NotificationsBottomSheet({
    required this.userId,
    required this.onUpdated,
  });

  @override
  State<_NotificationsBottomSheet> createState() =>
      _NotificationsBottomSheetState();
}

class _NotificationsBottomSheetState extends State<_NotificationsBottomSheet> {
  String _selectedFilter = 'all'; // 'all', 'approved', 'rejected'

  @override
  Widget build(BuildContext context) {
    final allNotifications =
        NotificationService.getNotificationsForUser(widget.userId);
    final approvedCount = allNotifications
        .where((n) => n.status == NotificationStatus.approved)
        .length;
    final rejectedCount = allNotifications
        .where((n) => n.status == NotificationStatus.rejected)
        .length;

    List<NotificationItem> filteredList;
    if (_selectedFilter == 'approved') {
      filteredList = allNotifications
          .where((n) => n.status == NotificationStatus.approved)
          .toList();
    } else if (_selectedFilter == 'rejected') {
      filteredList = allNotifications
          .where((n) => n.status == NotificationStatus.rejected)
          .toList();
    } else {
      filteredList = allNotifications;
    }

    final unreadCount = NotificationService.getUnreadCount(widget.userId);

    return Container(
      decoration: const BoxDecoration(
        color: AppColors.cream,
        borderRadius: BorderRadius.vertical(top: Radius.circular(24)),
      ),
      constraints: BoxConstraints(
        maxHeight: MediaQuery.of(context).size.height * 0.88,
      ),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          // Drag Handle
          const SizedBox(height: 12),
          Container(
            width: 44,
            height: 4,
            decoration: BoxDecoration(
              color: AppColors.inkSoft.withAlpha(80),
              borderRadius: BorderRadius.circular(2),
            ),
          ),
          const SizedBox(height: 12),

          // Header
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 20),
            child: Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                Row(
                  children: [
                    Container(
                      padding: const EdgeInsets.all(8),
                      decoration: BoxDecoration(
                        color: AppColors.forest.withAlpha(20),
                        borderRadius: BorderRadius.circular(10),
                      ),
                      child: const Icon(
                        Icons.notifications_active_rounded,
                        color: AppColors.forest,
                        size: 22,
                      ),
                    ),
                    const SizedBox(width: 10),
                    Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        const Text(
                          'Field Notifications',
                          style: TextStyle(
                            fontFamily: 'serif',
                            fontSize: 18,
                            fontWeight: FontWeight.bold,
                            color: AppColors.ink,
                          ),
                        ),
                        Text(
                          '$unreadCount unread advice notice${unreadCount == 1 ? "" : "s"}',
                          style: const TextStyle(
                            fontSize: 12,
                            color: AppColors.inkSoft,
                          ),
                        ),
                      ],
                    ),
                  ],
                ),
                if (unreadCount > 0)
                  TextButton.icon(
                    onPressed: () async {
                      await NotificationService.markAllAsRead(widget.userId);
                      widget.onUpdated();
                      setState(() {});
                    },
                    icon: const Icon(Icons.done_all, size: 16),
                    label: const Text('Mark all read',
                        style: TextStyle(fontSize: 12)),
                  ),
              ],
            ),
          ),
          const SizedBox(height: 14),

          // Filter Chips
          SingleChildScrollView(
            scrollDirection: Axis.horizontal,
            padding: const EdgeInsets.symmetric(horizontal: 20),
            child: Row(
              children: [
                _buildFilterChip('all', 'All (${allNotifications.length})'),
                const SizedBox(width: 8),
                _buildFilterChip('approved', 'Approved ($approvedCount)',
                    icon: Icons.check_circle,
                    activeColor: const Color(0xFF166534)),
                const SizedBox(width: 8),
                _buildFilterChip('rejected', 'Rejected ($rejectedCount)',
                    icon: Icons.cancel,
                    activeColor: const Color(0xFF991B1B)),
              ],
            ),
          ),
          const SizedBox(height: 10),
          const Divider(height: 1, color: AppColors.line),

          // Notification List
          Expanded(
            child: filteredList.isEmpty
                ? Center(
                    child: Padding(
                      padding: const EdgeInsets.all(32),
                      child: Column(
                        mainAxisSize: MainAxisSize.min,
                        children: [
                          Icon(
                            Icons.notifications_off_outlined,
                            size: 48,
                            color: AppColors.inkSoft.withAlpha(120),
                          ),
                          const SizedBox(height: 12),
                          const Text(
                            'No notifications in this category',
                            style: TextStyle(
                              fontSize: 14,
                              fontWeight: FontWeight.bold,
                              color: AppColors.ink,
                            ),
                          ),
                          const SizedBox(height: 4),
                          const Text(
                            'Approved and rejected AI recommendations will display here.',
                            textAlign: TextAlign.center,
                            style: TextStyle(
                              fontSize: 12,
                              color: AppColors.inkSoft,
                            ),
                          ),
                        ],
                      ),
                    ),
                  )
                : ListView.builder(
                    padding: const EdgeInsets.all(16),
                    itemCount: filteredList.length,
                    itemBuilder: (context, index) {
                      final item = filteredList[index];
                      return _NotificationCard(
                        item: item,
                        onMarkRead: () async {
                          await NotificationService.markAsRead(item.id,
                              userId: widget.userId);
                          widget.onUpdated();
                          setState(() {});
                        },
                      );
                    },
                  ),
          ),
        ],
      ),
    );
  }

  Widget _buildFilterChip(String key, String label,
      {IconData? icon, Color? activeColor}) {
    final isSelected = _selectedFilter == key;
    return ChoiceChip(
      selected: isSelected,
      showCheckmark: false,
      avatar: icon != null
          ? Icon(icon,
              size: 14,
              color: isSelected
                  ? Colors.white
                  : (activeColor ?? AppColors.inkSoft))
          : null,
      label: Text(label),
      labelStyle: TextStyle(
        fontSize: 12,
        fontWeight: isSelected ? FontWeight.bold : FontWeight.normal,
        color: isSelected ? Colors.white : AppColors.ink,
      ),
      selectedColor: activeColor ?? AppColors.forest,
      backgroundColor: Colors.white,
      side: BorderSide(
        color: isSelected ? Colors.transparent : AppColors.line,
      ),
      onSelected: (val) {
        if (val) {
          setState(() {
            _selectedFilter = key;
          });
        }
      },
    );
  }
}

class _NotificationCard extends StatelessWidget {
  final NotificationItem item;
  final VoidCallback onMarkRead;

  const _NotificationCard({
    required this.item,
    required this.onMarkRead,
  });

  @override
  Widget build(BuildContext context) {
    Color borderColor;
    Color statusBg;
    Color statusTextColor;
    IconData statusIcon;
    String statusTitle;

    switch (item.status) {
      case NotificationStatus.approved:
        borderColor = const Color(0xFF16A34A);
        statusBg = const Color(0xFFDCFCE7);
        statusTextColor = const Color(0xFF166534);
        statusIcon = Icons.check_circle_rounded;
        statusTitle = 'OFFICER APPROVED';
        break;
      case NotificationStatus.rejected:
        borderColor = const Color(0xFFDC2626);
        statusBg = const Color(0xFFFEE2E2);
        statusTextColor = const Color(0xFF991B1B);
        statusIcon = Icons.cancel_rounded;
        statusTitle = 'OFFICER REJECTED';
        break;
      case NotificationStatus.pending:
      default:
        borderColor = const Color(0xFFD97706);
        statusBg = const Color(0xFFFEF3C7);
        statusTextColor = const Color(0xFF92400E);
        statusIcon = Icons.hourglass_top_rounded;
        statusTitle = 'PENDING REVIEW';
        break;
    }

    return Container(
      margin: const EdgeInsets.only(bottom: 14),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(12),
        border: Border.all(
          color: item.isRead ? AppColors.line : borderColor.withAlpha(120),
          width: item.isRead ? 1 : 1.5,
        ),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withAlpha(10),
            blurRadius: 4,
            offset: const Offset(0, 2),
          ),
        ],
      ),
      child: ClipRRect(
        borderRadius: BorderRadius.circular(12),
        child: Container(
          decoration: BoxDecoration(
            border: Border(
              left: BorderSide(color: borderColor, width: 4),
            ),
          ),
          padding: const EdgeInsets.all(14),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              // Top Row: Status badge & time
              Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  Container(
                    padding:
                        const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
                    decoration: BoxDecoration(
                      color: statusBg,
                      borderRadius: BorderRadius.circular(6),
                    ),
                    child: Row(
                      mainAxisSize: MainAxisSize.min,
                      children: [
                        Icon(statusIcon, size: 13, color: statusTextColor),
                        const SizedBox(width: 4),
                        Text(
                          statusTitle,
                          style: TextStyle(
                            fontSize: 10,
                            fontWeight: FontWeight.bold,
                            color: statusTextColor,
                            letterSpacing: 0.5,
                          ),
                        ),
                      ],
                    ),
                  ),
                  Row(
                    children: [
                      if (!item.isRead) ...[
                        Container(
                          width: 8,
                          height: 8,
                          decoration: const BoxDecoration(
                            color: Color(0xFFDC2626),
                            shape: BoxShape.circle,
                          ),
                        ),
                        const SizedBox(width: 6),
                      ],
                      Text(
                        _formatTimeAgo(item.createdAt),
                        style: const TextStyle(
                          fontSize: 11,
                          color: AppColors.inkSoft,
                        ),
                      ),
                    ],
                  ),
                ],
              ),
              const SizedBox(height: 10),

              // Title
              Text(
                item.title,
                style: const TextStyle(
                  fontSize: 14,
                  fontWeight: FontWeight.bold,
                  color: AppColors.ink,
                ),
              ),
              const SizedBox(height: 6),

              // Recommended / Proposed Action
              if (item.actionText != null && item.actionText!.isNotEmpty) ...[
                Container(
                  width: double.infinity,
                  padding: const EdgeInsets.all(10),
                  decoration: BoxDecoration(
                    color: AppColors.cream.withAlpha(140),
                    borderRadius: BorderRadius.circular(8),
                    border: Border.all(color: AppColors.line),
                  ),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      const Text(
                        'PROPOSED ACTIVITY',
                        style: TextStyle(
                          fontSize: 9,
                          fontWeight: FontWeight.bold,
                          letterSpacing: 0.6,
                          color: AppColors.inkSoft,
                        ),
                      ),
                      const SizedBox(height: 2),
                      Text(
                        item.actionText!,
                        style: const TextStyle(
                          fontSize: 13,
                          fontWeight: FontWeight.w600,
                          color: AppColors.ink,
                        ),
                      ),
                    ],
                  ),
                ),
                const SizedBox(height: 10),
              ],

              // Officer Guidance Note
              Container(
                width: double.infinity,
                padding: const EdgeInsets.all(10),
                decoration: BoxDecoration(
                  color: const Color(0xFFF8FAFC),
                  borderRadius: BorderRadius.circular(8),
                  border: Border.all(color: const Color(0xFFE2E8F0)),
                ),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Row(
                      children: [
                        const Icon(Icons.person_pin_rounded,
                            size: 15, color: AppColors.forest),
                        const SizedBox(width: 4),
                        Text(
                          'Reviewed by ${item.officerName ?? "Agricultural Officer"}',
                          style: const TextStyle(
                            fontSize: 11,
                            fontWeight: FontWeight.bold,
                            color: AppColors.forest,
                          ),
                        ),
                      ],
                    ),
                    const SizedBox(height: 4),
                    Text(
                      '"${item.officerComment ?? item.message}"',
                      style: const TextStyle(
                        fontSize: 12,
                        fontStyle: FontStyle.italic,
                        color: AppColors.ink,
                      ),
                    ),
                  ],
                ),
              ),
              const SizedBox(height: 12),

              // Footer Buttons
              Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  if (item.status == NotificationStatus.approved)
                    Row(
                      children: const [
                        Icon(Icons.check, size: 14, color: Color(0xFF166534)),
                        SizedBox(width: 4),
                        Text(
                          'DOA Certified Guidance',
                          style: TextStyle(
                            fontSize: 11,
                            fontWeight: FontWeight.w600,
                            color: Color(0xFF166534),
                          ),
                        ),
                      ],
                    )
                  else if (item.status == NotificationStatus.rejected)
                    Row(
                      children: const [
                        Icon(Icons.shield_outlined,
                            size: 14, color: Color(0xFF991B1B)),
                        SizedBox(width: 4),
                        Text(
                          'IPM Safety Hold',
                          style: TextStyle(
                            fontSize: 11,
                            fontWeight: FontWeight.w600,
                            color: Color(0xFF991B1B),
                          ),
                        ),
                      ],
                    )
                  else
                    const SizedBox.shrink(),

                  if (!item.isRead)
                    TextButton(
                      style: TextButton.styleFrom(
                        padding: const EdgeInsets.symmetric(
                            horizontal: 10, vertical: 4),
                        minimumSize: Size.zero,
                        tapTargetSize: MaterialTapTargetSize.shrinkWrap,
                      ),
                      onPressed: onMarkRead,
                      child: const Text(
                        'Mark as read',
                        style: TextStyle(fontSize: 11),
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
}

