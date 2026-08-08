using ServiceManagement.Domain.Common;

namespace ServiceManagement.Domain.Entities;

public class Service : BaseEntity
{
    public Guid CategoryId { get; set; }
    public ServiceCategory Category { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal BasePrice { get; set; }
    public bool IsActive { get; set; } = true;
    public int? EstimatedDurationMinutes { get; set; }

    public ICollection<ProviderService> ProviderServices { get; set; } = [];
    public ICollection<ServiceRequest> ServiceRequests { get; set; } = [];
}
