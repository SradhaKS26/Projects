class ProviderProfile {
  const ProviderProfile({
    required this.id,
    required this.approvalStatus,
    required this.isActive,
    required this.availabilityStatus,
    this.description,
    this.reviewReason,
    required this.services,
    required this.documents,
    required this.missingRequiredDocuments,
  });

  factory ProviderProfile.fromJson(Map<String, dynamic> json) {
    return ProviderProfile(
      id: json['id'] as String,
      approvalStatus: json['approvalStatus'] as String? ?? 'pending',
      isActive: json['isActive'] as bool? ?? true,
      availabilityStatus: json['availabilityStatus'] as String? ?? 'unavailable',
      description: json['description'] as String?,
      reviewReason: json['reviewReason'] as String?,
      services: _list(json['services'], ProviderServiceItem.fromJson),
      documents: _list(json['documents'], ProviderDocument.fromJson),
      missingRequiredDocuments: _list(
        json['missingRequiredDocuments'],
        MissingDocumentRequirement.fromJson,
      ),
    );
  }

  final String id;
  final String approvalStatus;
  final bool isActive;
  final String availabilityStatus;
  final String? description;
  final String? reviewReason;
  final List<ProviderServiceItem> services;
  final List<ProviderDocument> documents;
  final List<MissingDocumentRequirement> missingRequiredDocuments;

  bool get isApproved => approvalStatus == 'approved';
  bool get canEdit => approvalStatus == 'pending' || approvalStatus == 'rejected';
}

class ProviderServiceItem {
  const ProviderServiceItem({
    required this.serviceId,
    required this.serviceName,
    required this.categoryId,
    required this.categoryName,
    required this.status,
  });

  factory ProviderServiceItem.fromJson(Map<String, dynamic> json) {
    return ProviderServiceItem(
      serviceId: json['serviceId'] as String,
      serviceName: json['serviceName'] as String,
      categoryId: json['categoryId'] as String,
      categoryName: json['categoryName'] as String,
      status: json['status'] as String? ?? 'pending',
    );
  }

  final String serviceId;
  final String serviceName;
  final String categoryId;
  final String categoryName;
  final String status;
}

class ProviderDocument {
  const ProviderDocument({
    required this.id,
    required this.documentRequirementId,
    required this.requirementName,
    required this.originalFileName,
  });

  factory ProviderDocument.fromJson(Map<String, dynamic> json) {
    return ProviderDocument(
      id: json['id'] as String,
      documentRequirementId: json['documentRequirementId'] as String,
      requirementName: json['requirementName'] as String,
      originalFileName: json['originalFileName'] as String,
    );
  }

  final String id;
  final String documentRequirementId;
  final String requirementName;
  final String originalFileName;
}

class MissingDocumentRequirement {
  const MissingDocumentRequirement({
    required this.id,
    required this.categoryName,
    required this.name,
  });

  factory MissingDocumentRequirement.fromJson(Map<String, dynamic> json) {
    return MissingDocumentRequirement(
      id: json['id'] as String,
      categoryName: json['categoryName'] as String,
      name: json['name'] as String,
    );
  }

  final String id;
  final String categoryName;
  final String name;
}

class CategoryDocumentRequirement {
  const CategoryDocumentRequirement({
    required this.id,
    required this.categoryId,
    required this.categoryName,
    required this.name,
    this.description,
    required this.isRequired,
  });

  factory CategoryDocumentRequirement.fromJson(Map<String, dynamic> json) {
    return CategoryDocumentRequirement(
      id: json['id'] as String,
      categoryId: json['categoryId'] as String,
      categoryName: json['categoryName'] as String,
      name: json['name'] as String,
      description: json['description'] as String?,
      isRequired: json['isRequired'] as bool? ?? true,
    );
  }

  final String id;
  final String categoryId;
  final String categoryName;
  final String name;
  final String? description;
  final bool isRequired;
}

List<T> _list<T>(
  dynamic raw,
  T Function(Map<String, dynamic>) fromJson,
) {
  if (raw is! List) {
    return const [];
  }

  return raw
      .cast<Map<String, dynamic>>()
      .map(fromJson)
      .toList(growable: false);
}
