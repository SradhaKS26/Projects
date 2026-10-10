using ServiceManagement.Domain.Common;

namespace ServiceManagement.Domain.Entities;

public class ServiceCategory : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<Service> Services { get; set; } = [];
    public ICollection<CategoryDocumentRequirement> DocumentRequirements { get; set; } = [];
}
