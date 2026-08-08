using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceManagement.Application.Auth.Dtos;
using ServiceManagement.Application.Common.Exceptions;
using ServiceManagement.Application.Common.Models;
using ServiceManagement.Application.Users.Dtos;
using ServiceManagement.Application.Users.Interfaces;
using ServiceManagement.Domain.Constants;
using ServiceManagement.Infrastructure.Auth;

namespace ServiceManagement.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet("me")]
    public async Task<ActionResult<ApiResponse<UserDto>>> Me(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var user = await _userService.GetByIdAsync(userId, cancellationToken);
        return Ok(ApiResponse<UserDto>.Ok(user));
    }

    [HttpGet]
    [Authorize(Policy = PermissionPolicies.Prefix + Permissions.ManageUsers)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<UserListItemDto>>>> GetUsers(CancellationToken cancellationToken)
    {
        var users = await _userService.GetUsersAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<UserListItemDto>>.Ok(users));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        EnsureCanAccessUser(id);
        var user = await _userService.GetByIdAsync(id, cancellationToken);
        return Ok(ApiResponse<UserDto>.Ok(user));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<UserDto>>> Update(
        Guid id,
        [FromBody] UpdateUserRequest request,
        CancellationToken cancellationToken)
    {
        var canManageUsers = HasPermission(Permissions.ManageUsers);
        if (!canManageUsers)
        {
            if (GetCurrentUserId() != id)
            {
                throw new ForbiddenException();
            }

            // Non-admins cannot deactivate accounts via profile update.
            request = request with { IsActive = null };
        }

        var user = await _userService.UpdateAsync(id, request, cancellationToken);
        return Ok(ApiResponse<UserDto>.Ok(user, "User updated."));
    }

    private Guid GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAppException();
        return Guid.Parse(value);
    }

    private void EnsureCanAccessUser(Guid id)
    {
        if (HasPermission(Permissions.ManageUsers) || GetCurrentUserId() == id)
        {
            return;
        }

        throw new ForbiddenException();
    }

    private bool HasPermission(string permission) =>
        User.Claims.Any(c =>
            c.Type == "permission" &&
            string.Equals(c.Value, permission, StringComparison.OrdinalIgnoreCase));
}
