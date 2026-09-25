import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import '../../theme/app_theme.dart';
import '../../models/user.dart';
import '../../services/auth_service.dart';
import '../storage/user_storage.dart';
import 'role_menu_config.dart';

/// AppShell is the persistent scaffold wrapping every authenticated screen.
/// It renders a top AppBar and an interactive role-based Navigation Drawer
/// mirroring the Kumburu web application sidebar.
class AppShell extends StatelessWidget {
  final Widget child;

  const AppShell({
    super.key,
    required this.child,
  });

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

  Future<void> _handleLogout(BuildContext context) async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('Sign Out'),
        content: const Text('Are you sure you want to sign out of Kumburu?'),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(ctx, false),
            child: const Text('Cancel'),
          ),
          ElevatedButton(
            style: ElevatedButton.styleFrom(
              backgroundColor: AppColors.errorText,
              foregroundColor: Colors.white,
            ),
            onPressed: () => Navigator.pop(ctx, true),
            child: const Text('Sign Out'),
          ),
        ],
      ),
    );

    if (confirmed == true && context.mounted) {
      await AuthService.logout();
      if (context.mounted) {
        context.go('/login');
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final currentUser = UserStorage.getCurrentUser();
    final roleString = UserStorage.getCurrentUserRole() ?? 'Farmer';
    final menuItems = getMenuItemsForRole(roleString);
    final currentRoute = GoRouterState.of(context).uri.path;

    return Scaffold(
      backgroundColor: AppColors.cream,
      appBar: AppBar(
        backgroundColor: AppColors.forest,
        elevation: 0,
        leading: Builder(
          builder: (ctx) => IconButton(
            icon: const Icon(Icons.menu_rounded, color: AppColors.cream),
            tooltip: 'Open navigation menu',
            onPressed: () => Scaffold.of(ctx).openDrawer(),
          ),
        ),
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
                color: AppColors.cream,
              ),
            ),
          ],
        ),
        actions: [
          if (currentUser != null)
            Container(
              margin: const EdgeInsets.symmetric(vertical: 12),
              padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 2),
              decoration: BoxDecoration(
                color: _getBadgeBg(currentUser.role),
                borderRadius: BorderRadius.circular(16),
                border: Border.all(
                  color: _getBadgeText(currentUser.role).withAlpha(120),
                ),
              ),
              alignment: Alignment.center,
              child: Text(
                currentUser.role.displayName.toUpperCase(),
                style: TextStyle(
                  fontSize: 10,
                  fontWeight: FontWeight.bold,
                  color: _getBadgeText(currentUser.role),
                  letterSpacing: 0.5,
                ),
              ),
            ),
          const SizedBox(width: 8),
          IconButton(
            icon: const Icon(Icons.logout_rounded, size: 20, color: AppColors.cream),
            tooltip: 'Sign Out',
            onPressed: () => _handleLogout(context),
          ),
          const SizedBox(width: 6),
        ],
      ),
      drawer: Drawer(
        backgroundColor: Colors.white,
        child: Column(
          children: [
            // Drawer Header
            _buildDrawerHeader(context, currentUser),

            // Navigation items list
            Expanded(
              child: ListView(
                padding: const EdgeInsets.symmetric(vertical: 8, horizontal: 8),
                children: [
                  ...menuItems.map((item) {
                    final isActive = currentRoute == item.route;
                    return _buildMenuItemTile(
                      context: context,
                      item: item,
                      isActive: isActive,
                    );
                  }),
                ],
              ),
            ),

            const Divider(height: 1, color: AppColors.line),

            // Drawer Footer: Sign out & Support
            _buildDrawerFooter(context),
          ],
        ),
      ),
      body: SafeArea(
        child: child,
      ),
    );
  }

  Widget _buildDrawerHeader(BuildContext context, User? user) {
    final roleName = user?.role.displayName ?? 'Farmer';
    final fullName = user?.fullName ?? 'User';
    final email = user?.email ?? '';

    return Container(
      width: double.infinity,
      padding: EdgeInsets.only(
        top: MediaQuery.of(context).padding.top + 16,
        left: 16,
        right: 16,
        bottom: 16,
      ),
      decoration: const BoxDecoration(
        color: AppColors.forest,
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              CircleAvatar(
                radius: 26,
                backgroundColor: AppColors.gold,
                child: Text(
                  fullName.isNotEmpty ? fullName[0].toUpperCase() : 'K',
                  style: const TextStyle(
                    fontSize: 22,
                    fontWeight: FontWeight.bold,
                    color: AppColors.forestDeep,
                  ),
                ),
              ),
              const SizedBox(width: 12),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      fullName,
                      style: const TextStyle(
                        fontSize: 16,
                        fontWeight: FontWeight.bold,
                        color: AppColors.cream,
                        fontFamily: 'serif',
                      ),
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                    ),
                    const SizedBox(height: 2),
                    Text(
                      email,
                      style: const TextStyle(
                        fontSize: 12,
                        color: AppColors.shootLight,
                      ),
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                    ),
                  ],
                ),
              ),
            ],
          ),
          const SizedBox(height: 12),
          Container(
            padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 3),
            decoration: BoxDecoration(
              color: Colors.white.withAlpha(30),
              borderRadius: BorderRadius.circular(12),
            ),
            child: Text(
              'Role: $roleName',
              style: const TextStyle(
                fontSize: 11,
                fontWeight: FontWeight.w600,
                color: AppColors.cream,
              ),
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildMenuItemTile({
    required BuildContext context,
    required MenuItemConfig item,
    required bool isActive,
  }) {
    return Container(
      margin: const EdgeInsets.symmetric(vertical: 2),
      decoration: BoxDecoration(
        color: isActive ? AppColors.forest.withAlpha(20) : Colors.transparent,
        borderRadius: BorderRadius.circular(8),
      ),
      child: ListTile(
        dense: true,
        leading: Icon(
          item.icon,
          color: isActive ? AppColors.forest : AppColors.inkSoft,
          size: 22,
        ),
        title: Text(
          item.label,
          style: TextStyle(
            fontSize: 14,
            fontWeight: isActive ? FontWeight.bold : FontWeight.w500,
            color: isActive ? AppColors.forest : AppColors.ink,
          ),
        ),
        trailing: item.badgeText != null
            ? Container(
                padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 2),
                decoration: BoxDecoration(
                  color: AppColors.gold,
                  borderRadius: BorderRadius.circular(10),
                ),
                child: Text(
                  item.badgeText!,
                  style: const TextStyle(
                    fontSize: 10,
                    fontWeight: FontWeight.bold,
                    color: AppColors.forestDeep,
                  ),
                ),
              )
            : null,
        onTap: () {
          Navigator.pop(context); // close drawer
          if (!isActive) {
            context.go(item.route);
          }
        },
      ),
    );
  }

  Widget _buildDrawerFooter(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(12),
      child: Column(
        children: [
          ListTile(
            dense: true,
            leading: const Icon(Icons.logout_rounded,
                color: AppColors.errorText, size: 20),
            title: const Text(
              'Sign Out',
              style: TextStyle(
                color: AppColors.errorText,
                fontWeight: FontWeight.w600,
                fontSize: 14,
              ),
            ),
            onTap: () {
              Navigator.pop(context);
              _handleLogout(context);
            },
          ),
          const SizedBox(height: 6),
          Container(
            padding: const EdgeInsets.all(10),
            decoration: BoxDecoration(
              color: AppColors.cream,
              borderRadius: BorderRadius.circular(8),
              border: Border.all(color: AppColors.line),
            ),
            child: Row(
              children: const [
                Icon(Icons.help_outline_rounded,
                    size: 16, color: AppColors.inkSoft),
                SizedBox(width: 8),
                Expanded(
                  child: Text(
                    'Need Help? Contact agrarian officer support.',
                    style: TextStyle(fontSize: 11, color: AppColors.inkSoft),
                  ),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}
