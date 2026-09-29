import 'pest_disease_report.dart';

/// Entities/PestDisease/ObservationType.cs, by member name — matches
/// paddywise-web/src/features/pest-disease/types.ts's ObservationType.
const List<String> observationTypes = ['Pest', 'Disease', 'Unknown'];

/// Mirrors OBSERVATION_TYPE_LABELS on the web.
const Map<String, String> observationTypeLabels = {
  'Pest': 'Pest',
  'Disease': 'Disease',
  'Unknown': 'Not sure — let the agent decide',
};

/// Entities/PestDisease/ObservationSeverity.cs, by member name.
const List<String> observationSeverities = ['Low', 'Moderate', 'Severe'];

/// Mirrors SEVERITY_LABELS on the web (currently a no-op map, kept for parity/consistency).
const Map<String, String> severityLabels = {
  'Low': 'Low',
  'Moderate': 'Moderate',
  'Severe': 'Severe',
};

/// CreateObservationRequestDto/UpdateObservationRequestDto's [MaxLength] — mirrored here so
/// the farmer sees the same verdict the server would give, without a round trip.
const int symptomsMaxLength = 2000;

/// Mirrors DTOs/PestDisease/ObservationResponseDto.cs.
class Observation {
  final int id;
  final int cultivationCycleId;
  final int fieldId;
  final String fieldName;
  final int reportedByUserId;
  final String reportedByUserName;
  final String observationType;
  final String cropStage;
  final String symptoms;
  final String severity;
  final String? imageUrl;

  /// Null when never analysed. Set with an empty [reports] list once the agent ran and found
  /// no likely match — distinct from "not yet analysed."
  final DateTime? lastAnalyzedAt;

  final DateTime createdAt;
  final DateTime updatedAt;
  final List<PestDiseaseReport> reports;

  const Observation({
    required this.id,
    required this.cultivationCycleId,
    required this.fieldId,
    required this.fieldName,
    required this.reportedByUserId,
    required this.reportedByUserName,
    required this.observationType,
    required this.cropStage,
    required this.symptoms,
    required this.severity,
    this.imageUrl,
    this.lastAnalyzedAt,
    required this.createdAt,
    required this.updatedAt,
    this.reports = const [],
  });

  factory Observation.fromJson(Map<String, dynamic> json) {
    return Observation(
      id: json['id'] as int,
      cultivationCycleId: json['cultivationCycleId'] as int,
      fieldId: json['fieldId'] as int,
      fieldName: json['fieldName'] as String? ?? '',
      reportedByUserId: json['reportedByUserId'] as int,
      reportedByUserName: json['reportedByUserName'] as String? ?? '',
      observationType: json['observationType'] as String? ?? 'Unknown',
      cropStage: json['cropStage'] as String? ?? '',
      symptoms: json['symptoms'] as String? ?? '',
      severity: json['severity'] as String? ?? 'Low',
      imageUrl: json['imageUrl'] as String?,
      lastAnalyzedAt: json['lastAnalyzedAt'] == null
          ? null
          : DateTime.tryParse(json['lastAnalyzedAt'] as String),
      createdAt: DateTime.tryParse(json['createdAt'] as String? ?? '') ??
          DateTime.now(),
      updatedAt: DateTime.tryParse(json['updatedAt'] as String? ?? '') ??
          DateTime.now(),
      reports: (json['reports'] as List<dynamic>? ?? [])
          .map((item) => PestDiseaseReport.fromJson(item as Map<String, dynamic>))
          .toList(),
    );
  }
}

/// Mirrors DTOs/PestDisease/CreateObservationRequestDto.cs.
class CreateObservationRequest {
  final int cultivationCycleId;
  final String observationType;
  final String symptoms;
  final String severity;
  final String? imageUrl;

  const CreateObservationRequest({
    required this.cultivationCycleId,
    required this.observationType,
    required this.symptoms,
    required this.severity,
    this.imageUrl,
  });

  Map<String, dynamic> toJson() => {
        'cultivationCycleId': cultivationCycleId,
        'observationType': observationType,
        'symptoms': symptoms,
        'severity': severity,
        'imageUrl': imageUrl,
      };
}

/// Mirrors DTOs/PestDisease/UpdateObservationRequestDto.cs — the cycle cannot change.
class UpdateObservationRequest {
  final String observationType;
  final String symptoms;
  final String severity;
  final String? imageUrl;

  const UpdateObservationRequest({
    required this.observationType,
    required this.symptoms,
    required this.severity,
    this.imageUrl,
  });

  Map<String, dynamic> toJson() => {
        'observationType': observationType,
        'symptoms': symptoms,
        'severity': severity,
        'imageUrl': imageUrl,
      };
}
