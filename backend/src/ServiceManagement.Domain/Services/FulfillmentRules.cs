using ServiceManagement.Domain.Enums;

namespace ServiceManagement.Domain.Services;

public static class FulfillmentRules
{
    public const int MinPassengers = 1;
    public const int MaxPassengers = 6;

    public static bool RequiresDropoff(FulfillmentKind kind) =>
        kind is FulfillmentKind.Ride or FulfillmentKind.Parcel;

    public static bool RequiresPassengerCount(FulfillmentKind kind) =>
        kind == FulfillmentKind.Ride;

    public static bool RequiresParcelDetails(FulfillmentKind kind) =>
        kind == FulfillmentKind.Parcel;

    public static bool IsValidPassengerCount(int? count) =>
        count is >= MinPassengers and <= MaxPassengers;
}
