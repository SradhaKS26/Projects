using FluentAssertions;
using ServiceManagement.Domain.Entities;
using ServiceManagement.Domain.Enums;
using ServiceManagement.Domain.Services;

namespace ServiceManagement.UnitTests;

public class ProviderEligibilityTests
{
    [Theory]
    [InlineData(ProviderApprovalStatus.Approved, true, AvailabilityStatus.Available, true, true)]
    [InlineData(ProviderApprovalStatus.Pending, true, AvailabilityStatus.Available, true, false)]
    [InlineData(ProviderApprovalStatus.Rejected, true, AvailabilityStatus.Available, true, false)]
    [InlineData(ProviderApprovalStatus.Suspended, true, AvailabilityStatus.Available, true, false)]
    [InlineData(ProviderApprovalStatus.Approved, false, AvailabilityStatus.Available, true, false)]
    [InlineData(ProviderApprovalStatus.Approved, true, AvailabilityStatus.Unavailable, true, false)]
    [InlineData(ProviderApprovalStatus.Approved, true, AvailabilityStatus.Busy, true, false)]
    [InlineData(ProviderApprovalStatus.Approved, true, AvailabilityStatus.Available, false, false)]
    public void CanReceiveRequests_requires_approved_active_available_and_registered(
        ProviderApprovalStatus approval,
        bool isActive,
        AvailabilityStatus availability,
        bool registered,
        bool expected)
    {
        ProviderEligibility.CanReceiveRequests(approval, isActive, availability, registered)
            .Should()
            .Be(expected);
    }

    [Fact]
    public void CanReceiveRequests_uses_approved_service_registration_only()
    {
        var serviceId = Guid.NewGuid();
        var otherServiceId = Guid.NewGuid();
        var profile = new ServiceProviderProfile
        {
            ApprovalStatus = ProviderApprovalStatus.Approved,
            IsActive = true,
            AvailabilityStatus = AvailabilityStatus.Available,
            ProviderServices =
            [
                new ProviderService
                {
                    ServiceId = otherServiceId,
                    Status = ProviderServiceStatus.Approved
                },
                new ProviderService
                {
                    ServiceId = serviceId,
                    Status = ProviderServiceStatus.Pending
                }
            ]
        };

        ProviderEligibility.CanReceiveRequests(profile, serviceId).Should().BeFalse();

        profile.ProviderServices.First(ps => ps.ServiceId == serviceId).Status = ProviderServiceStatus.Approved;
        ProviderEligibility.CanReceiveRequests(profile, serviceId).Should().BeTrue();
    }
}
