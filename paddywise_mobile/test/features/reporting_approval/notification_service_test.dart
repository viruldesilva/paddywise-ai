import 'package:flutter_test/flutter_test.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'package:paddywise_mobile/models/notification_item.dart';
import 'package:paddywise_mobile/services/notification_service.dart';

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();

  setUp(() async {
    SharedPreferences.setMockInitialValues({});
    await NotificationService.init();
  });

  group('NotificationService Integration Tests', () {
    test('init populates default notifications when storage is empty', () {
      final notifications = NotificationService.getNotificationsForUser('usr_farmer_01');
      expect(notifications, isNotEmpty);
      expect(notifications.length, greaterThanOrEqualTo(3));
    });

    test('unread count notifier tracks unread count accurately', () {
      final unreadCount = NotificationService.getUnreadCount('usr_farmer_01');
      expect(unreadCount, greaterThan(0));
      expect(NotificationService.unreadCountNotifier.value, equals(unreadCount));
    });

    test('markAsRead updates isRead to true and decrements unread counter', () async {
      final initialUnread = NotificationService.getUnreadCount('usr_farmer_01');
      final notifications = NotificationService.getNotificationsForUser('usr_farmer_01');
      final unreadItem = notifications.firstWhere((n) => !n.isRead);

      await NotificationService.markAsRead(unreadItem.id, userId: 'usr_farmer_01');

      final updatedNotifications = NotificationService.getNotificationsForUser('usr_farmer_01');
      final updatedItem = updatedNotifications.firstWhere((n) => n.id == unreadItem.id);

      expect(updatedItem.isRead, isTrue);
      expect(NotificationService.getUnreadCount('usr_farmer_01'), equals(initialUnread - 1));
      expect(NotificationService.unreadCountNotifier.value, equals(initialUnread - 1));
    });

    test('markAllAsRead sets all user notifications to read', () async {
      await NotificationService.markAllAsRead('usr_farmer_01');

      expect(NotificationService.getUnreadCount('usr_farmer_01'), equals(0));
      expect(NotificationService.unreadCountNotifier.value, equals(0));

      final allNotifications = NotificationService.getNotificationsForUser('usr_farmer_01');
      expect(allNotifications.every((n) => n.isRead), isTrue);
    });

    test('addNotification prepends new notification and updates unread counter', () async {
      final initialCount = NotificationService.getNotificationsForUser('usr_farmer_01').length;
      final initialUnread = NotificationService.getUnreadCount('usr_farmer_01');

      final newItem = NotificationItem(
        id: 'new_notif_999',
        userId: 'usr_farmer_01',
        title: 'New AI Assessment',
        message: 'Plan reviewed successfully.',
        status: NotificationStatus.approved,
        isRead: false,
        createdAt: DateTime.now(),
      );

      await NotificationService.addNotification(newItem);

      final list = NotificationService.getNotificationsForUser('usr_farmer_01');
      expect(list.length, equals(initialCount + 1));
      expect(list.first.id, equals('new_notif_999'));
      expect(NotificationService.getUnreadCount('usr_farmer_01'), equals(initialUnread + 1));
    });
  });
}
