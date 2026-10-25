namespace ServiceManagement.Domain.Entities;

/// <summary>
/// Limits a document requirement to specific catalog services. When a requirement
/// has no scopes, it applies to every service in its category (taxi licences, walk
/// delivery ID). Vehicle-only delivery licences get one row per vehicle service.
/// </summary>
public class DocumentRequirementScope
{
    public Guid DocumentRequirementId { get; set; }
    public CategoryDocumentRequirement DocumentRequirement { get; set; } = null!;

    public Guid ServiceId { get; set; }
    public Service Service { get; set; } = null!;
}
