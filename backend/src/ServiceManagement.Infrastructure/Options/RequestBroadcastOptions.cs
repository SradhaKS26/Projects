namespace ServiceManagement.Infrastructure.Options;

public class RequestBroadcastOptions
{
    public const string SectionName = "RequestBroadcast";

    public int ExpiryMinutes { get; set; } = 10;
}
