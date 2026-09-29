import 'package:flutter/material.dart';

class MenuItemConfig {
  final String label;
  final IconData icon;
  final String route;
  final String? badgeText;

  const MenuItemConfig({
    required this.label,
    required this.icon,
    required this.route,
    this.badgeText,
  });
}

/// Centralized role-based menu configuration.
/// Role keys match the backend PascalCase strings:
/// "Farmer", "AgriculturalOfficer", "FieldOfficer", "Admin".
/// Non-existent screens are omitted per Option (a) with explanatory TODO comments.
final Map<String, List<MenuItemConfig>> roleMenus = {
  'Farmer': [
    const MenuItemConfig(
      label: 'Home / Dashboard',
      icon: Icons.dashboard_outlined,
      route: '/dashboard',
    ),
    // TODO: 'My Fields' - awaiting Field & Cultivation feature screen
    // TODO: 'My Cultivations' - awaiting Field & Cultivation feature screen
    const MenuItemConfig(
      label: 'Report Crop Problem',
      icon: Icons.bug_report_outlined,
      route: '/farmer/observations',
    ),
    const MenuItemConfig(
      label: 'Plan Status & AI Advice',
      icon: Icons.psychology_outlined,
      route: '/farmer/plan-status',
    ),
    const MenuItemConfig(
      label: 'Notifications',
      icon: Icons.notifications_none_outlined,
      route: '/farmer/notifications',
    ),
    // TODO: 'Weather' - awaiting Weather service integration
    const MenuItemConfig(
      label: 'Profile',
      icon: Icons.person_outline,
      route: '/profile',
    ),
  ],

  'AgriculturalOfficer': [
    const MenuItemConfig(
      label: 'Dashboard / Overview',
      icon: Icons.dashboard_outlined,
      route: '/dashboard',
    ),
    const MenuItemConfig(
      label: 'Pending Reviews',
      icon: Icons.assignment_turned_in_outlined,
      route: '/officer/pending-reviews',
      badgeText: 'Live',
    ),
    // TODO: 'Cultivation Plans' - awaiting Cultivation Plan list screen
    // TODO: 'Pest & Disease Reports' - awaiting Pest & Disease report screen
    const MenuItemConfig(
      label: 'Notifications',
      icon: Icons.notifications_none_outlined,
      route: '/officer/notifications',
    ),
    const MenuItemConfig(
      label: 'Profile',
      icon: Icons.person_outline,
      route: '/profile',
    ),
  ],

  'FieldOfficer': [
    const MenuItemConfig(
      label: 'Dashboard / Overview',
      icon: Icons.dashboard_outlined,
      route: '/dashboard',
    ),
    // TODO: 'Field Inspections' - awaiting Field Officer inspections screen
    const MenuItemConfig(
      label: 'Notifications',
      icon: Icons.notifications_none_outlined,
      route: '/field-officer/notifications',
    ),
    const MenuItemConfig(
      label: 'Profile',
      icon: Icons.person_outline,
      route: '/profile',
    ),
  ],

  'Admin': [
    const MenuItemConfig(
      label: 'Dashboard / Overview',
      icon: Icons.dashboard_outlined,
      route: '/dashboard',
    ),
    // TODO: 'Users' - awaiting Admin user management screen
    // TODO: 'Audit Logs' - awaiting Admin audit logs screen
    // TODO: 'Reports' - awaiting Admin reports screen
    const MenuItemConfig(
      label: 'Notifications',
      icon: Icons.notifications_none_outlined,
      route: '/admin/notifications',
    ),
    const MenuItemConfig(
      label: 'Profile',
      icon: Icons.person_outline,
      route: '/profile',
    ),
  ],
};

/// Helper to safely retrieve menu items for any given role string
List<MenuItemConfig> getMenuItemsForRole(String? role) {
  if (role == null || role.isEmpty) {
    return roleMenus['Farmer'] ?? [];
  }

  // Exact match
  if (roleMenus.containsKey(role)) {
    return roleMenus[role]!;
  }

  // Case-insensitive match fallback
  for (final entry in roleMenus.entries) {
    if (entry.key.toLowerCase() == role.toLowerCase().replaceAll(' ', '')) {
      return entry.value;
    }
  }

  // Fallback default
  return roleMenus['Farmer'] ?? [];
}
