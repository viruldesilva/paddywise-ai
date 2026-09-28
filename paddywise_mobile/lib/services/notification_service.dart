import 'dart:convert';
import 'package:flutter/foundation.dart';
import 'package:shared_preferences/shared_preferences.dart';
import '../models/notification_item.dart';

class NotificationService {
  static const String _storageKeyNotifications = 'kumburu_notifications';

  static final ValueNotifier<int> unreadCountNotifier = ValueNotifier<int>(0);

  static final List<NotificationItem> _defaultNotifications = [
    NotificationItem(
      id: 'notif_001',
      userId: 'usr_farmer_01',
      title: 'Crop Activity Recommendation Approved',
      message:
          'Agricultural Officer Dr. Nilmini Perera approved: "Apply 2nd Split Urea (25 kg/ac) with 5 kg MOP at Mid-Tillering stage". Advice: "Approved as per DOA RRDI guidelines. Apply under 2-3cm standing water and ensure field is drained before application."',
      type: 'CropActivityReview',
      status: NotificationStatus.approved,
      relatedCycleId: 1,
      relatedRecommendationId: 101,
      officerName: 'Dr. Nilmini Perera',
      officerComment:
          'Approved as per DOA RRDI guidelines. Apply under 2-3cm standing water and ensure field is drained 24h prior.',
      actionText:
          'Apply 2nd Split Urea (25 kg/ac) with 5 kg MOP at Mid-Tillering stage',
      isRead: false,
      createdAt: DateTime.now().subtract(const Duration(hours: 2)),
    ),
    NotificationItem(
      id: 'notif_002',
      userId: 'usr_farmer_01',
      title: 'Crop Activity Recommendation Rejected',
      message:
          'Agricultural Officer Dr. Nilmini Perera advised not to proceed with: "Apply Chlorpyrifos 40% EC spray for hopper scorch". Note: "Rejected: Excessive chemical toxicity risk to natural predators (mirid bugs & spiders). Adopt field intermittent drying and water management instead."',
      type: 'CropActivityReview',
      status: NotificationStatus.rejected,
      relatedCycleId: 1,
      relatedRecommendationId: 102,
      officerName: 'Dr. Nilmini Perera',
      officerComment:
          'Rejected: Excessive chemical toxicity risk to natural predators (mirid bugs & spiders). Adopt field intermittent drying and water management instead.',
      actionText: 'Apply Chlorpyrifos 40% EC spray for hopper scorch',
      isRead: false,
      createdAt: DateTime.now().subtract(const Duration(hours: 14)),
    ),
    NotificationItem(
      id: 'notif_003',
      userId: 'usr_farmer_01',
      title: 'AI Crop Activity Advisory Generated',
      message:
          'Agentic AI generated: "Intermittent Drainage & Panicle Initiation Water Level Control". Awaiting review by Agrarian Services Division.',
      type: 'CropActivityReview',
      status: NotificationStatus.pending,
      relatedCycleId: 1,
      relatedRecommendationId: 103,
      officerName: 'Agrarian Services Division',
      officerComment:
          'AI recommendation dispatched to Agrarian Services Division for officer review.',
      actionText:
          'Intermittent Drainage & Panicle Initiation Water Level Control',
      isRead: true,
      createdAt: DateTime.now().subtract(const Duration(days: 1)),
    ),
  ];

  static List<NotificationItem> _notifications = [];

  static Future<void> init() async {
    try {
      final prefs = await SharedPreferences.getInstance();
      final raw = prefs.getString(_storageKeyNotifications);
      if (raw == null) {
        _notifications = List.from(_defaultNotifications);
        await _saveToStorage(_notifications);
      } else {
        final List<dynamic> list = jsonDecode(raw) as List<dynamic>;
        _notifications = list
            .map((item) =>
                NotificationItem.fromJson(item as Map<String, dynamic>))
            .toList();
      }
    } catch (_) {
      _notifications = List.from(_defaultNotifications);
    }
    _updateUnreadCount('usr_farmer_01');
  }

  static Future<void> _saveToStorage(List<NotificationItem> items) async {
    try {
      final prefs = await SharedPreferences.getInstance();
      final jsonString = jsonEncode(items.map((i) => i.toJson()).toList());
      await prefs.setString(_storageKeyNotifications, jsonString);
    } catch (_) {}
  }

  static List<NotificationItem> getNotificationsForUser(String userId) {
    return _notifications
        .where((n) => n.userId == userId || userId.isEmpty)
        .toList()
      ..sort((a, b) => b.createdAt.compareTo(a.createdAt));
  }

  static int getUnreadCount(String userId) {
    return _notifications
        .where((n) => (n.userId == userId || userId.isEmpty) && !n.isRead)
        .length;
  }

  static void _updateUnreadCount(String userId) {
    unreadCountNotifier.value = getUnreadCount(userId);
  }

  static Future<void> markAsRead(String id, {String userId = 'usr_farmer_01'}) async {
    final index = _notifications.indexWhere((n) => n.id == id);
    if (index != -1) {
      _notifications[index] = _notifications[index].copyWith(isRead: true);
      await _saveToStorage(_notifications);
      _updateUnreadCount(userId);
    }
  }

  static Future<void> markAllAsRead(String userId) async {
    for (int i = 0; i < _notifications.length; i++) {
      if (_notifications[i].userId == userId || userId.isEmpty) {
        _notifications[i] = _notifications[i].copyWith(isRead: true);
      }
    }
    await _saveToStorage(_notifications);
    _updateUnreadCount(userId);
  }

  static Future<void> addNotification(NotificationItem item) async {
    _notifications.insert(0, item);
    await _saveToStorage(_notifications);
    _updateUnreadCount(item.userId);
  }

  static Future<void> resetToDefaults() async {
    _notifications = List.from(_defaultNotifications);
    await _saveToStorage(_notifications);
    _updateUnreadCount('usr_farmer_01');
  }
}
