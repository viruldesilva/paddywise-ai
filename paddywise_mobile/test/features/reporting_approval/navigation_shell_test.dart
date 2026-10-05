import 'package:flutter_test/flutter_test.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'package:paddywise_mobile/core/widgets/role_menu_config.dart';
import 'package:paddywise_mobile/services/auth_service.dart';

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();

  setUp(() async {
    SharedPreferences.setMockInitialValues({
      'kumburu_session': '{"id":"usr_01","fullName":"Dr. Nilmini Perera","email":"officer@kumburu.lk","role":"extensionOfficer","token":"dummy_jwt"}',
      'kumburu_access_token': 'dummy_access_token',
      'kumburu_refresh_token': 'dummy_refresh_token',
    });
  });

  group('Navigation Shell & Logout Role-Based Tests', () {
    test('role_menu_config isolates AgriculturalOfficer menu from Farmer menu items', () {
      final officerItems = getMenuItemsForRole('AgriculturalOfficer');
      final officerRoutes = officerItems.map((i) => i.route).toList();

      final farmerItems = getMenuItemsForRole('Farmer');
      final farmerRoutes = farmerItems.map((i) => i.route).toList();

      // Agricultural Officer has Pending Reviews
      expect(officerRoutes, contains('/officer/pending-reviews'));
      expect(officerRoutes, contains('/officer/notifications'));
      expect(officerRoutes, isNot(contains('/farmer/plan-status')));
      expect(officerRoutes, isNot(contains('/farmer/observations')));

      // Farmer has Plan Status and Observations
      expect(farmerRoutes, contains('/farmer/plan-status'));
      expect(farmerRoutes, contains('/farmer/notifications'));
      expect(farmerRoutes, isNot(contains('/officer/pending-reviews')));
    });

    test('AuthService logout clears stored session, tokens, and active user', () async {
      // Set an active session first
      final prefs = await SharedPreferences.getInstance();
      expect(prefs.getString('kumburu_session'), isNotNull);
      expect(prefs.getString('kumburu_access_token'), isNotNull);

      // Perform logout
      await AuthService.logout();

      // Verify stored tokens and session are purged
      expect(prefs.getString('kumburu_session'), isNull);
      expect(prefs.getString('kumburu_access_token'), isNull);
      expect(prefs.getString('kumburu_refresh_token'), isNull);
      expect(AuthService.getCurrentUser(), isNull);
      expect(AuthService.getAccessToken(), isNull);
    });

    test('role_menu_config handles FieldOfficer and Admin navigation isolation', () {
      final adminItems = getMenuItemsForRole('Admin');
      final adminRoutes = adminItems.map((i) => i.route).toList();

      expect(adminRoutes, contains('/dashboard'));
      expect(adminRoutes, contains('/admin/notifications'));
      expect(adminRoutes, contains('/profile'));
      expect(adminRoutes, isNot(contains('/officer/pending-reviews')));
    });
  });
}
