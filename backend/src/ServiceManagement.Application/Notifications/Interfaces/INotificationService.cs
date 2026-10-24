using ServiceManagement.Application.Notifications.Dtos;

namespace ServiceManagement.Application.Notifications.Interfaces;

public interface INotificationService
{
    Task<IReadOnlyList<UserNotificationDto>> ListMineAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<UserNotificationDto> MarkReadAsync(
        Guid userId,
        Guid notificationId,
        CancellationToken cancellationToken = default);

    Task<int> MarkAllReadAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}
