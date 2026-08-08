using ServiceManagement.Application.Auth.Dtos;
using ServiceManagement.Application.Users.Dtos;

namespace ServiceManagement.Application.Users.Interfaces;

public interface IUserService
{
    Task<IReadOnlyList<UserListItemDto>> GetUsersAsync(CancellationToken cancellationToken = default);
    Task<UserDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<UserDto> UpdateAsync(Guid id, UpdateUserRequest request, CancellationToken cancellationToken = default);
}
