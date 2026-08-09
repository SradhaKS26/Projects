using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceManagement.Application.Common.Models;
using ServiceManagement.Application.Roles.Dtos;
using ServiceManagement.Application.Roles.Interfaces;
using ServiceManagement.Domain.Constants;
using ServiceManagement.Infrastructure.Auth;

namespace ServiceManagement.Api.Controllers;

[ApiController]
[Route("api/roles")]
[Authorize]
public class RolesController : ControllerBase
{
    private readonly IRoleService _roleService;

    public RolesController(IRoleService roleService)
    {
        _roleService = roleService;
    }

    [HttpGet]
    [Authorize(Policy = PermissionPolicies.Prefix + Permissions.ManageRoles)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<RoleDto>>>> GetRoles(CancellationToken cancellationToken)
    {
        var roles = await _roleService.GetRolesAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<RoleDto>>.Ok(roles));
    }

    [HttpGet("/api/permissions")]
    [Authorize(Policy = PermissionPolicies.Prefix + Permissions.ManagePermissions)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PermissionDto>>>> GetPermissions(CancellationToken cancellationToken)
    {
        var permissions = await _roleService.GetPermissionsAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<PermissionDto>>.Ok(permissions));
    }

    [HttpPut("{id:guid}/permissions")]
    [Authorize(Policy = PermissionPolicies.Prefix + Permissions.ManagePermissions)]
    public async Task<ActionResult<ApiResponse<RoleDto>>> UpdatePermissions(
        Guid id,
        [FromBody] UpdateRolePermissionsRequest request,
        CancellationToken cancellationToken)
    {
        var role = await _roleService.UpdateRolePermissionsAsync(id, request, cancellationToken);
        return Ok(ApiResponse<RoleDto>.Ok(role, "Role permissions updated."));
    }
}
