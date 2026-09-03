using System.Text.Json;
using System.Text.Json.Serialization;

namespace ArkKeeper.Networking.Workshop;

/// <summary>Looks up Steam Workshop mod metadata via the public
/// ISteamRemoteStorage/GetPublishedFileDetails endpoint. This endpoint doesn't require an
/// API key, so no Steam credentials are needed just to show mod names/sizes in ArkKeeper.</summary>
public sealed class SteamWorkshopClient
{
    private const string Endpoint = "https://api.steampowered.com/ISteamRemoteStorage/GetPublishedFileDetails/v1/";

    private readonly HttpClient _httpClient;

    public SteamWorkshopClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IReadOnlyList<WorkshopModDetails>> GetModDetailsAsync(
        IReadOnlyCollection<string> publishedFileIds, CancellationToken cancellationToken = default)
    {
        if (publishedFileIds.Count == 0)
        {
            return Array.Empty<WorkshopModDetails>();
        }

        var form = new List<KeyValuePair<string, string>> { new("itemcount", publishedFileIds.Count.ToString()) };
        var index = 0;
        foreach (var id in publishedFileIds)
        {
            form.Add(new($"publishedfileids[{index}]", id));
            index++;
        }

        using var content = new FormUrlEncodedContent(form);
        using var response = await _httpClient.PostAsync(Endpoint, content, cancellationToken);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var envelope = JsonSerializer.Deserialize<ResponseEnvelope>(json)
            ?? throw new InvalidOperationException("Steam Workshop response was empty or malformed.");

        return envelope.Response.PublishedFileDetails.Select(ToModDetails).ToList();
    }

    private static WorkshopModDetails ToModDetails(RawFileDetails raw) => new(
        PublishedFileId: raw.PublishedFileId,
        Found: raw.Result == 1,
        Title: raw.Title,
        FileSizeBytes: long.TryParse(raw.FileSize, out var size) ? size : null,
        TimeUpdatedUtc: raw.TimeUpdated is { } seconds ? DateTimeOffset.FromUnixTimeSeconds(seconds) : null,
        PreviewUrl: raw.PreviewUrl,
        IsBanned: raw.Banned == 1);

    private sealed record ResponseEnvelope(
        [property: JsonPropertyName("response")] ResponseBody Response);

    private sealed record ResponseBody(
        [property: JsonPropertyName("publishedfiledetails")] List<RawFileDetails> PublishedFileDetails);

    private sealed record RawFileDetails(
        [property: JsonPropertyName("publishedfileid")] string PublishedFileId,
        [property: JsonPropertyName("result")] int Result,
        [property: JsonPropertyName("title")] string? Title,
        [property: JsonPropertyName("file_size")] string? FileSize,
        [property: JsonPropertyName("time_updated")] long? TimeUpdated,
        [property: JsonPropertyName("preview_url")] string? PreviewUrl,
        [property: JsonPropertyName("banned")] int Banned);
}
