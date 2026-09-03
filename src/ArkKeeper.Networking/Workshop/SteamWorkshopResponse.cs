using System.Text.Json.Serialization;

namespace ArkKeeper.Networking.Workshop;

internal sealed record ResponseEnvelope(
    [property: JsonPropertyName("response")] ResponseBody Response);

internal sealed record ResponseBody(
    [property: JsonPropertyName("publishedfiledetails")] List<RawFileDetails> PublishedFileDetails);

internal sealed record RawFileDetails(
    [property: JsonPropertyName("publishedfileid")] string PublishedFileId,
    [property: JsonPropertyName("result")] int Result,
    [property: JsonPropertyName("title")] string? Title,
    [property: JsonPropertyName("file_size")] string? FileSize,
    [property: JsonPropertyName("time_updated")] long? TimeUpdated,
    [property: JsonPropertyName("preview_url")] string? PreviewUrl,
    [property: JsonPropertyName("banned")] int Banned);
