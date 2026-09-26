import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import '../theme/app_theme.dart';
import '../models/user.dart';
import '../services/auth_service.dart';
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

  @override
  void initState() {
    super.initState();
    _currentUser = widget.user ?? AuthService.getCurrentUser()!;
    if (_currentUser.role == UserRole.admin) {
      _usersList = AuthService.getAllUsers();
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

  @override
  Widget build(BuildContext context) {
    final bodyContent = SingleChildScrollView(
      padding: const EdgeInsets.all(18),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          // Welcome Banner
          _buildWelcomeBanner(),
          const SizedBox(height: 18),

          // Farmer Quick Action to Record Activity
          if (_currentUser.role == UserRole.farmer) ...[
            _buildFarmerQuickActions(),
            const SizedBox(height: 18),
          ],

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

  Widget _buildFarmerQuickActions() {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 14),
      decoration: BoxDecoration(
        color: AppColors.forest,
        borderRadius: BorderRadius.circular(12),
        boxShadow: [
          BoxShadow(
            color: AppColors.forest.withAlpha(45),
            blurRadius: 10,
            offset: const Offset(0, 4),
          ),
        ],
      ),
      child: Row(
        children: [
          Container(
            padding: const EdgeInsets.all(10),
            decoration: BoxDecoration(
              color: AppColors.gold,
              borderRadius: BorderRadius.circular(10),
            ),
            child: const Icon(Icons.add_task_rounded, color: AppColors.forestDeep, size: 22),
          ),
          const SizedBox(width: 14),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: const [
                Text(
                  'Record Crop Activity',
                  style: TextStyle(
                    fontSize: 15,
                    fontWeight: FontWeight.bold,
                    color: AppColors.cream,
                    fontFamily: 'serif',
                  ),
                ),
                SizedBox(height: 2),
                Text(
                  'Fertilizer, irrigation, sprays & farm tasks',
                  style: TextStyle(
                    fontSize: 12,
                    color: AppColors.shootLight,
                  ),
                ),
              ],
            ),
          ),
          ElevatedButton(
            onPressed: () => context.push('/activities/new'),
            style: ElevatedButton.styleFrom(
              backgroundColor: AppColors.gold,
              foregroundColor: AppColors.forestDeep,
              padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 10),
              minimumSize: Size.zero,
              tapTargetSize: MaterialTapTargetSize.shrinkWrap,
            ),
            child: const Text('Add Now', style: TextStyle(fontSize: 12, fontWeight: FontWeight.bold)),
          ),
        ],
      ),
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
            sub: 'Day 45 (Tillering) • Tap to Log',
            onTap: () => context.push('/activities/new'),
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
              Icon(Icons.camera_alt_outlined, color: AppColors.shoot, size: 22),
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
          const SizedBox(height: 20),
          const Divider(height: 1, color: AppColors.line),
          const SizedBox(height: 16),
          Row(
            children: const [
              Icon(Icons.edit_calendar_rounded, color: AppColors.forest, size: 22),
              SizedBox(width: 8),
              Text(
                'Crop Activity Tracking',
                style: TextStyle(
                  fontSize: 16,
                  fontWeight: FontWeight.bold,
                  color: AppColors.ink,
                  fontFamily: 'serif',
                ),
              ),
            ],
          ),
          const SizedBox(height: 8),
          const Text(
            'Record fertilizer dosages, irrigation cycles, pesticide sprays, and farm operations within the 7-day compliance window.',
            style: TextStyle(fontSize: 13, color: AppColors.inkSoft),
          ),
          const SizedBox(height: 14),
          SizedBox(
            width: double.infinity,
            child: ElevatedButton.icon(
              onPressed: () {
                context.push('/activities/new');
              },
              icon: const Icon(Icons.add_task_rounded, size: 18),
              label: const Text('Record Activity', style: TextStyle(fontSize: 14, fontWeight: FontWeight.bold)),
              style: ElevatedButton.styleFrom(
                backgroundColor: AppColors.forest,
                foregroundColor: AppColors.cream,
                padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 14),
              ),
            ),
          ),
        ],
      ),
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
                Row(
                  children: [
                    ElevatedButton(
                      style: ElevatedButton.styleFrom(
                        padding: const EdgeInsets.symmetric(
                            horizontal: 16, vertical: 8),
                      ),
                      onPressed: () {
                        ScaffoldMessenger.of(context).showSnackBar(
                          const SnackBar(
                            content: Text(
                              'Treatment plan approved! Notification sent to farmer.',
                            ),
                          ),
                        );
                      },
                      child: const Text('Approve Treatment',
                          style: TextStyle(fontSize: 13)),
                    ),
                    const SizedBox(width: 10),
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
  final VoidCallback? onTap;

  const _MetricCard({
    required this.label,
    required this.value,
    required this.sub,
    this.valueColor,
    this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    final card = Container(
      padding: const EdgeInsets.all(14),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(10),
        border: Border.all(color: AppColors.line),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
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
              if (onTap != null)
                const Icon(Icons.arrow_forward_ios_rounded, size: 10, color: AppColors.shoot),
            ],
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

    if (onTap != null) {
      return InkWell(
        onTap: onTap,
        borderRadius: BorderRadius.circular(10),
        child: card,
      );
    }
    return card;
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
