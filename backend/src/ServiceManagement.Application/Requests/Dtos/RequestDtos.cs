using ServiceManagement.Domain.Enums;

namespace ServiceManagement.Application.Requests.Dtos;

public record PlaceServiceRequest(
    Guid ServiceId,
    string AddressLine1,
    string? AddressLine2,
    string City,
    string State,
    string PostalCode,
    double? Latitude,
    double? Longitude,
    string? Notes);

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
    RequestContactDto? Provider);
