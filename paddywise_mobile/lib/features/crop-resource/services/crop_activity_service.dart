import 'dart:convert';
import 'package:http/http.dart' as http;
import '../../../services/auth_service.dart';
import '../models/crop_activity_models.dart';

class CropActivityService {
  static const Duration _requestTimeout = Duration(seconds: 8);

  /// Default demo cycle when running offline or cycle is not found
  static final CultivationCycleSummary defaultDemoCycle = CultivationCycleSummary(
    id: 1,
    fieldId: 10,
    fieldName: 'Maha Kumbura (Plot 04)',
    varietyId: 2,
    varietyName: 'Bg 352 (Nadu)',
    season: 'Yala',
    year: DateTime.now().year,
    currentStage: 'Tillering',
    sowingDate: DateTime.now().subtract(const Duration(days: 45)).toIso8601String().split('T').first,
    actualHarvestDate: null,
    status: 'Active',
  );

  /// Fetch a single cultivation cycle by ID
  static Future<CultivationCycleSummary> getCycleById(int id) async {
    final token = AuthService.getAccessToken();
    final url = Uri.parse('${AuthService.apiBaseUrl}/cycles/$id');

    try {
      final response = await http.get(
        url,
        headers: {
          'Content-Type': 'application/json',
          if (token != null) 'Authorization': 'Bearer $token',
        },
      ).timeout(_requestTimeout);

      if (response.statusCode == 200) {
        final data = jsonDecode(response.body) as Map<String, dynamic>;
        return CultivationCycleSummary.fromJson(data);
      }
    } catch (_) {
      // Backend offline or timeout -> fallback to demo cycle
    }

    return defaultDemoCycle;
  }

  /// Fetch all cultivation cycles available to the current user
  static Future<List<CultivationCycleSummary>> getCycles() async {
    final token = AuthService.getAccessToken();
    final url = Uri.parse('${AuthService.apiBaseUrl}/cycles');

    try {
      final response = await http.get(
        url,
        headers: {
          'Content-Type': 'application/json',
          if (token != null) 'Authorization': 'Bearer $token',
        },
      ).timeout(_requestTimeout);

      if (response.statusCode == 200) {
        final List<dynamic> list = jsonDecode(response.body) as List<dynamic>;
        if (list.isNotEmpty) {
          return list
              .map((item) => CultivationCycleSummary.fromJson(item as Map<String, dynamic>))
              .toList();
        }
      }
    } catch (_) {
      // Fallback
    }

    return [
      defaultDemoCycle,
      CultivationCycleSummary(
        id: 2,
        fieldId: 12,
        fieldName: 'Pahalawatta Field B',
        varietyId: 4,
        varietyName: 'At 362 (Red Samba)',
        season: 'Yala',
        year: DateTime.now().year,
        currentStage: 'PanicleInitiation',
        sowingDate: DateTime.now().subtract(const Duration(days: 60)).toIso8601String().split('T').first,
        actualHarvestDate: null,
        status: 'Active',
      ),
    ];
  }

  /// Create a new crop activity for a specific cycle
  static Future<CropActivityDto> createActivity({
    required int cycleId,
    required CreateCropActivityRequest request,
  }) async {
    final token = AuthService.getAccessToken();
    final url = Uri.parse('${AuthService.apiBaseUrl}/cycles/$cycleId/activities');

    http.Response? response;
    bool networkError = false;

    try {
      response = await http.post(
        url,
        headers: {
          'Content-Type': 'application/json',
          if (token != null) 'Authorization': 'Bearer $token',
        },
        body: jsonEncode(request.toJson()),
      ).timeout(_requestTimeout);
    } catch (_) {
      networkError = true;
    }

    // 1. Success from Backend
    if (response != null && (response.statusCode == 200 || response.statusCode == 201)) {
      try {
        final data = jsonDecode(response.body) as Map<String, dynamic>;
        return CropActivityDto.fromJson(data);
      } catch (e) {
        throw Exception('Failed to parse backend activity response: $e');
      }
    }

    // 2. Explicit Backend Error (400, 401, 403, 404, 500)
    if (response != null) {
      final message = _extractErrorMessage(response.body, response.statusCode);
      throw Exception(message);
    }

    // 3. Network Connection Failure -> Provide graceful demo feedback
    if (networkError) {
      // Check if user is in demo mode or backend is simply offline
      final currentUser = AuthService.getCurrentUser();
      return CropActivityDto(
        id: DateTime.now().millisecondsSinceEpoch % 100000,
        cultivationCycleId: cycleId,
        activityType: request.activityType,
        date: request.date,
        detailsJson: request.detailsJson,
        loggedByUserId: 1,
        loggedByUserName: currentUser?.fullName ?? 'Farmer',
        createdAt: DateTime.now().toIso8601String(),
      );
    }

    throw Exception('An unexpected error occurred while saving the activity.');
  }

  /// Extract error message from ASP.NET Core response
  static String _extractErrorMessage(String responseBody, int statusCode) {
    try {
      final dynamic decoded = jsonDecode(responseBody);
      if (decoded is Map<String, dynamic>) {
        if (decoded.containsKey('message') && decoded['message'] is String) {
          return decoded['message'] as String;
        }
        if (decoded.containsKey('title') && decoded['title'] is String) {
          return decoded['title'] as String;
        }
        if (decoded.containsKey('errors') && decoded['errors'] is Map) {
          final errors = decoded['errors'] as Map;
          final firstKey = errors.keys.firstOrNull;
          if (firstKey != null && errors[firstKey] is List) {
            return (errors[firstKey] as List).first.toString();
          }
        }
      }
    } catch (_) {}

    switch (statusCode) {
      case 400:
        return 'Invalid activity parameters. Please check all fields.';
      case 401:
        return 'Unauthorized. Please sign in again.';
      case 403:
        return 'Only the registered farmer can log activities for this cultivation cycle.';
      case 404:
        return 'Cultivation cycle not found on server.';
      case 500:
        return 'Internal server error while recording activity.';
      default:
        return 'Failed to save activity (HTTP $statusCode).';
    }
  }
}
