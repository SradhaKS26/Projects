import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/networking/api_client.dart';
import '../../services/data/catalog_models.dart';
import '../../services/data/catalog_repository.dart';
import 'provider_models.dart';

final providerRepositoryProvider = Provider<ProviderRepository>((ref) {
  return ProviderRepository(
    dio: ref.watch(dioProvider),
    catalog: ref.watch(catalogRepositoryProvider),
  );
});

class ProviderRepository {
  ProviderRepository({
    required Dio dio,
    required CatalogRepository catalog,
  })  : _dio = dio,
        _catalog = catalog;

  final Dio _dio;
  final CatalogRepository _catalog;

  Future<ProviderProfile> fetchMine() async {
    final response = await _dio.get<Map<String, dynamic>>('/providers/me');
    return ProviderProfile.fromJson(_data(response));
  }

  Future<ProviderProfile> saveApplication({
    required String? description,
    required List<String> serviceIds,
  }) async {
    final response = await _dio.put<Map<String, dynamic>>(
      '/providers/me/application',
      data: {
        'description': description,
        'serviceIds': serviceIds,
      },
    );
    return ProviderProfile.fromJson(_data(response));
  }

  Future<ProviderProfile> setAvailability(String status) async {
    final response = await _dio.put<Map<String, dynamic>>(
      '/providers/me/availability',
      data: {'availabilityStatus': status},
    );
    return ProviderProfile.fromJson(_data(response));
  }

  Future<void> uploadDocument({
    required String documentRequirementId,
    required String filePath,
    required String fileName,
  }) async {
    final form = FormData.fromMap({
      'documentRequirementId': documentRequirementId,
      'file': await MultipartFile.fromFile(filePath, filename: fileName),
    });

    await _dio.post<Map<String, dynamic>>('/providers/me/documents', data: form);
  }

  Future<List<CategoryDocumentRequirement>> fetchRequirements(String categoryId) async {
    final response = await _dio.get<Map<String, dynamic>>(
      '/service-categories/$categoryId/document-requirements',
    );
    final data = response.data?['data'] as List<dynamic>? ?? const [];
    return data
        .cast<Map<String, dynamic>>()
        .map(CategoryDocumentRequirement.fromJson)
        .toList(growable: false);
  }

  Future<List<CatalogService>> fetchServices() => _catalog.fetchServices();

  Map<String, dynamic> _data(Response<Map<String, dynamic>> response) {
    final data = response.data?['data'] as Map<String, dynamic>?;
    if (data == null) {
      throw StateError('Provider response was empty.');
    }
    return data;
  }
}

final myProviderProfileProvider =
    FutureProvider.autoDispose<ProviderProfile>((ref) {
  return ref.watch(providerRepositoryProvider).fetchMine();
});
