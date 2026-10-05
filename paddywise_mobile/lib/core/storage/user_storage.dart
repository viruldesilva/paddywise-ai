import 'dart:convert';
import 'package:shared_preferences/shared_preferences.dart';
import '../../models/user.dart';
import '../../services/auth_service.dart';

/// Centralized storage reader for current session and user profile data.
class UserStorage {
  static const String _storageKeySession = 'kumburu_session';
  static const String _storageKeyAccessToken = 'kumburu_access_token';

  /// Returns the currently active authenticated user, or null if not logged in.
  static User? getCurrentUser() {
    return AuthService.getCurrentUser();
  }

  /// Get the current user's role as a normalized backend string (e.g. "Farmer", "AgriculturalOfficer", "Admin", "FieldOfficer")
  static String? getCurrentUserRole() {
    final user = AuthService.getCurrentUser();
    if (user == null) return null;
    return user.role.toBackendString();
  }

  /// Read token directly from storage
  static Future<String?> getAccessToken() async {
    final cached = AuthService.getAccessToken();
    if (cached != null && cached.isNotEmpty) return cached;
    try {
      final prefs = await SharedPreferences.getInstance();
      return prefs.getString(_storageKeyAccessToken);
    } catch (_) {
      return null;
    }
  }

  /// Read user session from storage asynchronously
  static Future<User?> readUserFromStorage() async {
    try {
      final prefs = await SharedPreferences.getInstance();
      final raw = prefs.getString(_storageKeySession);
      if (raw != null) {
        final Map<String, dynamic> data = jsonDecode(raw) as Map<String, dynamic>;
        return User.fromJson(data);
      }
    } catch (_) {}
    return AuthService.getCurrentUser();
  }
}
