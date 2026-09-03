using System.Text.Json.Serialization;

namespace ArkKeeper.Networking.Workshop;

[JsonSerializable(typeof(ResponseEnvelope))]
internal sealed partial class SteamWorkshopResponseJsonContext : JsonSerializerContext
{
}
