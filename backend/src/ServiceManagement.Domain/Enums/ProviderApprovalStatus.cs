namespace ServiceManagement.Domain.Enums;

/// <summary>
/// Onboarding review state for a provider profile. Independent of
/// <see cref="ServiceManagement.Domain.Entities.ServiceProviderProfile.IsActive"/>
/// (operational kill switch) and <see cref="AvailabilityStatus"/> (the provider's
/// current willingness to receive work).
/// </summary>
public enum ProviderApprovalStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2,
    Suspended = 3
}
