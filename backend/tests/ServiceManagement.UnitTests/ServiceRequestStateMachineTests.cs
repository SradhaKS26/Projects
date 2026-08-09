using FluentAssertions;
using ServiceManagement.Domain.Enums;
using ServiceManagement.Domain.Services;

namespace ServiceManagement.UnitTests;

public class ServiceRequestStateMachineTests
{
    [Theory]
    [InlineData(ServiceRequestStatus.Pending, ServiceRequestStatus.Accepted, true)]
    [InlineData(ServiceRequestStatus.Pending, ServiceRequestStatus.Completed, false)]
    [InlineData(ServiceRequestStatus.Assigned, ServiceRequestStatus.InProgress, true)]
    [InlineData(ServiceRequestStatus.InProgress, ServiceRequestStatus.Completed, true)]
    [InlineData(ServiceRequestStatus.Completed, ServiceRequestStatus.InProgress, false)]
    [InlineData(ServiceRequestStatus.Cancelled, ServiceRequestStatus.Pending, false)]
    public void CanTransition_enforces_lifecycle_rules(
        ServiceRequestStatus from,
        ServiceRequestStatus to,
        bool expected)
    {
        ServiceRequestStateMachine.CanTransition(from, to).Should().Be(expected);
    }
}
