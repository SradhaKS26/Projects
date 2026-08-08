using Microsoft.EntityFrameworkCore;
using ServiceManagement.Application.Common.Exceptions;
using ServiceManagement.Application.Roles.Dtos;
using ServiceManagement.Application.Roles.Interfaces;
using ServiceManagement.Domain.Entities;
using ServiceManagement.Infrastructure.Persistence;

namespace ServiceManagement.Infrastructure.Services;

public class RoleService : IRoleService
{
    private readonly AppDbContext _db;

    public RoleService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<RoleDto>> GetRolesAsync(CancellationToken cancellationToken = default)
    {
        var roles = await _db.Roles
            .Include(r => r.RolePermissions)
            .ThenInclude(rp => rp.Permission)
            .OrderBy(r => r.Name)
            .ToListAsync(cancellationToken);

        return roles.Select(r => new RoleDto(
            r.Id,
            r.Name ?? string.Empty,
            r.Description,
            r.RolePermissions.Select(rp => rp.Permission.Name).OrderBy(n => n).ToList())).ToList();
    }

    public async Task<IReadOnlyList<PermissionDto>> GetPermissionsAsync(CancellationToken cancellationToken = default)
    {
        return await _db.Permissions
            .OrderBy(p => p.Name)
            .Select(p => new PermissionDto(p.Id, p.Name, p.Description))
            .ToListAsync(cancellationToken);
    }

    public async Task<RoleDto> UpdateRolePermissionsAsync(
        Guid roleId,
        UpdateRolePermissionsRequest request,
        CancellationToken cancellationToken = default)
    {
        var role = await _db.Roles
            .Include(r => r.RolePermissions)
            .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(r => r.Id == roleId, cancellationToken)
            ?? throw new NotFoundException("Role not found.");

        var requested = request.PermissionNames.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var permissions = await _db.Permissions
            .Where(p => requested.Contains(p.Name))
            .ToListAsync(cancellationToken);

        if (permissions.Count != requested.Count)
        {
            var found = permissions.Select(p => p.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var missing = requested.Where(n => !found.Contains(n));
            throw new AppException($"Unknown permissions: {string.Join(", ", missing)}");
        }

        _db.RolePermissions.RemoveRange(role.RolePermissions);
        foreach (var permission in permissions)
        {
            _db.RolePermissions.Add(new RolePermission
            {
                RoleId = role.Id,
                PermissionId = permission.Id
            });
        }

        await _db.SaveChangesAsync(cancellationToken);

        return new RoleDto(
            role.Id,
            role.Name ?? string.Empty,
            role.Description,
            permissions.Select(p => p.Name).OrderBy(n => n).ToList());
    }
}
