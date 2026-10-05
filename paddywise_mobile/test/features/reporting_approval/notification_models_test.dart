import 'package:flutter_test/flutter_test.dart';
import 'package:paddywise_mobile/models/notification_item.dart';

void main() {
  group('NotificationItem Model Unit Tests', () {
    test('fromJson correctly parses sample API response into NotificationItem', () {
      final json = {
        'id': 'notif_100',
        'userId': 'usr_farmer_42',
        'title': 'Plan Approved',
        'message': 'Your seasonal plan has been reviewed and approved.',
        'type': 'CultivationPlanReview',
        'status': 'APPROVED',
        'relatedCycleId': 10,
        'relatedRecommendationId': 25,
        'officerName': 'Dr. Nilmini Perera',
        'officerComment': 'Dosage compliant with RRDI guidelines.',
        'actionText': 'Proceed to land preparation',
        'isRead': false,
        'createdAt': '2026-10-01T08:30:00.000Z',
      };

      final item = NotificationItem.fromJson(json);

      expect(item.id, 'notif_100');
      expect(item.userId, 'usr_farmer_42');
      expect(item.title, 'Plan Approved');
      expect(item.message, 'Your seasonal plan has been reviewed and approved.');
      expect(item.type, 'CultivationPlanReview');
      expect(item.status, NotificationStatus.approved);
      expect(item.relatedCycleId, 10);
      expect(item.relatedRecommendationId, 25);
      expect(item.officerName, 'Dr. Nilmini Perera');
      expect(item.officerComment, 'Dosage compliant with RRDI guidelines.');
      expect(item.actionText, 'Proceed to land preparation');
      expect(item.isRead, isFalse);
      expect(item.createdAt, DateTime.parse('2026-10-01T08:30:00.000Z'));
    });

    test('fromJson handles null and missing optional fields with safe fallbacks', () {
      final json = {
        'id': 101, // int ID should parse to string
        'userId': 99,
        'title': 'General Notice',
        'message': 'System maintenance scheduled.',
        // missing status, type, dates, comments
      };

      final item = NotificationItem.fromJson(json);

      expect(item.id, '101');
      expect(item.userId, '99');
      expect(item.title, 'General Notice');
      expect(item.type, 'CropActivityReview');
      expect(item.status, NotificationStatus.general);
      expect(item.relatedCycleId, isNull);
      expect(item.relatedRecommendationId, isNull);
      expect(item.officerName, isNull);
      expect(item.officerComment, isNull);
      expect(item.isRead, isFalse);
      expect(item.createdAt, isNotNull);
    });

    test('NotificationStatusExtension parses all known statuses case-insensitively', () {
      expect(NotificationStatusExtension.fromString('APPROVED'), NotificationStatus.approved);
      expect(NotificationStatusExtension.fromString('approved'), NotificationStatus.approved);
      expect(NotificationStatusExtension.fromString('REJECTED'), NotificationStatus.rejected);
      expect(NotificationStatusExtension.fromString('rejected'), NotificationStatus.rejected);
      expect(NotificationStatusExtension.fromString('PENDING'), NotificationStatus.pending);
      expect(NotificationStatusExtension.fromString('EXECUTED'), NotificationStatus.executed);
      expect(NotificationStatusExtension.fromString('UNKNOWN_STATUS'), NotificationStatus.general);
      expect(NotificationStatusExtension.fromString(null), NotificationStatus.general);
    });

    test('toJson serializes NotificationItem correctly for persistence', () {
      final item = NotificationItem(
        id: 'notif_005',
        userId: 'usr_01',
        title: 'Action Required',
        message: 'Revise nitrogen application rate.',
        type: 'PlanRevision',
        status: NotificationStatus.rejected,
        relatedCycleId: 5,
        officerName: 'Officer Bandara',
        officerComment: 'Too high dosage for early stage.',
        isRead: true,
        createdAt: DateTime.parse('2026-10-02T12:00:00.000Z'),
      );

      final json = item.toJson();

      expect(json['id'], 'notif_005');
      expect(json['status'], 'REJECTED');
      expect(json['isRead'], isTrue);
      expect(json['relatedCycleId'], 5);
      expect(json['officerComment'], 'Too high dosage for early stage.');
    });

    test('copyWith updates specified fields while retaining remaining properties', () {
      final item = NotificationItem(
        id: 'notif_001',
        userId: 'usr_01',
        title: 'Original Title',
        message: 'Original Message',
        status: NotificationStatus.pending,
        isRead: false,
        createdAt: DateTime.parse('2026-10-01T00:00:00.000Z'),
      );

      final updated = item.copyWith(isRead: true, title: 'Updated Title');

      expect(updated.id, 'notif_001');
      expect(updated.title, 'Updated Title');
      expect(updated.message, 'Original Message');
      expect(updated.isRead, isTrue);
    });
  });
}
