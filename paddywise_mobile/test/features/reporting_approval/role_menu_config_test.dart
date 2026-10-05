import 'package:flutter_test/flutter_test.dart';
import 'package:paddywise_mobile/core/widgets/role_menu_config.dart';

void main() {
  group('Role Menu Config Component 4 Unit Tests', () {
    test('Farmer menu includes Plan Status and Notifications screens', () {
      final farmerItems = getMenuItemsForRole('Farmer');
      final routes = farmerItems.map((i) => i.route).toList();

      expect(routes, contains('/farmer/plan-status'));
      expect(routes, contains('/farmer/notifications'));
    });

    test('AgriculturalOfficer menu includes Pending Reviews and notifications', () {
      final officerItems = getMenuItemsForRole('AgriculturalOfficer');
      final routes = officerItems.map((i) => i.route).toList();

      expect(routes, contains('/officer/pending-reviews'));
      expect(routes, contains('/officer/notifications'));
      // Agricultural Officer should NOT have farmer-specific plan status route
      expect(routes, isNot(contains('/farmer/plan-status')));
    });

    test('Case-insensitive role lookup works properly for all roles', () {
      final officerLower = getMenuItemsForRole('agriculturalofficer');
      expect(officerLower.any((i) => i.route == '/officer/pending-reviews'), isTrue);

      final adminLower = getMenuItemsForRole('admin');
      expect(adminLower.any((i) => i.route == '/admin/notifications'), isTrue);
    });

    test('Unrecognized or mismatched role string degrades gracefully without crashing', () {
      // Must not throw exception
      expect(() => getMenuItemsForRole('NonExistentRole'), returnsNormally);
      expect(() => getMenuItemsForRole(''), returnsNormally);
      expect(() => getMenuItemsForRole(null), returnsNormally);

      final fallbackItems = getMenuItemsForRole('UnknownGuestRole');
      expect(fallbackItems, isNotEmpty);
      // Ensures the fallback provides basic navigation (dashboard & profile)
      expect(fallbackItems.any((i) => i.route == '/dashboard'), isTrue);
    });
  });
}
