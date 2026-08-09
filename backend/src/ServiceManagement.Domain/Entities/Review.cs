using ServiceManagement.Domain.Common;

namespace ServiceManagement.Domain.Entities;

public class Review : BaseEntity
{
    public Guid ServiceRequestId { get; set; }
    public ServiceRequest ServiceRequest { get; set; } = null!;

    public Guid CustomerId { get; set; }
    public ApplicationUser Customer { get; set; } = null!;

    public Guid ProviderId { get; set; }
    public ApplicationUser Provider { get; set; } = null!;

    public int Rating { get; set; }
    public string? Comment { get; set; }
}
