/// Cultivation cycle DTOs and enums, mirroring `CycleResponseDto` and the
/// `Entities/FieldCultivation` enums (sent over the wire as member names).
library;

/// Parses a `DateOnly` ("YYYY-MM-DD") without any time-zone shift.
DateTime parseDateOnly(String value) {
  final parts = value.split('-').map(int.parse).toList();
  return DateTime(parts[0], parts[1], parts[2]);
}

DateTime? parseNullableDateOnly(dynamic value) =>
    value is String && value.isNotEmpty ? parseDateOnly(value) : null;

/// Formats a date as the backend's `DateOnly` wire form.
String toDateOnly(DateTime date) =>
    '${date.year.toString().padLeft(4, '0')}-'
    '${date.month.toString().padLeft(2, '0')}-'
    '${date.day.toString().padLeft(2, '0')}';

const List<String> _months = [
  'Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun',
  'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec',
];

/// "14 Mar 2026".
String formatDate(DateTime? date) =>
    date == null ? '—' : '${date.day} ${_months[date.month - 1]} ${date.year}';

/// "14 Mar".
String formatShortDate(DateTime date) => '${date.day} ${_months[date.month - 1]}';

DateTime get today {
  final now = DateTime.now();
  return DateTime(now.year, now.month, now.day);
}

enum Season {
  yala('Yala', 'Yala'),
  maha('Maha', 'Maha');

  final String wire;
  final String label;
  const Season(this.wire, this.label);

  static Season fromString(String? value) =>
      values.firstWhere((s) => s.wire == value, orElse: () => yala);
}

enum CultivationMethod {
  broadcasting('Broadcasting', 'Broadcasting'),
  transplanting('Transplanting', 'Transplanting'),
  directSeeding('DirectSeeding', 'Direct seeding');

  final String wire;
  final String label;
  const CultivationMethod(this.wire, this.label);

  static CultivationMethod fromString(String? value) =>
      values.firstWhere((m) => m.wire == value, orElse: () => broadcasting);
}

enum CycleStatus {
  planned('Planned', 'Planned'),
  active('Active', 'Active'),
  harvested('Harvested', 'Harvested'),
  abandoned('Abandoned', 'Abandoned');

  final String wire;
  final String label;
  const CycleStatus(this.wire, this.label);

  static CycleStatus fromString(String? value) =>
      values.firstWhere((s) => s.wire == value, orElse: () => planned);

  /// Planned or Active — the field is taken for this season.
  bool get isOpen => this == planned || this == active;
}

enum GrowthStage {
  nursery('Nursery', 'Nursery'),
  tillering('Tillering', 'Tillering'),
  panicleInitiation('PanicleInitiation', 'Panicle initiation'),
  flowering('Flowering', 'Flowering'),
  grainFilling('GrainFilling', 'Grain filling'),
  harvest('Harvest', 'Harvest');

  final String wire;
  final String label;
  const GrowthStage(this.wire, this.label);

  static GrowthStage? tryParse(String? value) {
    for (final stage in values) {
      if (stage.wire == value) return stage;
    }
    return null;
  }

  static GrowthStage fromString(String? value) => tryParse(value) ?? nursery;
}

/// One planned stage of the timeline (StageWindowDto).
class StageWindow {
  final GrowthStage stage;
  final DateTime start;
  final DateTime end;

  const StageWindow({required this.stage, required this.start, required this.end});

  factory StageWindow.fromJson(Map<String, dynamic> json) => StageWindow(
        stage: GrowthStage.fromString(json['stage'] as String?),
        start: parseDateOnly(json['start'] as String),
        end: parseDateOnly(json['end'] as String),
      );
}

/// A reported growth stage (StageLogDto).
class StageLog {
  final int id;
  final GrowthStage stage;
  final DateTime observedOn;
  final String? notes;
  final String loggedByUserName;

  const StageLog({
    required this.id,
    required this.stage,
    required this.observedOn,
    required this.notes,
    required this.loggedByUserName,
  });

  factory StageLog.fromJson(Map<String, dynamic> json) => StageLog(
        id: json['id'] as int,
        stage: GrowthStage.fromString(json['stage'] as String?),
        observedOn: parseDateOnly(json['observedOn'] as String),
        notes: json['notes'] as String?,
        loggedByUserName: json['loggedByUserName'] as String? ?? '',
      );
}

/// A cultivation cycle with its timeline and stage log (CycleResponseDto).
class CultivationCycle {
  final int id;
  final int fieldId;
  final String fieldName;
  final int varietyId;
  final String varietyName;
  final int durationDays;
  final Season season;
  final int year;
  final CultivationMethod method;
  final DateTime sowingDate;
  final DateTime expectedHarvestDate;
  final DateTime? actualHarvestDate;
  final GrowthStage currentStage;
  final GrowthStage expectedStageToday;
  final CycleStatus status;
  final String? notes;
  final List<StageWindow> timeline;
  final List<StageLog> stageLogs;

  const CultivationCycle({
    required this.id,
    required this.fieldId,
    required this.fieldName,
    required this.varietyId,
    required this.varietyName,
    required this.durationDays,
    required this.season,
    required this.year,
    required this.method,
    required this.sowingDate,
    required this.expectedHarvestDate,
    required this.actualHarvestDate,
    required this.currentStage,
    required this.expectedStageToday,
    required this.status,
    required this.notes,
    required this.timeline,
    required this.stageLogs,
  });

  factory CultivationCycle.fromJson(Map<String, dynamic> json) => CultivationCycle(
        id: json['id'] as int,
        fieldId: json['fieldId'] as int? ?? 0,
        fieldName: json['fieldName'] as String? ?? '',
        varietyId: json['varietyId'] as int? ?? 0,
        varietyName: json['varietyName'] as String? ?? '',
        durationDays: json['durationDays'] as int? ?? 0,
        season: Season.fromString(json['season'] as String?),
        year: json['year'] as int? ?? 0,
        method: CultivationMethod.fromString(json['method'] as String?),
        sowingDate: parseDateOnly(json['sowingDate'] as String),
        expectedHarvestDate: parseDateOnly(json['expectedHarvestDate'] as String),
        actualHarvestDate: parseNullableDateOnly(json['actualHarvestDate']),
        currentStage: GrowthStage.fromString(json['currentStage'] as String?),
        expectedStageToday: GrowthStage.fromString(json['expectedStageToday'] as String?),
        status: CycleStatus.fromString(json['status'] as String?),
        notes: json['notes'] as String?,
        timeline: (json['timeline'] as List<dynamic>? ?? [])
            .map((e) => StageWindow.fromJson(e as Map<String, dynamic>))
            .toList(),
        stageLogs: (json['stageLogs'] as List<dynamic>? ?? [])
            .map((e) => StageLog.fromJson(e as Map<String, dynamic>))
            .toList(),
      );

  String get title => '${season.label} $year';

  /// Days since sowing, clamped to the season length (0 before sowing).
  int get dayOfSeason {
    final days = today.difference(sowingDate).inDays;
    if (days < 0) return 0;
    return days > durationDays ? durationDays : days;
  }

  double get progress => durationDays == 0 ? 0 : dayOfSeason / durationDays;

  /// The farmer's last report lags what the timeline expects today.
  bool get isBehindTimeline =>
      status == CycleStatus.active && currentStage.index < expectedStageToday.index;
}

/// Body for POST /fields/{fieldId}/start-cultivation (CreateCycleRequestDto).
class StartCycleRequest {
  final int varietyId;
  final Season season;
  final int year;
  final CultivationMethod method;
  final DateTime sowingDate;
  final String? notes;

  const StartCycleRequest({
    required this.varietyId,
    required this.season,
    required this.year,
    required this.method,
    required this.sowingDate,
    this.notes,
  });

  Map<String, dynamic> toJson(int fieldId) => {
        'fieldId': fieldId,
        'varietyId': varietyId,
        'season': season.wire,
        'year': year,
        'method': method.wire,
        'sowingDate': toDateOnly(sowingDate),
        'notes': notes,
      };
}

/// CycleService's calendar window around the sowing date, and DTO limits.
class CycleRules {
  static const int maxBackdateDays = 30;
  static const int maxLookaheadDays = 365;
  static const int notesMaxLength = 1000;
}
