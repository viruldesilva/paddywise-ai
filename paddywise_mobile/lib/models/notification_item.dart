enum NotificationStatus {
  approved,
  rejected,
  pending,
  executed,
  general,
}

extension NotificationStatusExtension on NotificationStatus {
  String get value {
    switch (this) {
      case NotificationStatus.approved:
        return 'APPROVED';
      case NotificationStatus.rejected:
        return 'REJECTED';
      case NotificationStatus.pending:
        return 'PENDING';
      case NotificationStatus.executed:
        return 'EXECUTED';
      case NotificationStatus.general:
        return 'GENERAL';
    }
  }

  static NotificationStatus fromString(String? status) {
    if (status == null) return NotificationStatus.general;
    switch (status.toUpperCase()) {
      case 'APPROVED':
        return NotificationStatus.approved;
      case 'REJECTED':
        return NotificationStatus.rejected;
      case 'PENDING':
        return NotificationStatus.pending;
      case 'EXECUTED':
        return NotificationStatus.executed;
      default:
        return NotificationStatus.general;
    }
  }
}

class NotificationItem {
  final String id;
  final String userId;
  final String title;
  final String message;
  final String type; // e.g. "CropActivityReview"
  final NotificationStatus status;
  final int? relatedCycleId;
  final int? relatedRecommendationId;
  final String? officerName;
  final String? officerComment;
  final String? actionText;
  final bool isRead;
  final DateTime createdAt;

  NotificationItem({
    required this.id,
    required this.userId,
    required this.title,
    required this.message,
    this.type = 'CropActivityReview',
    required this.status,
    this.relatedCycleId,
    this.relatedRecommendationId,
    this.officerName,
    this.officerComment,
    this.actionText,
    this.isRead = false,
    required this.createdAt,
  });

  NotificationItem copyWith({
    String? id,
    String? userId,
    String? title,
    String? message,
    String? type,
    NotificationStatus? status,
    int? relatedCycleId,
    int? relatedRecommendationId,
    String? officerName,
    String? officerComment,
    String? actionText,
    bool? isRead,
    DateTime? createdAt,
  }) {
    return NotificationItem(
      id: id ?? this.id,
      userId: userId ?? this.userId,
      title: title ?? this.title,
      message: message ?? this.message,
      type: type ?? this.type,
      status: status ?? this.status,
      relatedCycleId: relatedCycleId ?? this.relatedCycleId,
      relatedRecommendationId:
          relatedRecommendationId ?? this.relatedRecommendationId,
      officerName: officerName ?? this.officerName,
      officerComment: officerComment ?? this.officerComment,
      actionText: actionText ?? this.actionText,
      isRead: isRead ?? this.isRead,
      createdAt: createdAt ?? this.createdAt,
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'id': id,
      'userId': userId,
      'title': title,
      'message': message,
      'type': type,
      'status': status.value,
      'relatedCycleId': relatedCycleId,
      'relatedRecommendationId': relatedRecommendationId,
      'officerName': officerName,
      'officerComment': officerComment,
      'actionText': actionText,
      'isRead': isRead,
      'createdAt': createdAt.toIso8601String(),
    };
  }

  factory NotificationItem.fromJson(Map<String, dynamic> json) {
    return NotificationItem(
      id: json['id']?.toString() ?? '',
      userId: json['userId']?.toString() ?? '',
      title: json['title'] ?? '',
      message: json['message'] ?? '',
      type: json['type'] ?? 'CropActivityReview',
      status:
          NotificationStatusExtension.fromString(json['status']?.toString()),
      relatedCycleId: json['relatedCycleId'] != null
          ? int.tryParse(json['relatedCycleId'].toString())
          : null,
      relatedRecommendationId: json['relatedRecommendationId'] != null
          ? int.tryParse(json['relatedRecommendationId'].toString())
          : null,
      officerName: json['officerName']?.toString(),
      officerComment: json['officerComment']?.toString(),
      actionText: json['actionText']?.toString(),
      isRead: json['isRead'] == true,
      createdAt: json['createdAt'] != null
          ? DateTime.tryParse(json['createdAt'].toString()) ?? DateTime.now()
          : DateTime.now(),
    );
  }
}
