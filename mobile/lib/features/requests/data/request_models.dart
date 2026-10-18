class RequestContact {
  const RequestContact({
    required this.userId,
    required this.firstName,
    required this.lastName,
    this.email,
    this.phoneNumber,
  });

  factory RequestContact.fromJson(Map<String, dynamic> json) {
    return RequestContact(
      userId: json['userId'] as String,
      firstName: json['firstName'] as String? ?? '',
      lastName: json['lastName'] as String? ?? '',
      email: json['email'] as String?,
      phoneNumber: json['phoneNumber'] as String?,
    );
  }

  final String userId;
  final String firstName;
  final String lastName;
  final String? email;
  final String? phoneNumber;

  String get displayName => '$firstName $lastName'.trim();
}

class RequestAddress {
  const RequestAddress({
    required this.addressLine1,
    this.addressLine2,
    required this.city,
    required this.state,
    required this.postalCode,
  });

  factory RequestAddress.fromJson(Map<String, dynamic> json) {
    return RequestAddress(
      addressLine1: json['addressLine1'] as String? ?? '',
      addressLine2: json['addressLine2'] as String?,
      city: json['city'] as String? ?? '',
      state: json['state'] as String? ?? '',
      postalCode: json['postalCode'] as String? ?? '',
    );
  }

  final String addressLine1;
  final String? addressLine2;
  final String city;
  final String state;
  final String postalCode;

  String get summary =>
      [addressLine1, if (addressLine2 != null) addressLine2, city, state, postalCode]
          .whereType<String>()
          .where((part) => part.isNotEmpty)
          .join(', ');
}

class ServiceJob {
  const ServiceJob({
    required this.id,
    required this.serviceId,
    required this.serviceName,
    required this.categoryName,
    required this.price,
    required this.status,
    required this.requestedDate,
    required this.expiresAt,
    this.completedAt,
    this.notes,
    this.address,
    this.customer,
    this.provider,
  });

  factory ServiceJob.fromJson(Map<String, dynamic> json) {
    return ServiceJob(
      id: json['id'] as String,
      serviceId: json['serviceId'] as String,
      serviceName: json['serviceName'] as String? ?? '',
      categoryName: json['categoryName'] as String? ?? '',
      price: (json['price'] as num?)?.toDouble() ?? 0,
      status: json['status'] as String? ?? 'pending',
      requestedDate: DateTime.tryParse(json['requestedDate'] as String? ?? '') ?? DateTime.now(),
      expiresAt: DateTime.tryParse(json['expiresAt'] as String? ?? '') ?? DateTime.now(),
      completedAt: DateTime.tryParse(json['completedAt'] as String? ?? ''),
      notes: json['notes'] as String?,
      address: json['address'] is Map<String, dynamic>
          ? RequestAddress.fromJson(json['address'] as Map<String, dynamic>)
          : null,
      customer: json['customer'] is Map<String, dynamic>
          ? RequestContact.fromJson(json['customer'] as Map<String, dynamic>)
          : null,
      provider: json['provider'] is Map<String, dynamic>
          ? RequestContact.fromJson(json['provider'] as Map<String, dynamic>)
          : null,
    );
  }

  final String id;
  final String serviceId;
  final String serviceName;
  final String categoryName;
  final double price;
  final String status;
  final DateTime requestedDate;
  final DateTime expiresAt;
  final DateTime? completedAt;
  final String? notes;
  final RequestAddress? address;
  final RequestContact? customer;
  final RequestContact? provider;

  bool get isPending => status == 'pending';
  bool get isAccepted => status == 'accepted';
  bool get isInProgress => status == 'inProgress';
  bool get isCompleted => status == 'completed';
  bool get isFailed => status == 'failed';
  bool get isExpired => status == 'expired';
  bool get isCancelled => status == 'cancelled';

  String get statusLabel {
    return switch (status) {
      'pending' => 'Pending',
      'accepted' => 'Accepted',
      'inProgress' => 'In progress',
      'completed' => 'Completed',
      'failed' => 'Failed',
      'cancelled' => 'Cancelled',
      'expired' => 'Expired',
      _ => status,
    };
  }
}
