import 'package:flutter_test/flutter_test.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'package:paddywise_mobile/services/notification_service.dart';

// Reused unchanged from Component 1's suite: http.runWithClient + MockClient.
import '../field_cultivation/fc_test_harness.dart';

/// Whether the farmer's app shows the notifications the backend creates
/// (GET /api/notifications). Virul's notification_service_test covers the local store.
void main() {
  TestWidgetsFlutterBinding.ensureInitialized();

  setUp(() => SharedPreferences.setMockInitialValues({}));

  test('control: today the service seeds local demo notifications and makes no HTTP call', () async {
    final calls = await withFakeBackend((_) => jsonResponse([]), () async {
      await NotificationService.init();
    });

    expect(calls, isEmpty);
    expect(NotificationService.getNotificationsForUser('usr_farmer_01'), isNotEmpty);
  });

  test('init loads the signed-in farmer\'s notifications from GET /api/notifications', () async {
    final calls = await withFakeBackend(
      (_) => jsonResponse([
        {
          'id': 77,
          'userId': 1,
          'title': 'Cultivation Plan Update',
          'message': 'Your plan was approved.',
          'type': 'CultivationPlanReview',
          'status': 'Approved',
          'isRead': false,
          'createdAt': '2026-10-01T05:00:00Z',
        }
      ]),
      () async => NotificationService.init(),
    );

    expect(calls.map((c) => c.path), contains('/notifications'));
    expect(NotificationService.getNotificationsForUser('').map((n) => n.message), contains('Your plan was approved.'));
  },
      skip: 'Known bug (finding 7): NotificationService is SharedPreferences-only, seeded with demo '
          'notifications, and never calls /api/notifications — backend notifications never reach the app.');
}
