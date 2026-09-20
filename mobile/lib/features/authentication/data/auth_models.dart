import 'dart:convert';

class AuthUser {
  const AuthUser({
    required this.id,
    required this.firstName,
    required this.lastName,
    required this.email,
    required this.roles,
    required this.permissions,
  });

  factory AuthUser.fromJson(Map<String, dynamic> json) {
    return AuthUser(
      id: json['id'] as String,
      firstName: json['firstName'] as String? ?? '',
      lastName: json['lastName'] as String? ?? '',
      email: json['email'] as String? ?? '',
      roles: (json['roles'] as List<dynamic>? ?? const []).cast<String>(),
      permissions:
          (json['permissions'] as List<dynamic>? ?? const []).cast<String>(),
    );
  }

  final String id;
  final String firstName;
  final String lastName;
  final String email;
  final List<String> roles;
  final List<String> permissions;

  bool get isProvider => roles.contains('ServiceProvider');
  bool get isAdministrator => roles.contains('Administrator');

  String toJson() => jsonEncode({
        'id': id,
        'firstName': firstName,
        'lastName': lastName,
        'email': email,
        'roles': roles,
        'permissions': permissions,
      });
}

class AuthSession {
  const AuthSession({
    required this.accessToken,
    required this.refreshToken,
    required this.user,
  });

  final String accessToken;
  final String refreshToken;
  final AuthUser user;
}
