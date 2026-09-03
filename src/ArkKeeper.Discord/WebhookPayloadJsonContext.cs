using System.Text.Json.Serialization;

namespace ArkKeeper.Discord;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(WebhookPayload))]
internal sealed partial class WebhookPayloadJsonContext : JsonSerializerContext
{
}
