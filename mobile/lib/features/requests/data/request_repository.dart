import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/networking/api_client.dart';
import 'request_models.dart';

final requestRepositoryProvider = Provider<RequestRepository>((ref) {
  return RequestRepository(dio: ref.watch(dioProvider));
});

class RequestRepository {
  RequestRepository({required Dio dio}) : _dio = dio;

  final Dio _dio;

  Future<ServiceJob> place({
    required String serviceId,
    required String addressLine1,
    String? addressLine2,
    required String city,
    required String state,
    required String postalCode,
    String? notes,
  }) async {
    final response = await _dio.post<Map<String, dynamic>>(
      '/service-requests',
      data: {
        'serviceId': serviceId,
        'addressLine1': addressLine1,
        'addressLine2': addressLine2,
        'city': city,
        'state': state,
        'postalCode': postalCode,
        'notes': notes,
      },
    );
    return ServiceJob.fromJson(_data(response));
  }

  Future<List<ServiceJob>> fetchMine() => _list('/service-requests/mine');

  Future<List<ServiceJob>> fetchInbox() => _list('/service-requests/inbox');

  Future<ServiceJob> accept(String id) async {
    final response = await _dio.post<Map<String, dynamic>>('/service-requests/$id/accept');
    return ServiceJob.fromJson(_data(response));
  }

  Future<ServiceJob> cancel(String id) async {
    final response = await _dio.post<Map<String, dynamic>>('/service-requests/$id/cancel');
    return ServiceJob.fromJson(_data(response));
  }

  Future<List<ServiceJob>> _list(String path) async {
    final response = await _dio.get<Map<String, dynamic>>(path);
    final data = response.data?['data'] as List<dynamic>? ?? const [];
    return data
        .cast<Map<String, dynamic>>()
        .map(ServiceJob.fromJson)
        .toList(growable: false);
  }

  Map<String, dynamic> _data(Response<Map<String, dynamic>> response) {
    final data = response.data?['data'] as Map<String, dynamic>?;
    if (data == null) {
      throw StateError('Request response was empty.');
    }
    return data;
  }
}

final myJobsProvider = FutureProvider.autoDispose<List<ServiceJob>>((ref) {
  return ref.watch(requestRepositoryProvider).fetchMine();
});

final inboxJobsProvider = FutureProvider.autoDispose<List<ServiceJob>>((ref) {
  return ref.watch(requestRepositoryProvider).fetchInbox();
});
