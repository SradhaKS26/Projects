using Microsoft.AspNetCore.Identity;

namespace ServiceManagement.Domain.Entities;

public class ApplicationUser : IdentityUser<Guid>
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? ProfileImageUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ServiceProviderProfile? ProviderProfile { get; set; }
    public ICollection<CustomerAddress> Addresses { get; set; } = [];
    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
}
