using ServiceManagement.Domain.Enums;

namespace ServiceManagement.Application.Providers.Dtos;

public record ProviderListQuery(
    ProviderApprovalStatus? ApprovalStatus = null,
    string? Search = null);

public record ProviderListItemDto(
    Guid Id,
    Guid UserId,
    string FirstName,
    string LastName,
    string Email,
    string? PhoneNumber,
    ProviderApprovalStatus ApprovalStatus,
    bool IsActive,
    AvailabilityStatus AvailabilityStatus,
    IReadOnlyList<string> AppliedServices,
    int DocumentCount,
    bool HasMissingRequiredDocuments,
    DateTime CreatedAt,
    DateTime? ReviewedAt);

public record ProviderServiceItemDto(
    Guid ServiceId,
    string ServiceName,
    Guid CategoryId,
    string CategoryName,
    decimal BasePrice,
    ProviderServiceStatus Status,
    DateTime AppliedAt,
    DateTime? ReviewedAt);

public record ProviderDocumentDto(
    Guid Id,
    Guid DocumentRequirementId,
    string RequirementName,
    Guid CategoryId,
    string CategoryName,
    string OriginalFileName,
    string ContentType,
    long FileSize,
    DateTime UploadedAt);

public record MissingDocumentRequirementDto(
    Guid Id,
    Guid CategoryId,
    string CategoryName,
    string Name,
    string? Description);

public record ProviderProfileDto(
    Guid Id,
    Guid UserId,
    string FirstName,
    string LastName,
    string Email,
    string? PhoneNumber,
    string? Description,
    ProviderApprovalStatus ApprovalStatus,
    bool IsActive,
    AvailabilityStatus AvailabilityStatus,
    decimal Rating,
    string? ReviewReason,
    DateTime? ReviewedAt,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    IReadOnlyList<ProviderServiceItemDto> Services,
    IReadOnlyList<ProviderDocumentDto> Documents,
    IReadOnlyList<MissingDocumentRequirementDto> MissingRequiredDocuments);

public record UpdateProviderApplicationRequest(
    string? Description,
    IReadOnlyList<Guid> ServiceIds);

public record SetAvailabilityRequest(AvailabilityStatus AvailabilityStatus);

public record ReviewProviderRequest(string? Reason);

public record RejectProviderRequest(string Reason);

public record SuspendProviderRequest(string Reason);

public record SetProviderActiveRequest(bool IsActive);
