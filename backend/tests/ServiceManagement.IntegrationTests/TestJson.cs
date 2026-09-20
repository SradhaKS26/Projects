using System.Text.Json;
using System.Text.Json.Serialization;

namespace ServiceManagement.IntegrationTests;

internal static class TestJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: true) }
    };
}
