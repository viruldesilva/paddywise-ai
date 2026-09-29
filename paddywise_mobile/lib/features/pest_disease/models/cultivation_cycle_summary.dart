/// The subset of `DTOs/FieldCultivation/CycleResponseDto.cs` the pest/disease pickers need —
/// GET /api/cycles returns more fields (timeline, stage logs, variety…) that aren't relevant
/// to picking which cycle an observation belongs to.
class CultivationCycleSummary {
  final int id;
  final String fieldName;
  final String season;
  final int year;
  final String currentStage;

  const CultivationCycleSummary({
    required this.id,
    required this.fieldName,
    required this.season,
    required this.year,
    required this.currentStage,
  });

  /// Matches ObservationForm.tsx's cycle option label:
  /// "{fieldName} — {season} {year} ({currentStage})".
  String get label => '$fieldName — $season $year ($currentStage)';

  factory CultivationCycleSummary.fromJson(Map<String, dynamic> json) {
    return CultivationCycleSummary(
      id: json['id'] as int,
      fieldName: json['fieldName'] as String? ?? '',
      season: json['season'] as String? ?? '',
      year: json['year'] as int? ?? 0,
      currentStage: json['currentStage'] as String? ?? '',
    );
  }
}
