using ArkKeeper.Core.Settings;
using Xunit;

namespace ArkKeeper.Core.Tests;

public class AppSettingsStoreTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "ArkKeeperAppSettingsTests_" + Guid.NewGuid());
    private readonly string _filePath;

    public AppSettingsStoreTests() => _filePath = Path.Combine(_directory, "settings.json");

    [Fact]
    public async Task LoadAsync_WhenFileDoesNotExist_ReturnsDefaults()
    {
        var store = new AppSettingsStore(_filePath);

        var settings = await store.LoadAsync();

        Assert.Equal(string.Empty, settings.DefaultInstallDirectory);
        Assert.True(settings.DarkTheme);
    }

    [Fact]
    public async Task SaveAndLoad_RoundTripsAllFields()
    {
        var store = new AppSettingsStore(_filePath);
        var settings = new AppSettings
        {
            DefaultInstallDirectory = @"C:\Servers",
            SteamCmdDirectory = @"C:\SteamCmd",
            DiscordWebhookUrl = "https://discord.com/api/webhooks/1/a",
            DarkTheme = false,
            AccentColorHex = "#FF00FF",
        };

        await store.SaveAsync(settings);
        var loaded = await store.LoadAsync();

        Assert.Equivalent(settings, loaded);
    }

    [Fact]
    public async Task SaveAsync_CreatesTheDirectoryIfMissing()
    {
        var store = new AppSettingsStore(_filePath);

        await store.SaveAsync(new AppSettings());

        Assert.True(File.Exists(_filePath));
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }
}
