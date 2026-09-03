using System.Text.Json.Serialization;

namespace ArkKeeper.Updater;

[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(UpdateManifest))]
internal sealed partial class UpdateManifestJsonContext : JsonSerializerContext
{
}
