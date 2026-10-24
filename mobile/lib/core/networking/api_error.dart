import 'package:dio/dio.dart';

String describeApiError(Object error, {required String fallback}) {
  if (error is! DioException) {
    return fallback;
  }

  final data = error.response?.data;
  if (data is Map) {
    final message = data['message'];
    if (message is String && message.trim().isNotEmpty) {
      return message;
    }
    final errors = data['errors'];
    if (errors is List && errors.isNotEmpty) {
      return errors.map((e) => e.toString()).join(' ');
    }
  }

  final status = error.response?.statusCode;
  if (status == 404) {
    return 'The request API is not running. Restart the backend, then try again.';
  }
  if (status == 403) {
    return 'Only customers can place requests. Sign in as customer@test.local.';
  }
  if (status == 401) {
    return 'Your session expired. Sign out and sign in again.';
  }
  if (error.type == DioExceptionType.connectionError ||
      error.type == DioExceptionType.connectionTimeout) {
    return 'Cannot reach the API. Confirm it is running on port 5080.';
  }

  return fallback;
}
