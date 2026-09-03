namespace ArkKeeper.Updater.Tests;

public class UpdateDownloaderTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "ArkKeeperUpdateDownloaderTests_" + Guid.NewGuid());

    [Fact]
    public async Task DownloadAsync_SavesTheResponseBodyToDisk()
    {
        var handler = new FakeHttpMessageHandler("fake installer bytes");
        var downloader = new UpdateDownloader(new HttpClient(handler));
        var update = new UpdateCheckResult(true, new Version(2, 0, 0), "https://example.com/downloads/ArkKeeperSetup.exe");

        var path = await downloader.DownloadAsync(update, _directory);

        Assert.True(File.Exists(path));
        Assert.Equal("fake installer bytes", await File.ReadAllTextAsync(path));
    }

    [Fact]
    public async Task DownloadAsync_UsesTheFileNameFromTheDownloadUrl()
    {
        var handler = new FakeHttpMessageHandler("data");
        var downloader = new UpdateDownloader(new HttpClient(handler));
        var update = new UpdateCheckResult(true, new Version(2, 0, 0), "https://example.com/downloads/ArkKeeperSetup.exe");

        var path = await downloader.DownloadAsync(update, _directory);

        Assert.Equal("ArkKeeperSetup.exe", Path.GetFileName(path));
    }

    [Fact]
    public async Task DownloadAsync_WithNoFileNameInUrl_FallsBackToAVersionedName()
    {
        var handler = new FakeHttpMessageHandler("data");
        var downloader = new UpdateDownloader(new HttpClient(handler));
        var update = new UpdateCheckResult(true, new Version(2, 1, 0), "https://example.com/downloads/");

        var path = await downloader.DownloadAsync(update, _directory);

        Assert.Equal("ArkKeeper-2.1.0.exe", Path.GetFileName(path));
    }

    [Fact]
    public async Task DownloadAsync_CreatesTheDestinationDirectoryIfMissing()
    {
        var handler = new FakeHttpMessageHandler("data");
        var downloader = new UpdateDownloader(new HttpClient(handler));
        var update = new UpdateCheckResult(true, new Version(2, 0, 0), "https://example.com/setup.exe");

        await downloader.DownloadAsync(update, _directory);

        Assert.True(Directory.Exists(_directory));
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }
}
