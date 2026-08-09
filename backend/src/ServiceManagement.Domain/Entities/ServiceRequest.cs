using ServiceManagement.Domain.Common;
using ServiceManagement.Domain.Enums;

namespace ServiceManagement.Domain.Entities;

public class ServiceRequest : BaseEntity
{
    public Guid CustomerId { get; set; }
    public ApplicationUser Customer { get; set; } = null!;

    public Guid ServiceId { get; set; }
    public Service Service { get; set; } = null!;

    public Guid? AssignedProviderId { get; set; }
    public ApplicationUser? AssignedProvider { get; set; }

    public ServiceRequestStatus Status { get; set; } = ServiceRequestStatus.Pending;
    public DateTime RequestedDate { get; set; } = DateTime.UtcNow;
    public DateTime? ScheduledDate { get; set; }
    public Guid? AddressId { get; set; }
    public CustomerAddress? Address { get; set; }
    public string? Notes { get; set; }
    public decimal Price { get; set; }
    public DateTime? CompletedAt { get; set; }

    public ICollection<ServiceRequestStatusHistory> StatusHistory { get; set; } = [];
    public ICollection<ServiceRequestAttachment> Attachments { get; set; } = [];
    public Review? Review { get; set; }
}
