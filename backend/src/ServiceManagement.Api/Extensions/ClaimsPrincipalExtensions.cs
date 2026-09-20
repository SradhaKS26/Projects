using System.Security.Claims;
using ServiceManagement.Application.Common.Exceptions;

namespace ServiceManagement.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        var value = user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? user.FindFirstValue("sub")
            ?? throw new UnauthorizedAppException();

        return Guid.Parse(value);
    }

    public static bool HasPermission(this ClaimsPrincipal user, string permission) =>
        user.Claims.Any(c =>
            c.Type == "permission" &&
            string.Equals(c.Value, permission, StringComparison.OrdinalIgnoreCase));
}
