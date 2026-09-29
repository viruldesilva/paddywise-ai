/// DTOs for fields, divisions and varieties, mirroring the backend's
/// `DTOs/FieldCultivation/*` shapes (camelCase JSON).
library;

double _toDouble(dynamic value) => value is num ? value.toDouble() : 0;
double? _toNullableDouble(dynamic value) => value is num ? value.toDouble() : null;

/// An agrarian division (DivisionResponseDto).
class Division {
  final int id;
  final String name;
  final String district;
  final String province;

  const Division({
    required this.id,
    required this.name,
    required this.district,
    required this.province,
  });

  factory Division.fromJson(Map<String, dynamic> json) => Division(
        id: json['id'] as int,
        name: json['name'] as String? ?? '',
        district: json['district'] as String? ?? '',
        province: json['province'] as String? ?? '',
      );

  String get label => district.isEmpty ? name : '$name · $district';
}

/// A paddy variety a cycle can be sown with (VarietyResponseDto).
class Variety {
  final int id;
  final String name;
  final int durationDays;
  final String ageGroup;
  final String? notes;

  const Variety({
    required this.id,
    required this.name,
    required this.durationDays,
    required this.ageGroup,
    this.notes,
  });

  factory Variety.fromJson(Map<String, dynamic> json) => Variety(
        id: json['id'] as int,
        name: json['name'] as String? ?? '',
        durationDays: json['durationDays'] as int? ?? 0,
        ageGroup: json['ageGroup'] as String? ?? '',
        notes: json['notes'] as String?,
      );
}

/// A registered paddy field (FieldResponseDto).
class Field {
  final int id;
  final String name;
  final double area;
  final String soilType;
  final String irrigationType;
  final double? latitude;
  final double? longitude;
  final int divisionId;
  final String divisionName;
  final int farmerId;
  final String farmerName;
  final bool isActive;

  const Field({
    required this.id,
    required this.name,
    required this.area,
    required this.soilType,
    required this.irrigationType,
    required this.latitude,
    required this.longitude,
    required this.divisionId,
    required this.divisionName,
    required this.farmerId,
    required this.farmerName,
    required this.isActive,
  });

  factory Field.fromJson(Map<String, dynamic> json) => Field(
        id: json['id'] as int,
        name: json['name'] as String? ?? '',
        area: _toDouble(json['area']),
        soilType: json['soilType'] as String? ?? '',
        irrigationType: json['irrigationType'] as String? ?? '',
        latitude: _toNullableDouble(json['latitude']),
        longitude: _toNullableDouble(json['longitude']),
        divisionId: json['divisionId'] as int? ?? 0,
        divisionName: json['divisionName'] as String? ?? '',
        farmerId: json['farmerId'] as int? ?? 0,
        farmerName: json['farmerName'] as String? ?? '',
        isActive: json['isActive'] as bool? ?? true,
      );

  /// "2.5 acres" / "1 acre", without a trailing ".0".
  String get areaLabel {
    final text = area == area.roundToDouble() ? area.toStringAsFixed(0) : '$area';
    return '$text ${area == 1 ? 'acre' : 'acres'}';
  }
}

/// Body for POST /fields and PUT /fields/{id} (Create/UpdateFieldRequestDto).
class FieldRequest {
  final String name;
  final double area;
  final String soilType;
  final String irrigationType;
  final int divisionId;
  final double? latitude;
  final double? longitude;

  const FieldRequest({
    required this.name,
    required this.area,
    required this.soilType,
    required this.irrigationType,
    required this.divisionId,
    this.latitude,
    this.longitude,
  });

  Map<String, dynamic> toJson() => {
        'name': name,
        'area': area,
        'soilType': soilType,
        'irrigationType': irrigationType,
        'divisionId': divisionId,
        'latitude': latitude,
        'longitude': longitude,
      };
}

/// The validation attributes on CreateFieldRequestDto, checked before sending.
class FieldRules {
  static const int nameMaxLength = 100;
  static const double areaMin = 0.01;
  static const double areaMax = 1000;
  static const int soilTypeMaxLength = 50;
  static const int irrigationTypeMaxLength = 50;
}

/// Common Sri Lankan paddy soil and water sources, offered as quick picks.
/// Both fields stay free text on the backend.
const List<String> kSoilTypeSuggestions = [
  'Low humic gley',
  'Reddish brown earth',
  'Alluvial',
  'Red yellow podzolic',
  'Non-calcic brown',
];

const List<String> kIrrigationTypeSuggestions = [
  'Major tank',
  'Minor tank',
  'Anicut / canal',
  'Rainfed',
  'Agro-well',
];
