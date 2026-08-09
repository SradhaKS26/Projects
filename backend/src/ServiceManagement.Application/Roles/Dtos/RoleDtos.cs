namespace ServiceManagement.Application.Roles.Dtos;

public record RoleDto(Guid Id, string Name, string? Description, IReadOnlyList<string> Permissions);

public record PermissionDto(Guid Id, string Name, string? Description);

public record UpdateRolePermissionsRequest(IReadOnlyList<string> PermissionNames);
