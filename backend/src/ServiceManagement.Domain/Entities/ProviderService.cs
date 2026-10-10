using ServiceManagement.Domain.Enums;

namespace ServiceManagement.Domain.Entities;

/// <summary>
/// A provider's application to fulfill a specific catalog service.
/// Pricing stays on <see cref="Service.BasePrice"/> — never on the provider.
/// </summary>
public class ProviderService
{
    public Guid ProviderProfileId { get; set; }
    public ServiceProviderProfile ProviderProfile { get; set; } = null!;

    public Guid ServiceId { get; set; }
    public Service Service { get; set; } = null!;

    public ProviderServiceStatus Status { get; set; } = ProviderServiceStatus.Pending;
    public DateTime AppliedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedAt { get; set; }
}
