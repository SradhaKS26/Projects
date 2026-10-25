using FluentAssertions;
using ServiceManagement.Domain.Enums;
using ServiceManagement.Domain.Services;

namespace ServiceManagement.UnitTests;

public class FulfillmentRulesTests
{
    [Theory]
    [InlineData(FulfillmentKind.OnSite, false)]
    [InlineData(FulfillmentKind.Ride, true)]
    [InlineData(FulfillmentKind.Parcel, true)]
    public void RequiresDropoff_for_taxi_and_delivery(FulfillmentKind kind, bool expected)
    {
        FulfillmentRules.RequiresDropoff(kind).Should().Be(expected);
    }

    [Fact]
    public void Ride_needs_passenger_count_parcel_needs_recipient()
    {
        FulfillmentRules.RequiresPassengerCount(FulfillmentKind.Ride).Should().BeTrue();
        FulfillmentRules.RequiresPassengerCount(FulfillmentKind.Parcel).Should().BeFalse();
        FulfillmentRules.RequiresParcelDetails(FulfillmentKind.Parcel).Should().BeTrue();
        FulfillmentRules.RequiresParcelDetails(FulfillmentKind.Ride).Should().BeFalse();
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(6, true)]
    [InlineData(0, false)]
    [InlineData(7, false)]
    [InlineData(null, false)]
    public void Passenger_count_is_one_through_six(int? count, bool expected)
    {
        FulfillmentRules.IsValidPassengerCount(count).Should().Be(expected);
    }
}
