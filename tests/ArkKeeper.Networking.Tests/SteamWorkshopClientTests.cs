using ArkKeeper.Networking.Workshop;
using Xunit;

namespace ArkKeeper.Networking.Tests;

public class SteamWorkshopClientTests
{
    // Real response captured from ISteamRemoteStorage/GetPublishedFileDetails for a known
    // ARK mod (Structures Plus, id 731604991), trimmed to the fields we actually read.
    private const string FoundResponse = """
        {"response":{"result":1,"resultcount":1,"publishedfiledetails":[{"publishedfileid":"731604991","result":1,"creator":"76561198023306710","file_size":"60998180","preview_url":"https://images.steamusercontent.com/ugc/2020468497848072692/52F830CE07455A0C1EE4F1676163301D97BD5F1B/","title":"Structures Plus (S+)","time_created":1469500830,"time_updated":1680660170,"banned":0,"ban_reason":""}]}}
        """;

    private const string NotFoundResponse = """
        {"response":{"result":1,"resultcount":1,"publishedfiledetails":[{"publishedfileid":"1","result":9}]}}
        """;

    [Fact]
    public async Task GetModDetailsAsync_OnKnownMod_ParsesTitleSizeAndTimestamp()
    {
        var handler = new FakeHttpMessageHandler(FoundResponse);
        var client = new SteamWorkshopClient(new HttpClient(handler));

        var details = await client.GetModDetailsAsync(new[] { "731604991" });

        var mod = Assert.Single(details);
        Assert.True(mod.Found);
        Assert.Equal("Structures Plus (S+)", mod.Title);
        Assert.Equal(60998180, mod.FileSizeBytes);
        Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(1680660170), mod.TimeUpdatedUtc);
        Assert.False(mod.IsBanned);
    }

    [Fact]
    public async Task GetModDetailsAsync_OnUnknownMod_ReturnsNotFound()
    {
        var handler = new FakeHttpMessageHandler(NotFoundResponse);
        var client = new SteamWorkshopClient(new HttpClient(handler));

        var details = await client.GetModDetailsAsync(new[] { "1" });

        var mod = Assert.Single(details);
        Assert.False(mod.Found);
        Assert.Null(mod.Title);
    }

    [Fact]
    public async Task GetModDetailsAsync_SendsItemCountAndIndexedIds()
    {
        var handler = new FakeHttpMessageHandler(FoundResponse);
        var client = new SteamWorkshopClient(new HttpClient(handler));

        await client.GetModDetailsAsync(new[] { "111", "222" });

        Assert.Contains("itemcount=2", handler.LastRequestBody);
        Assert.Contains("publishedfileids%5B0%5D=111", handler.LastRequestBody);
        Assert.Contains("publishedfileids%5B1%5D=222", handler.LastRequestBody);
    }

    [Fact]
    public async Task GetModDetailsAsync_WithNoIds_ReturnsEmptyWithoutCallingHttp()
    {
        var handler = new FakeHttpMessageHandler(FoundResponse);
        var client = new SteamWorkshopClient(new HttpClient(handler));

        var details = await client.GetModDetailsAsync(Array.Empty<string>());

        Assert.Empty(details);
        Assert.Null(handler.LastRequest);
    }
}
