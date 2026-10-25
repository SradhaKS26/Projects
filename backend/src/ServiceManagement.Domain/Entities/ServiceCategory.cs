using ServiceManagement.Domain.Common;
using ServiceManagement.Domain.Enums;

namespace ServiceManagement.Domain.Entities;

public class ServiceCategory : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public FulfillmentKind FulfillmentKind { get; set; } = FulfillmentKind.OnSite;

    public ICollection<Service> Services { get; set; } = [];
    public ICollection<CategoryDocumentRequirement> DocumentRequirements { get; set; } = [];
}
