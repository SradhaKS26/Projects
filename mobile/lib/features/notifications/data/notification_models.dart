class AppNotification {
  const AppNotification({
    required this.id,
    required this.title,
    required this.body,
    this.serviceRequestId,
    required this.isRead,
    required this.createdAt,
  });

  factory AppNotification.fromJson(Map<String, dynamic> json) {
    return AppNotification(
      id: json['id'] as String,
      title: json['title'] as String? ?? '',
      body: json['body'] as String? ?? '',
      serviceRequestId: json['serviceRequestId'] as String?,
      isRead: json['isRead'] as bool? ?? false,
      createdAt: DateTime.tryParse(json['createdAt'] as String? ?? '') ?? DateTime.now(),
    );
  }

  final String id;
  final String title;
  final String body;
  final String? serviceRequestId;
  final bool isRead;
  final DateTime createdAt;
}
