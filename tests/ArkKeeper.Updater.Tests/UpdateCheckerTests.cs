namespace ArkKeeper.Updater.Tests;

public class UpdateCheckerTests
{
    [Fact]
    public async Task CheckAsync_WhenManifestVersionIsNewer_ReportsUpdateAvailable()
    {
        var handler = new FakeHttpMessageHandler("""{"latestVersion":"1.2.0","downloadUrl":"https://example.com/setup.exe"}""");
        var checker = new UpdateChecker(new HttpClient(handler), "https://example.com/manifest.json");

        var result = await checker.CheckAsync(new Version(1, 1, 0));

        Assert.True(result.IsUpdateAvailable);
        Assert.Equal(new Version(1, 2, 0), result.LatestVersion);
        Assert.Equal("https://example.com/setup.exe", result.DownloadUrl);
    }

    [Fact]
    public async Task CheckAsync_WhenManifestVersionIsSameOrOlder_ReportsNoUpdate()
    {
        var handler = new FakeHttpMessageHandler("""{"latestVersion":"1.1.0","downloadUrl":"https://example.com/setup.exe"}""");
        var checker = new UpdateChecker(new HttpClient(handler), "https://example.com/manifest.json");

        var result = await checker.CheckAsync(new Version(1, 1, 0));

        Assert.False(result.IsUpdateAvailable);
    }
}
