using ServiceManagement.Domain.Common;

namespace ServiceManagement.Domain.Entities;

/// <summary>
/// Metadata for a file a provider uploaded against a category document
/// requirement. The bytes live in <c>IFileStorage</c>, not in PostgreSQL.
/// </summary>
public class ProviderDocument : BaseEntity
{
    public Guid ProviderProfileId { get; set; }
    public ServiceProviderProfile ProviderProfile { get; set; } = null!;

    public Guid DocumentRequirementId { get; set; }
    public CategoryDocumentRequirement DocumentRequirement { get; set; } = null!;

    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }

    /// <summary>
    /// Storage-relative key understood by <c>IFileStorage</c>. Swap the storage
    /// implementation without changing this column.
    /// </summary>
    public string StorageKey { get; set; } = string.Empty;
}
