using ServiceManagement.Domain.Enums;

namespace ServiceManagement.Application.Reviews.Dtos;

public record SubmitReviewRequest(int Rating, string? Comment);

public record ReviewDto(
    Guid Id,
    Guid ServiceRequestId,
    Guid ProviderId,
    int Rating,
    string? Comment,
    DateTime CreatedAt,
    string? ServiceName = null,
    string? CategoryName = null);

public record RequestStatusEventDto(
    ServiceRequestStatus? OldStatus,
    ServiceRequestStatus NewStatus,
    DateTime ChangedAt,
    string? Notes);
