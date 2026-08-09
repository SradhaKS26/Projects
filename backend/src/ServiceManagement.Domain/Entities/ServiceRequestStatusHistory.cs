using ServiceManagement.Domain.Common;
using ServiceManagement.Domain.Enums;

namespace ServiceManagement.Domain.Entities;

public class ServiceRequestStatusHistory : BaseEntity
{
    public Guid ServiceRequestId { get; set; }
    public ServiceRequest ServiceRequest { get; set; } = null!;

    public ServiceRequestStatus? OldStatus { get; set; }
    public ServiceRequestStatus NewStatus { get; set; }
    public Guid ChangedByUserId { get; set; }
    public ApplicationUser ChangedByUser { get; set; } = null!;
    public string? Notes { get; set; }
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
}
