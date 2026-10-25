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

class JobReview {
  const JobReview({
    required this.id,
    required this.rating,
    this.comment,
    required this.createdAt,
    this.serviceName,
    this.categoryName,
  });

  factory JobReview.fromJson(Map<String, dynamic> json) {
    return JobReview(
      id: json['id'] as String,
      rating: json['rating'] as int? ?? 0,
      comment: json['comment'] as String?,
      createdAt: DateTime.tryParse(json['createdAt'] as String? ?? '') ?? DateTime.now(),
      serviceName: json['serviceName'] as String?,
      categoryName: json['categoryName'] as String?,
    );
  }

  final String id;
  final int rating;
  final String? comment;
  final DateTime createdAt;
  final String? serviceName;
  final String? categoryName;
}

class JobStatusEvent {
  const JobStatusEvent({
    this.oldStatus,
    required this.newStatus,
    required this.changedAt,
    this.notes,
  });

  factory JobStatusEvent.fromJson(Map<String, dynamic> json) {
    return JobStatusEvent(
      oldStatus: json['oldStatus'] as String?,
      newStatus: json['newStatus'] as String? ?? '',
      changedAt: DateTime.tryParse(json['changedAt'] as String? ?? '') ?? DateTime.now(),
      notes: json['notes'] as String?,
    );
  }

  final String? oldStatus;
  final String newStatus;
  final DateTime changedAt;
  final String? notes;

  String get label {
    return switch (newStatus) {
      'pending' => 'Requested',
      'accepted' => 'Accepted',
      'inProgress' => 'Started',
      'completed' => 'Completed',
      'failed' => 'Could not complete',
      'cancelled' => 'Cancelled',
      'expired' => 'Expired',
      _ => newStatus,
    };
  }
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
    this.providerRating,
    this.providerReviewCount = 0,
    this.review,
    this.timeline = const [],
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
      providerRating: (json['providerRating'] as num?)?.toDouble(),
      providerReviewCount: json['providerReviewCount'] as int? ?? 0,
      review: json['review'] is Map<String, dynamic>
          ? JobReview.fromJson(json['review'] as Map<String, dynamic>)
          : null,
      timeline: json['timeline'] is List
          ? (json['timeline'] as List)
              .whereType<Map<String, dynamic>>()
              .map(JobStatusEvent.fromJson)
              .toList(growable: false)
          : const [],
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
  final double? providerRating;
  final int providerReviewCount;
  final JobReview? review;
  final List<JobStatusEvent> timeline;

  bool get isPending => status == 'pending';
  bool get isAccepted => status == 'accepted';
  bool get isInProgress => status == 'inProgress';
  bool get isCompleted => status == 'completed';
  bool get isFailed => status == 'failed';
  bool get isExpired => status == 'expired';
  bool get isCancelled => status == 'cancelled';
  bool get isOpen => isPending || isAccepted || isInProgress;
  bool get canReview => isCompleted && review == null;

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
