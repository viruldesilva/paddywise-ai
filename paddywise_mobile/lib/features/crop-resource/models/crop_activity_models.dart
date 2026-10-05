import 'dart:convert';

/// The four supported activity types matching the web and backend models.
enum ActivityType {
  fertilizer('Fertilizer'),
  irrigation('Irrigation'),
  pesticide('Pesticide'),
  other('Other');

  final String label;
  const ActivityType(this.label);

  static ActivityType fromString(String val) {
    for (final type in ActivityType.values) {
      if (type.label.toLowerCase() == val.trim().toLowerCase()) {
        return type;
      }
    }
    return ActivityType.fertilizer;
  }
}

/// Request DTO for creating an activity via POST /api/cycles/{cycleId}/activities
class CreateCropActivityRequest {
  final String activityType;
  final String date; // YYYY-MM-DD
  final String detailsJson;

  const CreateCropActivityRequest({
    required this.activityType,
    required this.date,
    required this.detailsJson,
  });

  Map<String, dynamic> toJson() => {
    'activityType': activityType,
    'date': date,
    'detailsJson': detailsJson,
  };
}

/// Request DTO for updating an activity via PUT /api/activities/{id}
class UpdateCropActivityRequest {
  final String activityType;
  final String date; // YYYY-MM-DD
  final String detailsJson;

  const UpdateCropActivityRequest({
    required this.activityType,
    required this.date,
    required this.detailsJson,
  });

  Map<String, dynamic> toJson() => {
    'activityType': activityType,
    'date': date,
    'detailsJson': detailsJson,
  };
}

/// DTO for a recorded crop activity returned by the backend.
class CropActivityDto {
  final int id;
  final int cultivationCycleId;
  final String activityType;
  final String date;
  final String detailsJson;
  final int loggedByUserId;
  final String loggedByUserName;
  final String createdAt;
  final String? fieldName;
  final String? farmerName;
  final int? farmerId;
  final String? cycleName;

  const CropActivityDto({
    required this.id,
    required this.cultivationCycleId,
    required this.activityType,
    required this.date,
    required this.detailsJson,
    required this.loggedByUserId,
    required this.loggedByUserName,
    required this.createdAt,
    this.fieldName,
    this.farmerName,
    this.farmerId,
    this.cycleName,
  });

  factory CropActivityDto.fromJson(Map<String, dynamic> json) {
    return CropActivityDto(
      id: json['id'] as int? ?? 0,
      cultivationCycleId: json['cultivationCycleId'] as int? ?? 0,
      activityType: json['activityType'] as String? ?? '',
      date: (json['date'] as String? ?? '').split('T').first,
      detailsJson: json['detailsJson'] as String? ?? '{}',
      loggedByUserId: json['loggedByUserId'] as int? ?? 0,
      loggedByUserName: json['loggedByUserName'] as String? ?? '',
      createdAt: json['createdAt'] as String? ?? '',
      fieldName: json['fieldName'] as String?,
      farmerName: json['farmerName'] as String?,
      farmerId: json['farmerId'] as int?,
      cycleName: json['cycleName'] as String?,
    );
  }

  Map<String, dynamic> toJson() => {
    'id': id,
    'cultivationCycleId': cultivationCycleId,
    'activityType': activityType,
    'date': date,
    'detailsJson': detailsJson,
    'loggedByUserId': loggedByUserId,
    'loggedByUserName': loggedByUserName,
    'createdAt': createdAt,
    if (fieldName != null) 'fieldName': fieldName,
    if (farmerName != null) 'farmerName': farmerName,
    if (farmerId != null) 'farmerId': farmerId,
    if (cycleName != null) 'cycleName': cycleName,
  };

  Map<String, dynamic> get parsedDetails {
    try {
      return jsonDecode(detailsJson) as Map<String, dynamic>;
    } catch (_) {
      return {};
    }
  }

  CropActivityDto copyWith({
    int? id,
    int? cultivationCycleId,
    String? activityType,
    String? date,
    String? detailsJson,
    int? loggedByUserId,
    String? loggedByUserName,
    String? createdAt,
    String? fieldName,
    String? farmerName,
    int? farmerId,
    String? cycleName,
  }) {
    return CropActivityDto(
      id: id ?? this.id,
      cultivationCycleId: cultivationCycleId ?? this.cultivationCycleId,
      activityType: activityType ?? this.activityType,
      date: date ?? this.date,
      detailsJson: detailsJson ?? this.detailsJson,
      loggedByUserId: loggedByUserId ?? this.loggedByUserId,
      loggedByUserName: loggedByUserName ?? this.loggedByUserName,
      createdAt: createdAt ?? this.createdAt,
      fieldName: fieldName ?? this.fieldName,
      farmerName: farmerName ?? this.farmerName,
      farmerId: farmerId ?? this.farmerId,
      cycleName: cycleName ?? this.cycleName,
    );
  }
}

/// Lightweight Cultivation Cycle representation for activity tracking.
class CultivationCycleSummary {
  final int id;
  final int fieldId;
  final String fieldName;
  final int varietyId;
  final String varietyName;
  final String season;
  final int year;
  final String currentStage;
  final String? sowingDate;
  final String? actualHarvestDate;
  final String? status;

  const CultivationCycleSummary({
    required this.id,
    required this.fieldId,
    required this.fieldName,
    required this.varietyId,
    required this.varietyName,
    required this.season,
    required this.year,
    required this.currentStage,
    this.sowingDate,
    this.actualHarvestDate,
    this.status,
  });

  factory CultivationCycleSummary.fromJson(Map<String, dynamic> json) {
    return CultivationCycleSummary(
      id: json['id'] as int? ?? 0,
      fieldId: json['fieldId'] as int? ?? 0,
      fieldName: json['fieldName'] as String? ?? 'Field',
      varietyId: json['varietyId'] as int? ?? 0,
      varietyName: json['varietyName'] as String? ?? 'Rice Variety',
      season: json['season'] as String? ?? 'Yala',
      year: json['year'] as int? ?? DateTime.now().year,
      currentStage: json['currentStage'] as String? ?? 'Tillering',
      sowingDate: json['sowingDate'] as String?,
      actualHarvestDate: json['actualHarvestDate'] as String?,
      status: json['status'] as String?,
    );
  }

  String get displayName => '$season $year · $varietyName';
}

/// Fertilizer payload details
class FertilizerFormData {
  String date;
  String type;
  double? quantity;
  String cropStage;
  String region;
  String method;

  FertilizerFormData({
    this.date = '',
    this.type = '',
    this.quantity,
    this.cropStage = '',
    this.region = '',
    this.method = '',
  });

  Map<String, dynamic> toJson() => {
    'activityType': 'Fertilizer',
    'date': date,
    'type': type,
    'quantity': quantity,
    'cropStage': cropStage,
    'region': region,
    'method': method,
  };
}

/// Irrigation payload details
class IrrigationFormData {
  String date;
  double? waterLevel;
  double? duration;
  String source;

  IrrigationFormData({
    this.date = '',
    this.waterLevel,
    this.duration,
    this.source = '',
  });

  Map<String, dynamic> toJson() => {
    'activityType': 'Irrigation',
    'date': date,
    'waterLevel': waterLevel,
    'duration': duration,
    'source': source,
  };
}

/// Pesticide payload details
class PesticideFormData {
  String date;
  String product;
  String targetPest;
  double? quantity;
  String method;

  PesticideFormData({
    this.date = '',
    this.product = '',
    this.targetPest = '',
    this.quantity,
    this.method = '',
  });

  Map<String, dynamic> toJson() => {
    'activityType': 'Pesticide',
    'date': date,
    'product': product,
    'targetPest': targetPest,
    'quantity': quantity,
    'method': method,
  };
}

/// Other Activity payload details
class OtherFormData {
  String date;
  String specificActivity;
  String notes;

  OtherFormData({
    this.date = '',
    this.specificActivity = '',
    this.notes = '',
  });

  Map<String, dynamic> toJson() => {
    'activityType': 'Other',
    'date': date,
    'specificActivity': specificActivity,
    'notes': notes,
  };
}
