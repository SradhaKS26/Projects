using ServiceManagement.Domain.Common;

namespace ServiceManagement.Domain.Entities;

/// <summary>
/// A document type that providers must (or may) upload when they apply. With no
/// <see cref="ServiceScopes"/> it applies to every service in the category. With
/// scopes it applies only to those services (vehicle delivery needs a licence;
/// walk delivery does not).
/// </summary>
public class CategoryDocumentRequirement : BaseEntity
{
    public Guid CategoryId { get; set; }
    public ServiceCategory Category { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsRequired { get; set; } = true;
    public int SortOrder { get; set; }

    public ICollection<ProviderDocument> Documents { get; set; } = [];
    public ICollection<DocumentRequirementScope> ServiceScopes { get; set; } = [];
}
