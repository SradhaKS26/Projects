import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/networking/api_client.dart';
import 'catalog_models.dart';

final catalogRepositoryProvider = Provider<CatalogRepository>((ref) {
  return CatalogRepository(dio: ref.watch(dioProvider));
});

class CatalogRepository {
  CatalogRepository({required Dio dio}) : _dio = dio;

  final Dio _dio;

  Future<List<ServiceCategory>> fetchCategories() async {
    final response = await _dio.get<Map<String, dynamic>>('/service-categories');
    return _listFrom(response, ServiceCategory.fromJson);
  }

  Future<List<CatalogService>> fetchServices({
    String? categoryId,
    String? search,
  }) async {
    final response = await _dio.get<Map<String, dynamic>>(
      '/services',
      queryParameters: <String, dynamic>{
        if (categoryId != null) 'categoryId': categoryId,
        if (search != null && search.trim().isNotEmpty) 'search': search.trim(),
      },
    );

    return _listFrom(response, CatalogService.fromJson);
  }

  List<T> _listFrom<T>(
    Response<Map<String, dynamic>> response,
    T Function(Map<String, dynamic>) fromJson,
  ) {
    final data = response.data?['data'] as List<dynamic>?;
    if (data == null) {
      return const [];
    }

    return data
        .cast<Map<String, dynamic>>()
        .map(fromJson)
        .toList(growable: false);
  }
}

final serviceCategoriesProvider =
    FutureProvider.autoDispose<List<ServiceCategory>>((ref) {
  return ref.watch(catalogRepositoryProvider).fetchCategories();
});

/// Services for a category, or the whole catalog when [categoryId] is null.
final servicesProvider = FutureProvider.autoDispose
    .family<List<CatalogService>, String?>((ref, categoryId) {
  return ref.watch(catalogRepositoryProvider).fetchServices(categoryId: categoryId);
});
