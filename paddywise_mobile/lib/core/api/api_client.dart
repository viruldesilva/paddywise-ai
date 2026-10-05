import 'dart:async';
import 'dart:convert';

import 'package:http/http.dart' as http;

import '../../services/auth_service.dart';
import '../router/app_router.dart';

/// A failed API call, carrying the message the backend sent when it sent one.
class ApiException implements Exception {
  final String message;
  final int? statusCode;

  const ApiException(this.message, {this.statusCode});

  @override
  String toString() => message;
}

/// Thin JSON client over `package:http` for authenticated backend calls.
///
/// Every request carries the bearer token from [AuthService]. A 401 triggers
/// one refresh through `/auth/refresh` and a single retry; if that fails the
/// session is cleared and the app returns to the login screen.
class ApiClient {
  static const Duration defaultTimeout = Duration(seconds: 15);

  static Future<dynamic> get(String path, {Map<String, String>? query, Duration? timeout}) =>
      _send('GET', path, query: query, timeout: timeout);

  static Future<dynamic> post(String path, {Object? body, Duration? timeout}) =>
      _send('POST', path, body: body, timeout: timeout);

  static Future<dynamic> put(String path, {Object? body, Duration? timeout}) =>
      _send('PUT', path, body: body, timeout: timeout);

  static Future<dynamic> patch(String path, {Object? body, Duration? timeout}) =>
      _send('PATCH', path, body: body, timeout: timeout);

  static Future<dynamic> delete(String path, {Duration? timeout}) =>
      _send('DELETE', path, timeout: timeout);

  static Future<dynamic> _send(
    String method,
    String path, {
    Map<String, String>? query,
    Object? body,
    Duration? timeout,
  }) async {
    final uri = Uri.parse('${AuthService.apiBaseUrl}$path').replace(
      queryParameters: (query == null || query.isEmpty) ? null : query,
    );

    var response = await _attempt(method, uri, body, timeout ?? defaultTimeout);

    if (response.statusCode == 401) {
      if (await AuthService.refreshSession()) {
        response = await _attempt(method, uri, body, timeout ?? defaultTimeout);
      }
      if (response.statusCode == 401) {
        await AuthService.logout();
        appRouter.go('/login');
        throw const ApiException(
          'Your session has expired. Please sign in again.',
          statusCode: 401,
        );
      }
    }

    if (response.statusCode >= 200 && response.statusCode < 300) {
      if (response.body.isEmpty) return null;
      return jsonDecode(response.body);
    }

    throw ApiException(
      _errorMessage(response.body, response.statusCode),
      statusCode: response.statusCode,
    );
  }

  static Future<http.Response> _attempt(
    String method,
    Uri uri,
    Object? body,
    Duration timeout,
  ) async {
    final request = http.Request(method, uri)
      ..headers.addAll({
        'Content-Type': 'application/json',
        'Accept': 'application/json',
        if (AuthService.getAccessToken() case final token? when token.isNotEmpty)
          'Authorization': 'Bearer $token',
      });
    if (body != null) request.body = jsonEncode(body);

    try {
      final streamed = await request.send().timeout(timeout);
      return await http.Response.fromStream(streamed).timeout(timeout);
    } on TimeoutException {
      throw const ApiException(
        'The server took too long to respond. Please try again.',
      );
    } catch (_) {
      throw const ApiException(
        'Cannot reach the Kumburu server. Check your connection and try again.',
      );
    }
  }

  /// Unwraps the backend's `{ message }`, ValidationProblemDetails `errors`,
  /// or ProblemDetails `title`, falling back to a status-based message.
  static String _errorMessage(String body, int statusCode) {
    try {
      final decoded = jsonDecode(body);
      if (decoded is Map<String, dynamic>) {
        final message = decoded['message'];
        if (message is String && message.isNotEmpty) return message;

        final errors = decoded['errors'];
        if (errors is Map<String, dynamic>) {
          for (final value in errors.values) {
            if (value is List && value.isNotEmpty) return value.first.toString();
          }
        }

        final title = decoded['title'];
        if (title is String && title.isNotEmpty) return title;
      }
    } catch (_) {}

    switch (statusCode) {
      case 400:
        return 'Some of the details were not accepted. Please check and try again.';
      case 403:
        return 'You do not have access to this record.';
      case 404:
        return 'That record could not be found.';
      default:
        return 'Something went wrong on the server (error $statusCode).';
    }
  }
}
