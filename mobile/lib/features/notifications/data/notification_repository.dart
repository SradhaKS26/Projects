import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/networking/api_client.dart';
import 'notification_models.dart';

final notificationRepositoryProvider = Provider<NotificationRepository>((ref) {
  return NotificationRepository(dio: ref.watch(dioProvider));
});

class NotificationRepository {
  NotificationRepository({required Dio dio}) : _dio = dio;

  final Dio _dio;

  Future<List<AppNotification>> fetchMine() async {
    final response = await _dio.get<Map<String, dynamic>>('/notifications');
    final data = response.data?['data'] as List<dynamic>? ?? const [];
    return data
        .cast<Map<String, dynamic>>()
        .map(AppNotification.fromJson)
        .toList(growable: false);
  }

  Future<void> markRead(String id) async {
    await _dio.post<Map<String, dynamic>>('/notifications/$id/read');
  }
}

final notificationsProvider = FutureProvider.autoDispose<List<AppNotification>>((ref) {
  return ref.watch(notificationRepositoryProvider).fetchMine();
});
