using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceManagement.Api.Extensions;
using ServiceManagement.Application.Common.Models;
using ServiceManagement.Application.Notifications.Dtos;
using ServiceManagement.Application.Notifications.Interfaces;

namespace ServiceManagement.Api.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notifications;

    public NotificationsController(INotificationService notifications)
    {
        _notifications = notifications;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<UserNotificationDto>>>> Mine(
        CancellationToken cancellationToken)
    {
        var items = await _notifications.ListMineAsync(User.GetUserId(), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<UserNotificationDto>>.Ok(items));
    }

    [HttpPost("read-all")]
    public async Task<ActionResult<ApiResponse<int>>> ReadAll(CancellationToken cancellationToken)
    {
        var count = await _notifications.MarkAllReadAsync(User.GetUserId(), cancellationToken);
        return Ok(ApiResponse<int>.Ok(count, "Notifications marked read."));
    }

    [HttpPost("{id:guid}/read")]
    public async Task<ActionResult<ApiResponse<UserNotificationDto>>> Read(
        Guid id,
        CancellationToken cancellationToken)
    {
        var item = await _notifications.MarkReadAsync(User.GetUserId(), id, cancellationToken);
        return Ok(ApiResponse<UserNotificationDto>.Ok(item));
    }
}
