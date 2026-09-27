/// Centralized API constants for PaddyWise mobile app.
/// Configurable at build/run time via `--dart-define=API_BASE_URL=...`.
class ApiConstants {
  /// Base URL for backend API calls.
  /// Defaults to Android emulator loopback (10.0.2.2:5164/api).
  /// For physical devices, override with `--dart-define=API_BASE_URL=http://<YOUR_LAN_IP>:5164/api`.
  static const String baseUrl = String.fromEnvironment(
    'API_BASE_URL',
    defaultValue: 'http://10.0.2.2:5164/api',
  );
}
