using ServiceManagement.Application.Reviews.Dtos;
using ServiceManagement.Domain.Enums;

namespace ServiceManagement.Application.Requests.Dtos;

public record PlaceAddressRequest(
    string AddressLine1,
    string? AddressLine2,
    string City,
    string State,
    string PostalCode,
    double? Latitude = null,
    double? Longitude = null);

public record PlaceServiceRequest(
    Guid ServiceId,
    string AddressLine1,
    string? AddressLine2,
    string City,
    string State,
    string PostalCode,
    double? Latitude,
    double? Longitude,
    string? Notes,
    PlaceAddressRequest? Destination = null,
    int? PassengerCount = null,
    string? ParcelDescription = null,
    string? RecipientName = null,
    string? RecipientPhone = null);

public record ServiceRequestAddressDto(
    Guid Id,
    string AddressLine1,
    string? AddressLine2,
    string City,
    string State,
    string PostalCode,
    double? Latitude,
    double? Longitude);

public record RequestContactDto(
    Guid UserId,
    string FirstName,
    string LastName,
    string? Email,
    string? PhoneNumber);

public record ServiceRequestDto(
    Guid Id,
    Guid ServiceId,
    string ServiceName,
    string CategoryName,
    decimal Price,
    ServiceRequestStatus Status,
    DateTime RequestedDate,
    DateTime ExpiresAt,
    DateTime? CompletedAt,
    string? Notes,
    ServiceRequestAddressDto? Address,
    RequestContactDto? Customer,
    RequestContactDto? Provider,
    decimal? ProviderRating = null,
    int ProviderReviewCount = 0,
    ReviewDto? Review = null,
    IReadOnlyList<RequestStatusEventDto>? Timeline = null,
    FulfillmentKind FulfillmentKind = FulfillmentKind.OnSite,
    TripDto? Trip = null,
    ServiceRequestAddressDto? Destination = null,
    int? PassengerCount = null,
    string? ParcelDescription = null,
    string? RecipientName = null,
    string? RecipientPhone = null);

public record TripDto(
    FulfillmentKind Kind,
    ServiceRequestAddressDto? Pickup,
    ServiceRequestAddressDto? Dropoff,
    int? PassengerCount,
    string? ParcelDescription,
    string? RecipientName,
    string? RecipientPhone);
