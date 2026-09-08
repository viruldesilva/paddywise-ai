import 'dart:convert';
import 'package:shared_preferences/shared_preferences.dart';
import '../models/user.dart';

class AuthService {
  static const String _storageKeyUsers = 'kumburu_users';
  static const String _storageKeySession = 'kumburu_session';

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

  // In-memory fallback
  static List<StoredUser> _memoryUsers = List.from(_defaultUsers);
  static User? _currentUser;

  static Future<void> init() async {
    try {
      final prefs = await SharedPreferences.getInstance();
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

      final sessionRaw = prefs.getString(_storageKeySession);
      if (sessionRaw != null) {
        final Map<String, dynamic> sessionJson =
            jsonDecode(sessionRaw) as Map<String, dynamic>;
        _currentUser = User.fromJson(sessionJson);
      }
    } catch (_) {
      // In-memory fallback if storage access fails
      _memoryUsers = List.from(_defaultUsers);
    }
  }

  static Future<void> _saveUsersToStorage(List<StoredUser> users) async {
    try {
      final prefs = await SharedPreferences.getInstance();
      final jsonString = jsonEncode(users.map((u) => u.toJson()).toList());
      await prefs.setString(_storageKeyUsers, jsonString);
    } catch (_) {
      // Fallback in memory
    }
  }

  static User? getCurrentUser() {
    return _currentUser;
  }

  static List<User> getAllUsers() {
    return List<User>.unmodifiable(_memoryUsers);
  }

  static Future<User> login({
    required String email,
    required String password,
  }) async {
    // Artificial small delay matching realistic auth
    await Future.delayed(const Duration(milliseconds: 300));

    final normalizedEmail = email.trim().toLowerCase();
    final matched = _memoryUsers.where(
      (u) => u.email.toLowerCase() == normalizedEmail,
    );

    if (matched.isEmpty) {
      throw Exception('Invalid email or password. Please try again.');
    }

    final user = matched.first;
    if (user.passwordHash != password) {
      throw Exception('Invalid email or password. Please try again.');
    }

    _currentUser = User(
      id: user.id,
      fullName: user.fullName,
      email: user.email,
      role: user.role,
      phone: user.phone,
      division: user.division,
      createdAt: user.createdAt,
    );

    try {
      final prefs = await SharedPreferences.getInstance();
      await prefs.setString(
          _storageKeySession, jsonEncode(_currentUser!.toJson()));
    } catch (_) {}

    return _currentUser!;
  }

  static Future<User> register({
    required String fullName,
    required String email,
    required UserRole role,
    required String password,
    String? phone,
    String? division,
  }) async {
    await Future.delayed(const Duration(milliseconds: 350));

    final normalizedEmail = email.trim().toLowerCase();
    final exists = _memoryUsers.any(
      (u) => u.email.toLowerCase() == normalizedEmail,
    );

    if (exists) {
      throw Exception('An account with this email address already exists.');
    }

    final newUser = StoredUser(
      id: 'usr_${DateTime.now().millisecondsSinceEpoch.toRadixString(36)}',
      fullName: fullName.trim(),
      email: normalizedEmail,
      role: role,
      phone: phone?.trim().isNotEmpty == true ? phone!.trim() : null,
      division: division?.trim().isNotEmpty == true ? division!.trim() : null,
      passwordHash: password,
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

    try {
      final prefs = await SharedPreferences.getInstance();
      await prefs.setString(
          _storageKeySession, jsonEncode(_currentUser!.toJson()));
    } catch (_) {}

    return _currentUser!;
  }

  static Future<void> logout() async {
    _currentUser = null;
    try {
      final prefs = await SharedPreferences.getInstance();
      await prefs.remove(_storageKeySession);
    } catch (_) {}
  }

  static Future<void> resetToDefaults() async {
    _memoryUsers = List.from(_defaultUsers);
    await _saveUsersToStorage(_memoryUsers);
  }
}
