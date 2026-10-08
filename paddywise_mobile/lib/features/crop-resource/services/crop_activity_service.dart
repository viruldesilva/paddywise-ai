import 'dart:convert';
import 'package:http/http.dart' as http;
import '../../../core/api/api_config.dart';
import '../../../services/auth_service.dart';
import '../models/crop_activity_models.dart';

class CropActivityService {
  static const Duration _requestTimeout = Duration(seconds: 8);

  /// Toggle whether demo / mock fallback data should be returned when backend is unreachable or offline.
  /// Set to false so the application does not display dummy data.
  static bool useDemoFallback = false;

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

  /// Local / demo activities store for seamless offline and responsive operations
  static final List<CropActivityDto> _demoActivities = [
    CropActivityDto(
      id: 201,
      cultivationCycleId: 1,
      activityType: 'Fertilizer',
      date: DateTime.now().subtract(const Duration(days: 2)).toIso8601String().split('T').first,
      detailsJson: jsonEncode({
        'activityType': 'Fertilizer',
        'date': DateTime.now().subtract(const Duration(days: 2)).toIso8601String().split('T').first,
        'type': 'Urea',
        'quantity': 50.0,
        'cropStage': 'Tillering',
        'region': 'Intermediate',
        'method': 'Top Dressing',
      }),
      loggedByUserId: 1,
      loggedByUserName: 'Bandara Wanninayake',
      createdAt: DateTime.now().subtract(const Duration(days: 2)).toIso8601String(),
      fieldName: 'Maha Kumbura (Plot 04)',
      cycleName: 'Yala 2026 · Bg 352',
    ),
    CropActivityDto(
      id: 202,
      cultivationCycleId: 1,
      activityType: 'Irrigation',
      date: DateTime.now().subtract(const Duration(days: 4)).toIso8601String().split('T').first,
      detailsJson: jsonEncode({
        'activityType': 'Irrigation',
        'date': DateTime.now().subtract(const Duration(days: 4)).toIso8601String().split('T').first,
        'waterLevel': 5.0,
        'duration': 3.0,
        'source': 'Canal',
      }),
      loggedByUserId: 1,
      loggedByUserName: 'Bandara Wanninayake',
      createdAt: DateTime.now().subtract(const Duration(days: 4)).toIso8601String(),
      fieldName: 'Maha Kumbura (Plot 04)',
      cycleName: 'Yala 2026 · Bg 352',
    ),
    CropActivityDto(
      id: 203,
      cultivationCycleId: 1,
      activityType: 'Pesticide',
      date: DateTime.now().subtract(const Duration(days: 6)).toIso8601String().split('T').first,
      detailsJson: jsonEncode({
        'activityType': 'Pesticide',
        'date': DateTime.now().subtract(const Duration(days: 6)).toIso8601String().split('T').first,
        'product': 'Chlorantraniliprole',
        'targetPest': 'Stem Borer',
        'quantity': 100.0,
        'method': 'Spraying',
      }),
      loggedByUserId: 1,
      loggedByUserName: 'Bandara Wanninayake',
      createdAt: DateTime.now().subtract(const Duration(days: 6)).toIso8601String(),
      fieldName: 'Maha Kumbura (Plot 04)',
      cycleName: 'Yala 2026 · Bg 352',
    ),
    CropActivityDto(
      id: 204,
      cultivationCycleId: 1,
      activityType: 'Other',
      date: DateTime.now().subtract(const Duration(days: 1)).toIso8601String().split('T').first,
      detailsJson: jsonEncode({
        'activityType': 'Other',
        'date': DateTime.now().subtract(const Duration(days: 1)).toIso8601String().split('T').first,
        'specificActivity': 'Weeding',
        'notes': 'Manual weeding completed across plot 04 with 2 hired laborers.',
      }),
      loggedByUserId: 1,
      loggedByUserName: 'Bandara Wanninayake',
      createdAt: DateTime.now().subtract(const Duration(days: 1)).toIso8601String(),
      fieldName: 'Maha Kumbura (Plot 04)',
      cycleName: 'Yala 2026 · Bg 352',
    ),
  ];

  /// Fetch a single cultivation cycle by ID
  static Future<CultivationCycleSummary> getCycleById(int id) async {
    final token = AuthService.getAccessToken();
    final url = ApiConfig.buildUri(AuthService.apiBaseUrl, '/cycles/$id');

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
      // Backend offline or timeout
    }

    if (useDemoFallback) {
      return defaultDemoCycle;
    }
    throw Exception('Cultivation cycle #$id was not found.');
  }

  /// Fetch all cultivation cycles available to the current user
  static Future<List<CultivationCycleSummary>> getCycles() async {
    final token = AuthService.getAccessToken();
    final url = ApiConfig.buildUri(AuthService.apiBaseUrl, '/cycles');

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
        return list
            .map((item) => CultivationCycleSummary.fromJson(item as Map<String, dynamic>))
            .toList();
      }
    } catch (_) {
      // Fallback
    }

    if (useDemoFallback) {
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

    return [];
  }

  /// Fetch all activities for the logged-in farmer with optional cycle and type filter
  static Future<List<CropActivityDto>> getAllActivities({
    int? cycleId,
    String? activityType,
  }) async {
    final token = AuthService.getAccessToken();
    final queryParams = <String, String>{};
    if (cycleId != null) queryParams['cycleId'] = cycleId.toString();
    if (activityType != null && activityType != 'All') queryParams['activityType'] = activityType;

    final uri = ApiConfig.buildUri(AuthService.apiBaseUrl, '/activities', queryParams);

    try {
      final response = await http.get(
        uri,
        headers: {
          'Content-Type': 'application/json',
          if (token != null) 'Authorization': 'Bearer $token',
        },
      ).timeout(_requestTimeout);

      if (response.statusCode == 200) {
        final List<dynamic> list = jsonDecode(response.body) as List<dynamic>;
        final items = list
            .map((item) => CropActivityDto.fromJson(item as Map<String, dynamic>))
            .toList();
        return items;
      }
    } catch (_) {
      // Fallback to local demo list only if enabled
    }

    if (useDemoFallback) {
      // Filter local demo activities
      return _demoActivities.where((a) {
        if (cycleId != null && a.cultivationCycleId != cycleId) return false;
        if (activityType != null && activityType != 'All' && a.activityType != activityType) {
          return false;
        }
        return true;
      }).toList();
    }

    return [];
  }

  /// Fetch activities specifically recorded for one cultivation cycle
  static Future<List<CropActivityDto>> getActivitiesForCycle(int cycleId) async {
    final token = AuthService.getAccessToken();
    final url = ApiConfig.buildUri(AuthService.apiBaseUrl, '/cycles/$cycleId/activities');

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
        return list
            .map((item) => CropActivityDto.fromJson(item as Map<String, dynamic>))
            .toList();
      }
    } catch (_) {
      // Fallback
    }

    if (useDemoFallback) {
      return _demoActivities.where((a) => a.cultivationCycleId == cycleId).toList();
    }

    return [];
  }

  /// Delete a recorded activity by ID
  static Future<void> deleteActivity(int activityId) async {
    final token = AuthService.getAccessToken();
    final url = ApiConfig.buildUri(AuthService.apiBaseUrl, '/activities/$activityId');

    try {
      final response = await http.delete(
        url,
        headers: {
          'Content-Type': 'application/json',
          if (token != null) 'Authorization': 'Bearer $token',
        },
      ).timeout(_requestTimeout);

      if (response.statusCode == 200 || response.statusCode == 204) {
        if (useDemoFallback) {
          _demoActivities.removeWhere((a) => a.id == activityId);
        }
        return;
      }
    } catch (_) {
      // Fallback
    }

    if (useDemoFallback) {
      _demoActivities.removeWhere((a) => a.id == activityId);
      return;
    }

    throw Exception('Failed to delete activity from server.');
  }

  /// Create a new crop activity for a specific cycle
  static Future<CropActivityDto> createActivity({
    required int cycleId,
    required CreateCropActivityRequest request,
  }) async {
    final token = AuthService.getAccessToken();
    final url = ApiConfig.buildUri(AuthService.apiBaseUrl, '/cycles/$cycleId/activities');

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
        final dto = CropActivityDto.fromJson(data);
        if (useDemoFallback) {
          _demoActivities.insert(0, dto);
        }
        return dto;
      } catch (e) {
        throw Exception('Failed to parse backend activity response: $e');
      }
    }

    // 2. Explicit Backend Error (400, 401, 403, 404, 500)
    if (response != null) {
      final message = _extractErrorMessage(response.body, response.statusCode);
      throw Exception(message);
    }

    // 3. Network Connection Failure
    if (networkError) {
      if (useDemoFallback) {
        final currentUser = AuthService.getCurrentUser();
        final dto = CropActivityDto(
          id: DateTime.now().millisecondsSinceEpoch % 100000,
          cultivationCycleId: cycleId,
          activityType: request.activityType,
          date: request.date,
          detailsJson: request.detailsJson,
          loggedByUserId: 1,
          loggedByUserName: currentUser?.fullName ?? 'Farmer',
          createdAt: DateTime.now().toIso8601String(),
          fieldName: 'Cultivated Field',
          cycleName: 'Active Cycle',
        );
        _demoActivities.insert(0, dto);
        return dto;
      }
      throw Exception('Network connection error. Could not connect to server.');
    }

    throw Exception('An unexpected error occurred while saving the activity.');
  }

  /// Fetch a single activity by ID
  static Future<CropActivityDto?> getActivityById(int id) async {
    try {
      final all = await getAllActivities();
      for (final a in all) {
        if (a.id == id) return a;
      }
    } catch (_) {}

    if (useDemoFallback) {
      for (final a in _demoActivities) {
        if (a.id == id) return a;
      }
    }

    return null;
  }

  /// Update an existing crop activity by ID
  static Future<CropActivityDto> updateActivity({
    required int activityId,
    required UpdateCropActivityRequest request,
  }) async {
    final token = AuthService.getAccessToken();
    final url = ApiConfig.buildUri(AuthService.apiBaseUrl, '/activities/$activityId');

    http.Response? response;
    bool networkError = false;

    try {
      response = await http.put(
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
    if (response != null && (response.statusCode == 200 || response.statusCode == 204)) {
      try {
        final data = jsonDecode(response.body) as Map<String, dynamic>;
        final dto = CropActivityDto.fromJson(data);
        if (useDemoFallback) {
          final index = _demoActivities.indexWhere((a) => a.id == activityId);
          if (index != -1) {
            _demoActivities[index] = dto;
          }
        }
        return dto;
      } catch (e) {
        throw Exception('Failed to parse backend activity response: $e');
      }
    }

    // 2. Explicit Backend Error (400, 401, 403, 404, 500)
    if (response != null) {
      final message = _extractErrorMessage(response.body, response.statusCode);
      throw Exception(message);
    }

    // 3. Network Connection Failure
    if (networkError) {
      if (useDemoFallback) {
        final index = _demoActivities.indexWhere((a) => a.id == activityId);
        final existing = index != -1 ? _demoActivities[index] : null;
        final dto = CropActivityDto(
          id: activityId,
          cultivationCycleId: existing?.cultivationCycleId ?? 1,
          activityType: request.activityType,
          date: request.date,
          detailsJson: request.detailsJson,
          loggedByUserId: existing?.loggedByUserId ?? 1,
          loggedByUserName: existing?.loggedByUserName ?? 'Farmer',
          createdAt: existing?.createdAt ?? DateTime.now().toIso8601String(),
          fieldName: existing?.fieldName ?? 'Cultivated Field',
          farmerName: existing?.farmerName,
          farmerId: existing?.farmerId,
          cycleName: existing?.cycleName ?? 'Active Cycle',
        );
        if (index != -1) {
          _demoActivities[index] = dto;
        } else {
          _demoActivities.insert(0, dto);
        }
        return dto;
      }
      throw Exception('Network connection error. Could not connect to server.');
    }

    throw Exception('An unexpected error occurred while updating the activity.');
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
