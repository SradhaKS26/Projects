namespace ServiceManagement.Application.Notifications.Dtos;

public record UserNotificationDto(
    Guid Id,
    string Title,
    string Body,
    Guid? ServiceRequestId,
    bool IsRead,
    DateTime CreatedAt);
