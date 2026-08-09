namespace ServiceManagement.Domain.Entities;

public class ProviderService
{
    public Guid ProviderProfileId { get; set; }
    public ServiceProviderProfile ProviderProfile { get; set; } = null!;

    public Guid ServiceId { get; set; }
    public Service Service { get; set; } = null!;
}
