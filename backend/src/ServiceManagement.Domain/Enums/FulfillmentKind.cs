using System.Text.Json;
using System.Text.Json.Serialization;

namespace ServiceManagement.Domain.Enums;

/// <summary>
/// How a catalog category is fulfilled. New kinds can be added later without
/// splitting the platform into per-service modules.
/// </summary>
[JsonConverter(typeof(FulfillmentKindJsonConverter))]
public enum FulfillmentKind
{
    /// <summary>One service location (cleaning, cooking, repairs).</summary>
    OnSite = 0,

    /// <summary>Passenger trip: pickup → drop-off. Vehicle type is the catalog service.</summary>
    Ride = 1,

    /// <summary>Package trip: pickup → drop-off with a recipient and parcel description.</summary>
    Parcel = 2
}

public sealed class FulfillmentKindJsonConverter : JsonConverter<FulfillmentKind>
{
    public override FulfillmentKind Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out var number))
        {
            return (FulfillmentKind)number;
        }

        var raw = reader.GetString();
        return raw?.Replace("_", string.Empty).ToLowerInvariant() switch
        {
            "ride" => FulfillmentKind.Ride,
            "parcel" => FulfillmentKind.Parcel,
            "onsite" or "standard" => FulfillmentKind.OnSite,
            _ => throw new JsonException($"Unknown fulfillment kind '{raw}'.")
        };
    }

    public override void Write(Utf8JsonWriter writer, FulfillmentKind value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value switch
        {
            FulfillmentKind.Ride => "ride",
            FulfillmentKind.Parcel => "parcel",
            _ => "onSite"
        });
    }
}
