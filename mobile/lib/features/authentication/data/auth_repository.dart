import 'dart:convert';

import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/networking/api_client.dart';
import '../../../core/storage/token_storage.dart';
import 'auth_models.dart';

final authRepositoryProvider = Provider<AuthRepository>((ref) {
  return AuthRepository(
    dio: ref.watch(dioProvider),
    tokenStorage: ref.watch(tokenStorageProvider),
  );
});

class AuthRepository {
  AuthRepository({
    required Dio dio,
    required TokenStorage tokenStorage,
  })  : _dio = dio,
        _tokenStorage = tokenStorage;

  final Dio _dio;
  final TokenStorage _tokenStorage;

  Future<AuthSession> login({
    required String email,
    required String password,
  }) {
    return _authenticate(
      '/auth/login',
      {
        'email': email,
        'password': password,
      },
    );
  }

  Future<AuthSession> register({
    required String firstName,
    required String lastName,
    required String email,
    required String password,
    required String role,
  }) {
    return _authenticate(
      '/auth/register',
      {
        'firstName': firstName,
        'lastName': lastName,
        'email': email,
        'password': password,
        'role': role,
      },
    );
  }

  Future<AuthUser?> currentUser() async {
    final raw = await _tokenStorage.readUserJson();
    if (raw == null || raw.isEmpty) {
      return null;
    }

    return AuthUser.fromJson(jsonDecode(raw) as Map<String, dynamic>);
  }

  Future<void> logout() => _tokenStorage.clear();

  Future<AuthSession> _authenticate(
    String path,
    Map<String, dynamic> body,
  ) async {
    final response = await _dio.post<Map<String, dynamic>>(path, data: body);
    final data = response.data?['data'] as Map<String, dynamic>?;
    if (data == null) {
      throw StateError('Auth response was empty.');
    }

    final user = AuthUser.fromJson(data['user'] as Map<String, dynamic>);
    final session = AuthSession(
      accessToken: data['accessToken'] as String,
      refreshToken: data['refreshToken'] as String,
      user: user,
    );

    await _tokenStorage.saveTokens(
      accessToken: session.accessToken,
      refreshToken: session.refreshToken,
    );
    await _tokenStorage.saveUserJson(user.toJson());
    return session;
  }
}
