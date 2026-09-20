using ServiceManagement.Domain.Common;

namespace ServiceManagement.Domain.Entities;

/// <summary>
/// A document type that providers must (or may) upload when they apply to serve
/// any service in this category. Configured per category so a cook can be asked
/// for a food-safety certificate while a driver is asked for a licence — without
/// category-specific code.
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
}
