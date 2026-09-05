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
        Assert.Equal(AppThemeKind.Navy, settings.ThemeKind);
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
            ThemeKind = AppThemeKind.OledBlack,
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

    [Fact]
    public async Task SaveAsync_CalledConcurrently_NeverThrows()
    {
        // Same shape as the ProfileStore race: File.Create defaults to FileShare.None, and
        // SettingsViewModel fires SaveAsync as fire-and-forget ("_ = SaveAsync();") from every
        // settings property setter — two settings changed in quick succession (e.g. toggling a
        // switch right after picking a theme) can each open this same file for exclusive write
        // at once.
        var store = new AppSettingsStore(_filePath);

        await Task.WhenAll(Enumerable.Range(0, 10).Select(_ => store.SaveAsync(new AppSettings())))
            .WaitAsync(TimeSpan.FromSeconds(10));

        Assert.True(File.Exists(_filePath));
    }

    [Fact]
    public async Task LoadAsync_WhenFileIsCorrupt_ReturnsDefaultsRatherThanThrowing()
    {
        // Same reasoning as ProfileStore's corrupt-file test: this is loaded from
        // MainViewModel.InitializeAsync via MainWindow's "async void OnOpened" with no try/catch
        // and no global handler — a truncated settings.json (crash, disk full, ...) would
        // otherwise crash the whole app before the window is even usable, before the user could
        // ever reach Settings to fix or delete the file themselves.
        Directory.CreateDirectory(_directory);
        await File.WriteAllTextAsync(_filePath, "{ not valid json");
        var store = new AppSettingsStore(_filePath);

        var settings = await store.LoadAsync();

        Assert.Equal(AppThemeKind.Navy, settings.ThemeKind);
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }
}
