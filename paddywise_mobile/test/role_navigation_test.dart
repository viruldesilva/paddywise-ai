import 'package:flutter_test/flutter_test.dart';
import 'package:paddywise_mobile/core/widgets/role_menu_config.dart';

void main() {
  group('Role-Based Menu Configuration Tests', () {
    test('contains exact keys matching backend UserRole enum strings', () {
      expect(roleMenus.containsKey('Farmer'), isTrue);
      expect(roleMenus.containsKey('AgriculturalOfficer'), isTrue);
      expect(roleMenus.containsKey('FieldOfficer'), isTrue);
      expect(roleMenus.containsKey('Admin'), isTrue);
    });

    test('Farmer menu has functional screens only', () {
      final items = getMenuItemsForRole('Farmer');
      final routes = items.map((i) => i.route).toList();

      expect(routes, contains('/dashboard'));
      expect(routes, contains('/farmer/plan-status'));
      expect(routes, contains('/farmer/notifications'));
      expect(routes, contains('/profile'));
      expect(routes, contains('/fields'));
      expect(routes, contains('/cycles'));
      // Unbuilt feature screens are omitted per Option (a)
      expect(routes.contains('/activities'), isFalse);
    });

    test('AgriculturalOfficer menu includes Pending Reviews and notifications', () {
      final items = getMenuItemsForRole('AgriculturalOfficer');
      final routes = items.map((i) => i.route).toList();

      expect(routes, contains('/dashboard'));
      expect(routes, contains('/officer/pending-reviews'));
      expect(routes, contains('/officer/notifications'));
      expect(routes, contains('/profile'));
    });

    test('Case-insensitive role lookup fallback works safely', () {
      final itemsLower = getMenuItemsForRole('agriculturalofficer');
      final routes = itemsLower.map((i) => i.route).toList();

      expect(routes, contains('/officer/pending-reviews'));
    });

    test('FieldOfficer and Admin have functional dashboards and profiles', () {
      final fieldOfficerItems = getMenuItemsForRole('FieldOfficer');
      expect(fieldOfficerItems.any((i) => i.route == '/dashboard'), isTrue);
      expect(fieldOfficerItems.any((i) => i.route == '/profile'), isTrue);

      final adminItems = getMenuItemsForRole('Admin');
      expect(adminItems.any((i) => i.route == '/dashboard'), isTrue);
      expect(adminItems.any((i) => i.route == '/profile'), isTrue);
    });
  });
}
