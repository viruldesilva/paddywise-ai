import 'dart:io' show Platform;
import 'package:flutter/foundation.dart';

/// Centralized configuration and URL building for the PaddyWise backend API.
class ApiConfig {
  /// The production Azure App Service base URL.
  /// (served over HTTPS on port 443 with no port suffix or trailing slash).
  static const String productionBaseUrl =
      'https://paddywise-backend-bubkb2b8gmf2dpe6.malaysiawest-01.azurewebsites.net';

  /// Standard Android emulator mapping to the host development machine.
  static const String emulatorBaseUrl = 'http://10.0.2.2:5164';

  /// Standard localhost mapping for web and desktop local development.
  static const String localBaseUrl = 'http://localhost:5164';

  /// Normalizes any raw base URL:
  /// - trims whitespace
  /// - prepends https:// if no http:// or https:// scheme is present
  /// - removes trailing slashes
  /// - ensures the '/api' suffix is present, as all ASP.NET Core controllers
  ///   in PaddyWise.Api are routed under /api (e.g. /api/auth, /api/cycles).
  static String normalizeBaseUrl(String url) {
    var cleaned = url.trim();
    if (cleaned.isEmpty) return cleaned;

    // Prepend https:// if no scheme is present
    if (!cleaned.startsWith('http://') && !cleaned.startsWith('https://')) {
      cleaned = 'https://$cleaned';
    }

    // Remove any trailing slashes
    cleaned = cleaned.replaceAll(RegExp(r'/+$'), '');

    // Ensure /api suffix exists so endpoints like /auth/login resolve to /api/auth/login
    if (!cleaned.endsWith('/api')) {
      cleaned = '$cleaned/api';
    }

    return cleaned;
  }

  /// Builds a full, safe URI by joining [baseUrl] and [path], preventing double slashes
  /// and preserving host/scheme. Optional [queryParameters] are safely attached.
  static Uri buildUri(
    String baseUrl,
    String path, [
    Map<String, dynamic>? queryParameters,
  ]) {
    final cleanBase = normalizeBaseUrl(baseUrl);
    final cleanPath = path.startsWith('/') ? path : '/$path';
    final fullUrl = '$cleanBase$cleanPath';
    final uri = Uri.parse(fullUrl);

    if (queryParameters != null && queryParameters.isNotEmpty) {
      final sanitizedParams = <String, String>{};
      queryParameters.forEach((key, value) {
        if (value != null) {
          sanitizedParams[key] = value.toString();
        }
      });
      if (sanitizedParams.isNotEmpty) {
        return uri.replace(queryParameters: sanitizedParams);
      }
    }

    return uri;
  }

  /// Resolves the default base URL for the active runtime environment:
  /// 1. If `--dart-define=API_BASE_URL=...` is passed, that takes highest priority.
  /// 2. In release mode (`kReleaseMode`), defaults to [productionBaseUrl].
  /// 3. In debug / local mode:
  ///    - Android emulator -> [emulatorBaseUrl]
  ///    - Web / desktop / other -> [localBaseUrl]
  static String resolveDefaultBaseUrl() {
    const envUrl = String.fromEnvironment('API_BASE_URL', defaultValue: '');
    if (envUrl.isNotEmpty) {
      return normalizeBaseUrl(envUrl);
    }

    if (kReleaseMode) {
      return normalizeBaseUrl(productionBaseUrl);
    }

    if (kIsWeb) {
      return normalizeBaseUrl(localBaseUrl);
    }

    try {
      if (Platform.isAndroid) {
        return normalizeBaseUrl(emulatorBaseUrl);
      }
    } catch (_) {}

    return normalizeBaseUrl(localBaseUrl);
  }

  /// Determines whether a given URL points to a local development backend.
  static bool isLocalUrl(String url) {
    try {
      final uri = Uri.parse(url.startsWith('http') ? url : 'http://$url');
      final host = uri.host.toLowerCase();
      return host == 'localhost' ||
          host == '127.0.0.1' ||
          host == '10.0.2.2' ||
          host.startsWith('192.168.') ||
          host.startsWith('10.') ||
          uri.port == 5164 ||
          uri.port == 7188;
    } catch (_) {
      return false;
    }
  }
}
