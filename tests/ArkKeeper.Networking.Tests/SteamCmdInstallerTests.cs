using System.IO.Compression;
using System.Net;
using ArkKeeper.Networking.SteamCmd;
using Xunit;

namespace ArkKeeper.Networking.Tests;

public class SteamCmdInstallerTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "ArkKeeperSteamCmdTests_" + Guid.NewGuid());

    [Fact]
    public async Task EnsureInstalledAsync_WhenAlreadyPresent_SkipsDownload()
    {
        Directory.CreateDirectory(_directory);
        var exePath = Path.Combine(_directory, "steamcmd.exe");
        await File.WriteAllTextAsync(exePath, "already here");

        var handler = new ThrowingHttpMessageHandler();
        var installer = new SteamCmdInstaller(new HttpClient(handler));

        var result = await installer.EnsureInstalledAsync(_directory);

        Assert.Equal(exePath, result);
    }

    [Fact]
    public async Task EnsureInstalledAsync_WhenMissing_DownloadsAndExtractsZip()
    {
        var zipBytes = BuildFakeSteamCmdZip();
        var handler = new ZipHttpMessageHandler(zipBytes);
        var installer = new SteamCmdInstaller(new HttpClient(handler));

        var result = await installer.EnsureInstalledAsync(_directory);

        Assert.True(File.Exists(result));
        Assert.Equal("fake steamcmd binary", await File.ReadAllTextAsync(result));
        Assert.False(File.Exists(Path.Combine(_directory, "steamcmd.zip")), "the downloaded zip should be cleaned up");
    }

    private static byte[] BuildFakeSteamCmdZip()
    {
        using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            var entry = archive.CreateEntry("steamcmd.exe");
            using var writer = new StreamWriter(entry.Open());
            writer.Write("fake steamcmd binary");
        }
        return stream.ToArray();
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    private sealed class ZipHttpMessageHandler : HttpMessageHandler
    {
        private readonly byte[] _zipBytes;

        public ZipHttpMessageHandler(byte[] zipBytes) => _zipBytes = zipBytes;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(_zipBytes) });
    }

    private sealed class ThrowingHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("No HTTP call should happen when steamcmd.exe already exists.");
    }
}
