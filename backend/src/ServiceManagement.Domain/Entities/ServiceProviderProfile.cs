using ServiceManagement.Domain.Common;
using ServiceManagement.Domain.Enums;

namespace ServiceManagement.Domain.Entities;

public class ServiceProviderProfile : BaseEntity
{
    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;

    public string? Description { get; set; }

    /// <summary>
    /// Provider-controlled: whether they are willing to receive work right now.
    /// </summary>
    public AvailabilityStatus AvailabilityStatus { get; set; } = AvailabilityStatus.Unavailable;

    public decimal Rating { get; set; }

    /// <summary>
    /// Operational kill switch. An approved provider can be deactivated without
    /// changing <see cref="ApprovalStatus"/>.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Onboarding review state. New profiles start <see cref="ProviderApprovalStatus.Pending"/>.
    /// </summary>
    public ProviderApprovalStatus ApprovalStatus { get; set; } = ProviderApprovalStatus.Pending;

    public string? ReviewReason { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public Guid? ReviewedByUserId { get; set; }
    public ApplicationUser? ReviewedByUser { get; set; }

    public ICollection<ProviderService> ProviderServices { get; set; } = [];
    public ICollection<ProviderDocument> Documents { get; set; } = [];
}
