import 'dart:convert';
import 'dart:io' show Platform;
import 'package:flutter/foundation.dart';
import 'package:http/http.dart' as http;
import 'package:shared_preferences/shared_preferences.dart';
import '../models/user.dart';

class AuthResponse {
  final String accessToken;
  final String refreshToken;
  final String name;
  final String email;
  final String role;
  final bool requiresApproval;
  final String message;

  const AuthResponse({
    required this.accessToken,
    required this.refreshToken,
    required this.name,
    required this.email,
    required this.role,
    this.requiresApproval = false,
    this.message = '',
  });

  factory AuthResponse.fromJson(Map<String, dynamic> json) {
    return AuthResponse(
      accessToken: json['accessToken'] as String? ?? '',
      refreshToken: json['refreshToken'] as String? ?? '',
      name: json['name'] as String? ?? '',
      email: json['email'] as String? ?? '',
      role: json['role'] as String? ?? '',
      requiresApproval: json['requiresApproval'] as bool? ?? false,
      message: json['message'] as String? ?? '',
    );
  }
}

class RegisterResult {
  final bool requiresApproval;
  final String message;
  final User? user;

  const RegisterResult({
    required this.requiresApproval,
    required this.message,
    this.user,
  });
}

class AuthService {
  static const String _storageKeyUsers = 'kumburu_users';
  static const String _storageKeySession = 'kumburu_session';
  static const String _storageKeyAccessToken = 'kumburu_access_token';
  static const String _storageKeyRefreshToken = 'kumburu_refresh_token';
  static const String _storageKeyApiBaseUrl = 'kumburu_api_base_url';

  static final List<StoredUser> _defaultUsers = [
    StoredUser(
      id: 'usr_farmer_01',
      fullName: 'Bandara Wanninayake',
      email: 'farmer@kumburu.lk',
      role: UserRole.farmer,
      phone: '+94 77 123 4567',
      division: 'Polonnaruwa - Medirigiriya',
      passwordHash: 'Password123!',
      createdAt: DateTime.parse('2026-01-15T00:00:00.000Z'),
    ),
    StoredUser(
      id: 'usr_officer_01',
      fullName: 'Dr. Nilmini Perera',
      email: 'officer@kumburu.lk',
      role: UserRole.extensionOfficer,
      phone: '+94 71 987 6543',
      division: 'Anuradhapura - Nuwaragam Palatha',
      passwordHash: 'Password123!',
      createdAt: DateTime.parse('2026-01-10T00:00:00.000Z'),
    ),
    StoredUser(
      id: 'usr_buyer_01',
      fullName: 'Roshan Fernando (Lanka Rice Mills)',
      email: 'buyer@kumburu.lk',
      role: UserRole.buyer,
      phone: '+94 76 345 6789',
      division: 'Kurunegala',
      passwordHash: 'Password123!',
      createdAt: DateTime.parse('2026-01-20T00:00:00.000Z'),
    ),
    StoredUser(
      id: 'usr_admin_01',
      fullName: 'System Administrator',
      email: 'admin@kumburu.lk',
      role: UserRole.admin,
      phone: '+94 11 234 5678',
      division: 'Colombo HQ',
      passwordHash: 'Password123!',
      createdAt: DateTime.parse('2026-01-01T00:00:00.000Z'),
    ),
  ];

  static List<StoredUser> _memoryUsers = List.from(_defaultUsers);
  static User? _currentUser;
  static String? _accessToken;
  static String? _refreshToken;
  static String? _customBaseUrl;

  /// Default API Base URL based on platform
  static String get defaultApiBaseUrl {
    if (kIsWeb) return 'http://localhost:5164/api';
    try {
      if (Platform.isAndroid) {
        return 'http://10.0.2.2:5164/api';
      }
    } catch (_) {}
    return 'http://localhost:5164/api';
  }

  /// Get current effective API base URL
  static String get apiBaseUrl => _customBaseUrl ?? defaultApiBaseUrl;

  /// Set and persist custom API base URL
  static Future<void> setApiBaseUrl(String url) async {
    final cleanUrl = url.trim().replaceAll(RegExp(r'/+$'), '');
    _customBaseUrl = cleanUrl.isNotEmpty ? cleanUrl : null;
    try {
      final prefs = await SharedPreferences.getInstance();
      if (_customBaseUrl != null) {
        await prefs.setString(_storageKeyApiBaseUrl, _customBaseUrl!);
      } else {
        await prefs.remove(_storageKeyApiBaseUrl);
      }
    } catch (_) {}
  }

  /// Reset API base URL to system default
  static Future<void> resetApiBaseUrl() async {
    _customBaseUrl = null;
    try {
      final prefs = await SharedPreferences.getInstance();
      await prefs.remove(_storageKeyApiBaseUrl);
    } catch (_) {}
  }

  static Future<void> init() async {
    try {
      final prefs = await SharedPreferences.getInstance();

      // Load custom API URL if set
      final savedUrl = prefs.getString(_storageKeyApiBaseUrl);
      if (savedUrl != null && savedUrl.trim().isNotEmpty) {
        _customBaseUrl = savedUrl.trim();
      }

      // Load tokens
      _accessToken = prefs.getString(_storageKeyAccessToken);
      _refreshToken = prefs.getString(_storageKeyRefreshToken);

      // Load local demo users
      final raw = prefs.getString(_storageKeyUsers);
      if (raw == null) {
        await _saveUsersToStorage(_defaultUsers);
        _memoryUsers = List.from(_defaultUsers);
      } else {
        final List<dynamic> list = jsonDecode(raw) as List<dynamic>;
        _memoryUsers = list
            .map((item) => StoredUser.fromJson(item as Map<String, dynamic>))
            .toList();
      }

      // Load existing active session
      final sessionRaw = prefs.getString(_storageKeySession);
      if (sessionRaw != null) {
        final Map<String, dynamic> sessionJson =
            jsonDecode(sessionRaw) as Map<String, dynamic>;
        _currentUser = User.fromJson(sessionJson);
      }
    } catch (_) {
      _memoryUsers = List.from(_defaultUsers);
    }
  }

  static Future<void> _saveUsersToStorage(List<StoredUser> users) async {
    try {
      final prefs = await SharedPreferences.getInstance();
      final jsonString = jsonEncode(users.map((u) => u.toJson()).toList());
      await prefs.setString(_storageKeyUsers, jsonString);
    } catch (_) {}
  }

  static User? getCurrentUser() => _currentUser;
  static String? getAccessToken() => _accessToken;
  static String? getRefreshToken() => _refreshToken;
  static bool get isAuthenticated => _currentUser != null;

  static List<User> getAllUsers() {
    return List<User>.unmodifiable(_memoryUsers);
  }

  /// Attempt login by connecting to the ASP.NET Core backend.
  /// If backend is unreachable and credentials match a seeded demo account,
  /// seamlessly logs into offline demo session so testing is never blocked.
  static Future<User> login({
    required String email,
    required String password,
  }) async {
    final cleanEmail = email.trim();
    final cleanPassword = password.trim();

    if (cleanEmail.isEmpty || cleanPassword.isEmpty) {
      throw Exception('Please fill in both your email address and password.');
    }

    final url = Uri.parse('$apiBaseUrl/auth/login');
    http.Response? response;
    bool networkError = false;
    String networkErrorDetail = '';

    try {
      response = await http
          .post(
            url,
            headers: {'Content-Type': 'application/json'},
            body: jsonEncode({
              'email': cleanEmail,
              'password': cleanPassword,
            }),
          )
          .timeout(const Duration(seconds: 8));
    } catch (e) {
      networkError = true;
      networkErrorDetail = e.toString();
    }

    // 1. Success from Backend
    if (response != null && response.statusCode == 200) {
      try {
        final Map<String, dynamic> data =
            jsonDecode(response.body) as Map<String, dynamic>;
        final authResponse = AuthResponse.fromJson(data);

        _accessToken = authResponse.accessToken;
        _refreshToken = authResponse.refreshToken;

        // Extract user ID from JWT if possible
        String userId = 'usr_${DateTime.now().millisecondsSinceEpoch}';
        if (_accessToken != null && _accessToken!.isNotEmpty) {
          final claims = _decodeJwt(_accessToken!);
          if (claims != null) {
            userId = claims['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'] ??
                claims['sub'] ??
                userId;
          }
        }

        final role = UserRole.fromString(authResponse.role);
        _currentUser = User(
          id: userId,
          fullName: authResponse.name.isNotEmpty ? authResponse.name : 'User',
          email: authResponse.email.isNotEmpty ? authResponse.email : cleanEmail,
          role: role,
          createdAt: DateTime.now(),
          token: _accessToken,
        );

        // Persist session and tokens
        final prefs = await SharedPreferences.getInstance();
        await prefs.setString(
            _storageKeySession, jsonEncode(_currentUser!.toJson()));
        await prefs.setString(_storageKeyAccessToken, _accessToken!);
        await prefs.setString(_storageKeyRefreshToken, _refreshToken!);

        return _currentUser!;
      } catch (e) {
        throw Exception('Failed to parse backend authentication response: $e');
      }
    }

    // 2. Explicit Backend Error (401, 400, etc.)
    if (response != null) {
      final errorMessage = _extractBackendError(
        response.body,
        response.statusCode,
      );
      throw Exception(errorMessage);
    }

    // 3. Network Connection Failure -> Check local demo fallback
    if (networkError) {
      final normalizedEmail = cleanEmail.toLowerCase();
      final matchedDemo = _memoryUsers.where(
        (u) => u.email.toLowerCase() == normalizedEmail,
      );

      if (matchedDemo.isNotEmpty && matchedDemo.first.passwordHash == cleanPassword) {
        final demoUser = matchedDemo.first;
        _currentUser = User(
          id: demoUser.id,
          fullName: demoUser.fullName,
          email: demoUser.email,
          role: demoUser.role,
          phone: demoUser.phone,
          division: demoUser.division,
          createdAt: demoUser.createdAt,
          token: 'offline_demo_token',
        );

        final prefs = await SharedPreferences.getInstance();
        await prefs.setString(
            _storageKeySession, jsonEncode(_currentUser!.toJson()));

        return _currentUser!;
      }

      // If not a demo user or password incorrect:
      throw Exception(
        'Cannot connect to backend server at $apiBaseUrl.\n'
        'Please ensure the backend is running (e.g. dotnet run on port 5164).\n'
        'Details: ${networkErrorDetail.split('\n').first}',
      );
    }

    throw Exception('An unexpected error occurred during sign in.');
  }

  /// Register via backend API, with fallback to local storage if offline
  static Future<RegisterResult> register({
    required String fullName,
    required String email,
    required UserRole role,
    required String password,
    String? phone,
    String? division,
  }) async {
    final cleanName = fullName.trim();
    final cleanEmail = email.trim();
    final cleanPassword = password.trim();

    final url = Uri.parse('$apiBaseUrl/auth/register');
    http.Response? response;
    bool networkError = false;

    try {
      response = await http
          .post(
            url,
            headers: {'Content-Type': 'application/json'},
            body: jsonEncode({
              'name': cleanName,
              'email': cleanEmail,
              'password': cleanPassword,
              'role': role.toBackendString(),
              'phone': phone?.trim(),
            }),
          )
          .timeout(const Duration(seconds: 8));
    } catch (_) {
      networkError = true;
    }

    if (response != null && response.statusCode == 200) {
      final Map<String, dynamic> data =
          jsonDecode(response.body) as Map<String, dynamic>;
      final authResponse = AuthResponse.fromJson(data);

      if (authResponse.requiresApproval) {
        return RegisterResult(
          requiresApproval: true,
          message: authResponse.message.isNotEmpty
              ? authResponse.message
              : 'Your account is pending admin verification. You will be able to log in once approved.',
        );
      }

      _accessToken = authResponse.accessToken;
      _refreshToken = authResponse.refreshToken;

      _currentUser = User(
        id: 'usr_${DateTime.now().millisecondsSinceEpoch}',
        fullName: authResponse.name.isNotEmpty ? authResponse.name : cleanName,
        email: authResponse.email.isNotEmpty ? authResponse.email : cleanEmail,
        role: UserRole.fromString(authResponse.role),
        phone: phone,
        division: division,
        createdAt: DateTime.now(),
        token: _accessToken,
      );

      final prefs = await SharedPreferences.getInstance();
      await prefs.setString(
          _storageKeySession, jsonEncode(_currentUser!.toJson()));
      await prefs.setString(_storageKeyAccessToken, _accessToken!);
      await prefs.setString(_storageKeyRefreshToken, _refreshToken!);

      return RegisterResult(
        requiresApproval: false,
        message: 'Account created successfully!',
        user: _currentUser,
      );
    }

    if (response != null) {
      final errorMsg = _extractBackendError(response.body, response.statusCode);
      throw Exception(errorMsg);
    }

    // Network error -> Local registration fallback
    if (networkError) {
      final normalizedEmail = cleanEmail.toLowerCase();
      final exists = _memoryUsers.any(
        (u) => u.email.toLowerCase() == normalizedEmail,
      );

      if (exists) {
        throw Exception('An account with this email address already exists.');
      }

      if (role == UserRole.extensionOfficer) {
        return const RegisterResult(
          requiresApproval: true,
          message: 'Your account is pending admin verification. You will be able to log in once approved.',
        );
      }

      final newUser = StoredUser(
        id: 'usr_${DateTime.now().millisecondsSinceEpoch.toRadixString(36)}',
        fullName: cleanName,
        email: normalizedEmail,
        role: role,
        phone: phone?.trim().isNotEmpty == true ? phone!.trim() : null,
        division: division?.trim().isNotEmpty == true ? division!.trim() : null,
        passwordHash: cleanPassword,
        createdAt: DateTime.now(),
      );

      _memoryUsers.add(newUser);
      await _saveUsersToStorage(_memoryUsers);

      _currentUser = User(
        id: newUser.id,
        fullName: newUser.fullName,
        email: newUser.email,
        role: newUser.role,
        phone: newUser.phone,
        division: newUser.division,
        createdAt: newUser.createdAt,
      );

      final prefs = await SharedPreferences.getInstance();
      await prefs.setString(
          _storageKeySession, jsonEncode(_currentUser!.toJson()));

      return RegisterResult(
        requiresApproval: false,
        message: 'Account created successfully!',
        user: _currentUser,
      );
    }

    throw Exception('Registration failed. Please try again.');
  }

  static Future<void> logout() async {
    _currentUser = null;
    _accessToken = null;
    _refreshToken = null;
    try {
      final prefs = await SharedPreferences.getInstance();
      await prefs.remove(_storageKeySession);
      await prefs.remove(_storageKeyAccessToken);
      await prefs.remove(_storageKeyRefreshToken);
    } catch (_) {}
  }

  static Future<void> resetToDefaults() async {
    _memoryUsers = List.from(_defaultUsers);
    await _saveUsersToStorage(_memoryUsers);
  }

  /// Parses error responses from ASP.NET Core
  static String _extractBackendError(String responseBody, int statusCode) {
    if (statusCode == 401) {
      return 'Invalid email or password. Please check your credentials.';
    }

    try {
      final dynamic decoded = jsonDecode(responseBody);
      if (decoded is Map<String, dynamic>) {
        if (decoded.containsKey('message') && decoded['message'] is String) {
          return decoded['message'] as String;
        }

        if (decoded.containsKey('errors') &&
            decoded['errors'] is Map<String, dynamic>) {
          final errors = decoded['errors'] as Map<String, dynamic>;
          final firstKey = errors.keys.firstOrNull;
          if (firstKey != null && errors[firstKey] is List) {
            final list = errors[firstKey] as List;
            if (list.isNotEmpty) return list.first.toString();
          }
        }

        if (decoded.containsKey('title') && decoded['title'] is String) {
          return decoded['title'] as String;
        }
      }
    } catch (_) {}

    switch (statusCode) {
      case 400:
        return 'Bad request. Please verify the input values.';
      case 403:
        return 'Your account is pending admin verification. Please check back later.';
      case 404:
        return 'Authentication endpoint not found on server.';
      case 500:
        return 'Internal server error occurred on backend.';
      default:
        return 'Request failed with status code $statusCode.';
    }
  }

  /// Decode JWT payload to read claims
  static Map<String, dynamic>? _decodeJwt(String token) {
    try {
      final parts = token.split('.');
      if (parts.length != 3) return null;
      final payload = parts[1];
      final normalized = base64Url.normalize(payload);
      final decodedBytes = base64Url.decode(normalized);
      final decodedString = utf8.decode(decodedBytes);
      return jsonDecode(decodedString) as Map<String, dynamic>;
    } catch (_) {
      return null;
    }
  }
}
