class ServiceCategory {
  const ServiceCategory({
    required this.id,
    required this.name,
    this.description,
    this.imageUrl,
    required this.isActive,
    required this.serviceCount,
    this.fulfillmentKind = 'onSite',
  });

  factory ServiceCategory.fromJson(Map<String, dynamic> json) {
    return ServiceCategory(
      id: json['id'] as String,
      name: json['name'] as String,
      description: json['description'] as String?,
      imageUrl: json['imageUrl'] as String?,
      isActive: json['isActive'] as bool? ?? true,
      serviceCount: json['serviceCount'] as int? ?? 0,
      fulfillmentKind: json['fulfillmentKind'] as String? ?? 'onSite',
    );
  }

  final String id;
  final String name;
  final String? description;
  final String? imageUrl;
  final bool isActive;
  final int serviceCount;
  final String fulfillmentKind;

  bool get isRide => fulfillmentKind == 'ride';
  bool get isParcel => fulfillmentKind == 'parcel';
}

class CatalogService {
  const CatalogService({
    required this.id,
    required this.categoryId,
    required this.categoryName,
    required this.name,
    this.description,
    required this.basePrice,
    required this.isActive,
    this.estimatedDurationMinutes,
    this.fulfillmentKind = 'onSite',
  });

  factory CatalogService.fromJson(Map<String, dynamic> json) {
    return CatalogService(
      id: json['id'] as String,
      categoryId: json['categoryId'] as String,
      categoryName: json['categoryName'] as String? ?? '',
      name: json['name'] as String,
      description: json['description'] as String?,
      basePrice: (json['basePrice'] as num).toDouble(),
      isActive: json['isActive'] as bool? ?? true,
      estimatedDurationMinutes: json['estimatedDurationMinutes'] as int?,
      fulfillmentKind: json['fulfillmentKind'] as String? ?? 'onSite',
    );
  }

  final String id;
  final String categoryId;
  final String categoryName;
  final String name;
  final String? description;
  final double basePrice;
  final bool isActive;
  final int? estimatedDurationMinutes;
  final String fulfillmentKind;

  bool get isRide => fulfillmentKind == 'ride';
  bool get isParcel => fulfillmentKind == 'parcel';
  bool get isPointToPoint => isRide || isParcel;
}
