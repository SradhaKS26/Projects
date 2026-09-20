namespace ServiceManagement.IntegrationTests;

/// <summary>
/// The API host configures a process-global Serilog logger, so integration test classes
/// must share a single xUnit collection rather than spinning up hosts in parallel.
/// </summary>
[CollectionDefinition(Name)]
public class ApiCollection : ICollectionFixture<CustomWebApplicationFactory>
{
    public const string Name = "Api";
}
