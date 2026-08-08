namespace ServiceManagement.Application.Users.Dtos;

public record UpdateUserRequest(
    string FirstName,
    string LastName,
    string? PhoneNumber,
    string? ProfileImageUrl,
    bool? IsActive);

public record UserListItemDto(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string? PhoneNumber,
    bool IsActive,
    IReadOnlyList<string> Roles,
    DateTime CreatedAt);
