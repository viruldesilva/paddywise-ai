enum UserRole {
  farmer,
  extensionOfficer,
  buyer,
  admin;

  static UserRole fromString(String value) {
    switch (value.toLowerCase().replaceAll(' ', '_')) {
      case 'farmer':
        return UserRole.farmer;
      case 'extension_officer':
      case 'officer':
        return UserRole.extensionOfficer;
      case 'buyer':
      case 'buyer_/_miller':
        return UserRole.buyer;
      case 'admin':
      case 'system_admin':
        return UserRole.admin;
      default:
        return UserRole.farmer;
    }
  }

  String toKey() {
    switch (this) {
      case UserRole.farmer:
        return 'farmer';
      case UserRole.extensionOfficer:
        return 'extension_officer';
      case UserRole.buyer:
        return 'buyer';
      case UserRole.admin:
        return 'admin';
    }
  }

  String get displayName {
    switch (this) {
      case UserRole.farmer:
        return 'Farmer';
      case UserRole.extensionOfficer:
        return 'Extension Officer';
      case UserRole.buyer:
        return 'Buyer / Miller';
      case UserRole.admin:
        return 'System Admin';
    }
  }
}

class User {
  final String id;
  final String fullName;
  final String email;
  final UserRole role;
  final String? phone;
  final String? division;
  final DateTime createdAt;

  const User({
    required this.id,
    required this.fullName,
    required this.email,
    required this.role,
    this.phone,
    this.division,
    required this.createdAt,
  });

  Map<String, dynamic> toJson() {
    return {
      'id': id,
      'fullName': fullName,
      'email': email,
      'role': role.toKey(),
      'phone': phone,
      'division': division,
      'createdAt': createdAt.toIso8601String(),
    };
  }

  factory User.fromJson(Map<String, dynamic> json) {
    return User(
      id: json['id'] as String,
      fullName: json['fullName'] as String,
      email: json['email'] as String,
      role: UserRole.fromString(json['role'] as String),
      phone: json['phone'] as String?,
      division: json['division'] as String?,
      createdAt: DateTime.tryParse(json['createdAt'] as String? ?? '') ??
          DateTime.now(),
    );
  }
}

class StoredUser extends User {
  final String passwordHash;

  const StoredUser({
    required super.id,
    required super.fullName,
    required super.email,
    required super.role,
    super.phone,
    super.division,
    required super.createdAt,
    required this.passwordHash,
  });

  @override
  Map<String, dynamic> toJson() {
    final map = super.toJson();
    map['passwordHash'] = passwordHash;
    return map;
  }

  factory StoredUser.fromJson(Map<String, dynamic> json) {
    return StoredUser(
      id: json['id'] as String,
      fullName: json['fullName'] as String,
      email: json['email'] as String,
      role: UserRole.fromString(json['role'] as String),
      phone: json['phone'] as String?,
      division: json['division'] as String?,
      createdAt: DateTime.tryParse(json['createdAt'] as String? ?? '') ??
          DateTime.now(),
      passwordHash: json['passwordHash'] as String? ?? '',
    );
  }
}
