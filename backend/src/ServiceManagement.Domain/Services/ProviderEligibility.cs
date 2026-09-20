using ServiceManagement.Domain.Entities;
using ServiceManagement.Domain.Enums;

namespace ServiceManagement.Domain.Services;

/// <summary>
/// The eligibility rule later request-broadcast depends on.
/// A provider receives requests for a service only when every condition holds.
/// </summary>
public static class ProviderEligibility
{
    public static bool CanReceiveRequests(
        ProviderApprovalStatus approvalStatus,
        bool isActive,
        AvailabilityStatus availabilityStatus,
        bool isRegisteredForService)
    {
        return approvalStatus == ProviderApprovalStatus.Approved
            && isActive
            && availabilityStatus == AvailabilityStatus.Available
            && isRegisteredForService;
    }

    public static bool CanReceiveRequests(ServiceProviderProfile profile, Guid serviceId)
    {
        var registered = profile.ProviderServices.Any(ps =>
            ps.ServiceId == serviceId && ps.Status == ProviderServiceStatus.Approved);

        return CanReceiveRequests(
            profile.ApprovalStatus,
            profile.IsActive,
            profile.AvailabilityStatus,
            registered);
    }

    public static IQueryable<ServiceProviderProfile> EligibleForService(
        this IQueryable<ServiceProviderProfile> profiles,
        Guid serviceId)
    {
        return profiles.Where(p =>
            p.ApprovalStatus == ProviderApprovalStatus.Approved
            && p.IsActive
            && p.AvailabilityStatus == AvailabilityStatus.Available
            && p.ProviderServices.Any(ps =>
                ps.ServiceId == serviceId && ps.Status == ProviderServiceStatus.Approved));
    }
}
