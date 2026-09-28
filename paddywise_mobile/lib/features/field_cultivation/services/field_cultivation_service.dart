import '../../../core/api/api_client.dart';
import '../models/cycle_models.dart';
import '../models/field_models.dart';
import '../models/plan_models.dart';

List<T> _list<T>(dynamic data, T Function(Map<String, dynamic>) parse) =>
    (data as List<dynamic>? ?? []).map((e) => parse(e as Map<String, dynamic>)).toList();

/// Farmer-side calls for Component 1: fields, cultivation cycles and the
/// Cultivation Planning Agent. Every call throws [ApiException] on failure.
class FieldCultivationService {
  /// The planning agent and the validation pass are two LLM round trips.
  static const Duration planRequestTimeout = Duration(seconds: 120);

  // ---------------------------------------------------------------- lookups

  static Future<List<Division>> getDivisions() async =>
      _list(await ApiClient.get('/divisions'), Division.fromJson);

  static Future<List<Variety>> getVarieties() async =>
      _list(await ApiClient.get('/varieties'), Variety.fromJson);

  // ----------------------------------------------------------------- fields

  static Future<List<Field>> getMyFields() async =>
      _list(await ApiClient.get('/fields'), Field.fromJson);

  static Future<Field> getField(int id) async =>
      Field.fromJson(await ApiClient.get('/fields/$id') as Map<String, dynamic>);

  static Future<Field> createField(FieldRequest request) async => Field.fromJson(
      await ApiClient.post('/fields', body: request.toJson()) as Map<String, dynamic>);

  static Future<Field> updateField(int id, FieldRequest request) async => Field.fromJson(
      await ApiClient.put('/fields/$id', body: request.toJson()) as Map<String, dynamic>);

  static Future<void> deleteField(int id) => ApiClient.delete('/fields/$id');

  // ----------------------------------------------------------------- cycles

  static Future<List<CultivationCycle>> getMyCycles({int? fieldId}) async => _list(
        await ApiClient.get('/cycles',
            query: fieldId == null ? null : {'fieldId': '$fieldId'}),
        CultivationCycle.fromJson,
      );

  static Future<CultivationCycle> getCycle(int id) async => CultivationCycle.fromJson(
      await ApiClient.get('/cycles/$id') as Map<String, dynamic>);

  static Future<CultivationCycle> startCultivation(
    int fieldId,
    StartCycleRequest request,
  ) async =>
      CultivationCycle.fromJson(await ApiClient.post(
        '/fields/$fieldId/start-cultivation',
        body: request.toJson(fieldId),
      ) as Map<String, dynamic>);

  static Future<CultivationCycle> logStage(
    int cycleId, {
    required GrowthStage stage,
    required DateTime observedOn,
    String? notes,
  }) async =>
      CultivationCycle.fromJson(await ApiClient.post(
        '/cycles/$cycleId/stages',
        body: {
          'stage': stage.wire,
          'observedOn': toDateOnly(observedOn),
          'notes': notes,
        },
      ) as Map<String, dynamic>);

  /// The only status change a farmer makes from the app; Active is reached
  /// through officer approval of a plan, Harvested by logging Harvest.
  static Future<CultivationCycle> abandonCycle(int cycleId) async =>
      CultivationCycle.fromJson(await ApiClient.patch(
        '/cycles/$cycleId/status',
        body: {'status': CycleStatus.abandoned.wire},
      ) as Map<String, dynamic>);

  // ------------------------------------------------------------------ plans

  static Future<CultivationPlan> requestPlan(int cycleId, String objective) async =>
      CultivationPlan.fromJson(await ApiClient.post(
        '/cycles/$cycleId/plans',
        body: {'objective': objective},
        timeout: planRequestTimeout,
      ) as Map<String, dynamic>);

  static Future<CultivationPlan> getPlan(int id) async => CultivationPlan.fromJson(
      await ApiClient.get('/plans/$id') as Map<String, dynamic>);

  /// Newest first, as the backend orders them.
  static Future<List<CultivationPlan>> getPlansForCycle(int cycleId) async =>
      _list(await ApiClient.get('/cycles/$cycleId/plans'), CultivationPlan.fromJson);
}
