import 'dart:convert';
import 'package:http/http.dart' as http;
import 'package:http_parser/http_parser.dart';
import '../../../core/storage/user_storage.dart';
import '../../../core/api/api_config.dart';
import '../../../services/auth_service.dart';
import '../models/cultivation_cycle_summary.dart';
import '../models/observation.dart';

/// Thrown for any non-2xx response, carrying the backend's own `{message}` (or an
/// ASP.NET ValidationProblemDetails / title fallback) — same unwrapping precedent as
/// AuthService._extractBackendError elsewhere in this app, and extractApiErrorMessage on
/// the web (paddywise-web/src/services/authService.ts).
class ApiException implements Exception {
  final String message;
  const ApiException(this.message);

  @override
  String toString() => message;
}

/// All HTTP calls behind the Pest & Disease Monitoring screens — GET/POST/PUT
/// /api/observations, POST .../photo, POST .../request-analysis, and GET /api/cycles for the
/// pickers. Takes an [http.Client] so tests can pass http.testing.MockClient instead of
/// hitting the network; nothing else in this app needs a second HTTP client.
class ObservationApiService {
  final http.Client _client;
  final Duration _requestAnalysisTimeout;
  final Duration _pollInterval;
  final Duration _pollWindow;

  /// Default: how long request-analysis itself is given before falling back to polling.
  /// Kept above the backend's own Gemini HttpClient.Timeout (240s) so a normal successful
  /// run returns before this client would ever give up first.
  static const Duration defaultRequestAnalysisTimeout = Duration(seconds: 270);

  static const Duration defaultPollInterval = Duration(seconds: 10);
  static const Duration defaultPollWindow = Duration(minutes: 5);

  /// [requestAnalysisTimeout], [pollInterval] and [pollWindow] are overridable so tests can
  /// use short durations instead of the real multi-minute defaults above.
  ObservationApiService({
    http.Client? client,
    Duration requestAnalysisTimeout = defaultRequestAnalysisTimeout,
    Duration pollInterval = defaultPollInterval,
    Duration pollWindow = defaultPollWindow,
  })  : _client = client ?? http.Client(),
        _requestAnalysisTimeout = requestAnalysisTimeout,
        _pollInterval = pollInterval,
        _pollWindow = pollWindow;

  Uri _uri(String path, [Map<String, String>? query]) {
    return ApiConfig.buildUri(AuthService.apiBaseUrl, path, query);
  }

  Future<Map<String, String>> _headers({bool json = true}) async {
    final token = await UserStorage.getAccessToken();
    return {
      if (json) 'Content-Type': 'application/json',
      if (token != null && token.isNotEmpty) 'Authorization': 'Bearer $token',
    };
  }

  Observation _parseObservationOrThrow(http.Response response) {
    if (response.statusCode >= 200 && response.statusCode < 300) {
      return Observation.fromJson(jsonDecode(response.body) as Map<String, dynamic>);
    }
    throw ApiException(_extractError(response.body, response.statusCode));
  }

  String _extractError(String body, int statusCode) {
    try {
      final dynamic decoded = jsonDecode(body);
      if (decoded is Map<String, dynamic>) {
        if (decoded['message'] is String) return decoded['message'] as String;

        if (decoded['errors'] is Map<String, dynamic>) {
          final errors = decoded['errors'] as Map<String, dynamic>;
          for (final value in errors.values) {
            if (value is List && value.isNotEmpty) return value.first.toString();
          }
        }

        if (decoded['title'] is String) return decoded['title'] as String;
      }
    } catch (_) {
      // Not JSON — fall through to the generic message below.
    }
    return 'Request failed with status code $statusCode.';
  }

  /// GET /api/observations — the farmer's own reports, newest first. [cultivationCycleId]
  /// narrows to one cycle, matching the list screen's filter.
  Future<List<Observation>> getObservations({int? cultivationCycleId}) async {
    final response = await _client
        .get(
          _uri(
            '/observations',
            cultivationCycleId == null
                ? null
                : {'cultivationCycleId': cultivationCycleId.toString()},
          ),
          headers: await _headers(json: false),
        )
        .timeout(const Duration(seconds: 15));

    if (response.statusCode >= 200 && response.statusCode < 300) {
      final List<dynamic> data = jsonDecode(response.body) as List<dynamic>;
      return data
          .map((item) => Observation.fromJson(item as Map<String, dynamic>))
          .toList();
    }
    throw ApiException(_extractError(response.body, response.statusCode));
  }

  /// GET /api/observations/{id}.
  Future<Observation> getObservationById(int id) async {
    final response = await _client
        .get(_uri('/observations/$id'), headers: await _headers(json: false))
        .timeout(const Duration(seconds: 15));
    return _parseObservationOrThrow(response);
  }

  /// POST /api/observations. Farmer only; the owner comes from the access token.
  Future<Observation> createObservation(CreateObservationRequest request) async {
    final response = await _client
        .post(
          _uri('/observations'),
          headers: await _headers(),
          body: jsonEncode(request.toJson()),
        )
        .timeout(const Duration(seconds: 15));
    return _parseObservationOrThrow(response);
  }

  /// PUT /api/observations/{id} — only while the observation has no diagnosis yet.
  Future<Observation> updateObservation(int id, UpdateObservationRequest request) async {
    final response = await _client
        .put(
          _uri('/observations/$id'),
          headers: await _headers(),
          body: jsonEncode(request.toJson()),
        )
        .timeout(const Duration(seconds: 15));
    return _parseObservationOrThrow(response);
  }

  /// POST /api/observations/{id}/photo — multipart upload, no manual Content-Type (the
  /// MultipartRequest sets its own boundary). [contentType] must be one of the backend's
  /// accepted image types ("image/jpeg", "image/png", "image/webp", "image/gif").
  Future<Observation> uploadPhoto({
    required int observationId,
    required String filePath,
    required String contentType,
  }) async {
    final uri = _uri('/observations/$observationId/photo');
    final request = http.MultipartRequest('POST', uri);

    final token = await UserStorage.getAccessToken();
    if (token != null && token.isNotEmpty) {
      request.headers['Authorization'] = 'Bearer $token';
    }

    final mediaTypeParts = contentType.split('/');
    request.files.add(await http.MultipartFile.fromPath(
      'file',
      filePath,
      contentType: mediaTypeParts.length == 2
          ? MediaType(mediaTypeParts[0], mediaTypeParts[1])
          : null,
    ));

    final streamedResponse = await _client.send(request).timeout(const Duration(seconds: 30));
    final response = await http.Response.fromStream(streamedResponse);
    return _parseObservationOrThrow(response);
  }

  /// POST /api/observations/{id}/request-analysis. Runs the diagnosis agent, which can take
  /// up to ~240s server-side. A real HTTP error response (e.g. 400 "already has a
  /// diagnosis") is surfaced directly. A network-level failure — the client timeout above,
  /// a dropped connection, no connectivity — does NOT mean the run failed; the backend may
  /// still be working. In that case, fall back to polling rather than reporting a failure we
  /// don't actually know happened.
  Future<Observation> requestAnalysis(int observationId) async {
    try {
      final response = await _client
          .post(
            _uri('/observations/$observationId/request-analysis'),
            headers: await _headers(json: false),
          )
          .timeout(_requestAnalysisTimeout);
      return _parseObservationOrThrow(response);
    } on ApiException {
      rethrow;
    } catch (_) {
      return pollUntilAnalyzed(observationId);
    }
  }

  /// Polls GET /api/observations/{id} every [pollInterval] for up to [pollWindow] until
  /// lastAnalyzedAt is set. A transient GET failure during the window is swallowed and
  /// retried on the next tick — the whole point of polling here is resilience to the same
  /// kind of network flakiness that triggered it in the first place.
  Future<Observation> pollUntilAnalyzed(int observationId) async {
    final deadline = DateTime.now().add(_pollWindow);
    while (DateTime.now().isBefore(deadline)) {
      await Future.delayed(_pollInterval);
      try {
        final observation = await getObservationById(observationId);
        if (observation.lastAnalyzedAt != null) return observation;
      } catch (_) {
        // Keep polling until the deadline.
      }
    }
    throw const ApiException(
      'The diagnosis agent is taking longer than expected. It may still be running — '
      'check back on this report in a few minutes.',
    );
  }

  /// GET /api/cycles — the caller's own cultivation cycles, used by both the create-form
  /// picker and the list's filter.
  Future<List<CultivationCycleSummary>> getCycles() async {
    final response = await _client
        .get(_uri('/cycles'), headers: await _headers(json: false))
        .timeout(const Duration(seconds: 15));

    if (response.statusCode >= 200 && response.statusCode < 300) {
      final List<dynamic> data = jsonDecode(response.body) as List<dynamic>;
      return data
          .map((item) => CultivationCycleSummary.fromJson(item as Map<String, dynamic>))
          .toList();
    }
    throw ApiException(_extractError(response.body, response.statusCode));
  }
}
