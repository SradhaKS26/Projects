using ServiceManagement.Domain.Common;
using ServiceManagement.Domain.Enums;

namespace ServiceManagement.Domain.Entities;

public class ServiceProviderProfile : BaseEntity
{
    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;

    public string? Description { get; set; }
    public AvailabilityStatus AvailabilityStatus { get; set; } = AvailabilityStatus.Unavailable;
    public decimal Rating { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<ProviderService> ProviderServices { get; set; } = [];
}
