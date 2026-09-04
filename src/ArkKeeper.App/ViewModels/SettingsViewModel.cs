using System.Net.Http;
using System.Reflection;
using ArkKeeper.App.Services;
using ArkKeeper.Core.Settings;
using ArkKeeper.Discord;
using ArkKeeper.Networking.SteamCmd;
using ArkKeeper.Updater;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArkKeeper.App.ViewModels;

/// <summary>App-level settings — theme/accent (now actually persisted; previously reset to
/// defaults on every launch since nothing wrote <see cref="AppSettingsStore"/> at all) plus the
/// default install directory, SteamCMD directory, Discord webhook, and update-manifest fields.</summary>
public partial class SettingsViewModel : ViewModelBase
{
    private readonly AppSettingsStore _store;
    private AppSettings _settings = new();
    private UpdateCheckResult? _lastCheckResult;

    public SettingsViewModel(AppSettingsStore store)
    {
        _store = store;
    }

    public IReadOnlyList<AccentSwatch> AccentSwatches => AccentSwatch.Presets;

    public IReadOnlyList<AppThemeKind> ThemeKinds { get; } = Enum.GetValues<AppThemeKind>();

    [ObservableProperty]
    public partial AppThemeKind ThemeKind { get; set; } = AppThemeKind.Navy;

    [ObservableProperty]
    public partial string DefaultInstallDirectory { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string SteamCmdDirectory { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? SteamCmdStatusMessage { get; set; }

    [ObservableProperty]
    public partial string? SteamCmdErrorMessage { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(InstallSteamCmdCommand))]
    public partial bool IsInstallingSteamCmd { get; set; }

    [ObservableProperty]
    public partial string DiscordWebhookUrl { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string UpdateManifestUrl { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? StatusMessage { get; set; }

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    [ObservableProperty]
    public partial string? UpdateStatusMessage { get; set; }

    [ObservableProperty]
    public partial string? UpdateErrorMessage { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DownloadUpdateCommand))]
    public partial bool IsUpdateAvailable { get; set; }

    public string CurrentVersion { get; } = FormatVersion(Assembly.GetExecutingAssembly().GetName().Version);

    /// <summary>Newest-first version history — see <see cref="Changelog.Entries"/> for how to add
    /// to this when a new version ships.</summary>
    public IReadOnlyList<ChangelogEntry> ChangelogEntries => Changelog.Entries;

    /// <summary>.NET assembly versions are always 4-part (Major.Minor.Build.Revision); trimmed to
    /// the 3-part "1.0.0" shape a user actually expects to see, since the csproj's own
    /// &lt;Version&gt; never sets a meaningful revision.</summary>
    private static string FormatVersion(Version? version) =>
        version is null ? "1.0.0" : $"{version.Major}.{version.Minor}.{version.Build}";

    /// <summary>Loads settings and applies theme/accent immediately. Called from
    /// <see cref="MainViewModel.InitializeAsync"/> before profiles load, so a configured Discord
    /// webhook is ready by the time any server gets tracked.</summary>
    public async Task InitializeAsync()
    {
        _settings = await _store.LoadAsync();

        ThemeKind = _settings.ThemeKind;
        DefaultInstallDirectory = _settings.DefaultInstallDirectory;
        SteamCmdDirectory = _settings.SteamCmdDirectory;
        DiscordWebhookUrl = _settings.DiscordWebhookUrl ?? string.Empty;
        UpdateManifestUrl = _settings.UpdateManifestUrl ?? string.Empty;

        ThemeService.SetThemeKind(ThemeKind);
        if (Avalonia.Media.Color.TryParse(_settings.AccentColorHex, out var color))
        {
            ThemeService.SetAccentColor(color);
        }
    }

    partial void OnThemeKindChanged(AppThemeKind value)
    {
        ThemeService.SetThemeKind(value);
        _ = SaveAsync();
    }

    [RelayCommand]
    private void ApplyAccent(AccentSwatch swatch)
    {
        ThemeService.SetAccentColor(swatch.Color);
        _settings.AccentColorHex = swatch.Color.ToString();
        _ = SaveAsync();
    }

    [RelayCommand]
    private Task SaveAsync()
    {
        _settings.ThemeKind = ThemeKind;
        _settings.DefaultInstallDirectory = DefaultInstallDirectory;
        _settings.SteamCmdDirectory = SteamCmdDirectory;
        _settings.DiscordWebhookUrl = string.IsNullOrWhiteSpace(DiscordWebhookUrl) ? null : DiscordWebhookUrl;
        _settings.UpdateManifestUrl = string.IsNullOrWhiteSpace(UpdateManifestUrl) ? null : UpdateManifestUrl;
        return _store.SaveAsync(_settings);
    }

    /// <summary>Installs steamcmd.exe into <see cref="SteamCmdDirectory"/> on its own — previously
    /// the only way to get steamcmd onto disk was as a side effect of installing a game server or
    /// downloading a mod, with no way to just set it up ahead of time from Settings.</summary>
    [RelayCommand(CanExecute = nameof(CanInstallSteamCmd))]
    private async Task InstallSteamCmdAsync()
    {
        SteamCmdErrorMessage = null;
        SteamCmdStatusMessage = null;

        if (string.IsNullOrWhiteSpace(SteamCmdDirectory))
        {
            SteamCmdErrorMessage = "Choose a folder first.";
            return;
        }

        IsInstallingSteamCmd = true;
        try
        {
            using var httpClient = new HttpClient();
            var installer = new SteamCmdInstaller(httpClient);
            var path = await installer.EnsureInstalledAsync(SteamCmdDirectory);
            SteamCmdStatusMessage = $"steamcmd.exe ready at {path}.";
        }
        catch (Exception ex)
        {
            SteamCmdErrorMessage = $"Couldn't install SteamCMD: {ex.Message}";
        }
        finally
        {
            IsInstallingSteamCmd = false;
        }
    }

    private bool CanInstallSteamCmd() => !IsInstallingSteamCmd;

    [RelayCommand]
    private async Task SendTestDiscordMessageAsync()
    {
        ErrorMessage = null;
        StatusMessage = null;

        if (string.IsNullOrWhiteSpace(DiscordWebhookUrl))
        {
            ErrorMessage = "Enter a Discord webhook URL first.";
            return;
        }

        try
        {
            using var httpClient = new HttpClient();
            var notifier = new DiscordWebhookNotifier(httpClient, DiscordWebhookUrl);
            await notifier.SendAsync("ArkKeeper: this is a test notification.");
            StatusMessage = "Test message sent.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Couldn't send: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task CheckForUpdatesAsync()
    {
        UpdateErrorMessage = null;
        UpdateStatusMessage = null;
        IsUpdateAvailable = false;
        _lastCheckResult = null;

        if (string.IsNullOrWhiteSpace(UpdateManifestUrl))
        {
            UpdateErrorMessage = "Enter an update manifest URL first.";
            return;
        }

        try
        {
            using var httpClient = new HttpClient();
            var checker = new UpdateChecker(httpClient, UpdateManifestUrl);
            var result = await checker.CheckAsync(Version.Parse(CurrentVersion));

            if (result.IsUpdateAvailable)
            {
                _lastCheckResult = result;
                IsUpdateAvailable = true;
                UpdateStatusMessage = $"Update available: {result.LatestVersion} (currently {CurrentVersion}).";
            }
            else
            {
                UpdateStatusMessage = $"Up to date (currently {CurrentVersion}).";
            }
        }
        catch (Exception ex)
        {
            UpdateErrorMessage = $"Couldn't check for updates: {ex.Message}";
        }
    }

    [RelayCommand(CanExecute = nameof(CanDownloadUpdate))]
    private async Task DownloadUpdateAsync()
    {
        UpdateErrorMessage = null;

        if (_lastCheckResult is not { } update)
        {
            return;
        }

        try
        {
            using var httpClient = new HttpClient();
            var downloader = new UpdateDownloader(httpClient);
            var destinationDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "ArkKeeper", "Updates");
            var downloadedPath = await downloader.DownloadAsync(update, destinationDirectory);
            UpdateStatusMessage = $"Downloaded to {downloadedPath}. Close ArkKeeper and run it to update.";
        }
        catch (Exception ex)
        {
            UpdateErrorMessage = $"Couldn't download the update: {ex.Message}";
        }
    }

    private bool CanDownloadUpdate() => IsUpdateAvailable;
}
