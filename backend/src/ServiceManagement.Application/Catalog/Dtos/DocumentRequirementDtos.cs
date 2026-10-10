namespace ServiceManagement.Application.Catalog.Dtos;

public record CategoryDocumentRequirementDto(
    Guid Id,
    Guid CategoryId,
    string CategoryName,
    string Name,
    string? Description,
    bool IsRequired,
    int SortOrder,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public record CreateDocumentRequirementRequest(
    string Name,
    string? Description,
    bool IsRequired = true,
    int SortOrder = 0);

public record UpdateDocumentRequirementRequest(
    string Name,
    string? Description,
    bool IsRequired,
    int SortOrder);
