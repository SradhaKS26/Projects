namespace ServiceManagement.Application.Catalog.Dtos;

public record ServiceCategoryDto(
    Guid Id,
    string Name,
    string? Description,
    string? ImageUrl,
    bool IsActive,
    int ServiceCount,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public record CreateServiceCategoryRequest(
    string Name,
    string? Description,
    string? ImageUrl,
    bool IsActive = true);

public record UpdateServiceCategoryRequest(
    string Name,
    string? Description,
    string? ImageUrl,
    bool IsActive);

public record ServiceDto(
    Guid Id,
    Guid CategoryId,
    string CategoryName,
    string Name,
    string? Description,
    decimal BasePrice,
    bool IsActive,
    int? EstimatedDurationMinutes,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public record CreateServiceRequest(
    Guid CategoryId,
    string Name,
    string? Description,
    decimal BasePrice,
    int? EstimatedDurationMinutes,
    bool IsActive = true);

public record UpdateServiceRequest(
    Guid CategoryId,
    string Name,
    string? Description,
    decimal BasePrice,
    int? EstimatedDurationMinutes,
    bool IsActive);

/// <summary>
/// Filters for catalog queries. Customers only ever see active records;
/// administrators can opt into inactive ones.
/// </summary>
public record CatalogQuery(
    Guid? CategoryId = null,
    string? Search = null,
    bool IncludeInactive = false);
