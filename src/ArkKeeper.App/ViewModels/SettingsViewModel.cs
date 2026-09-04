using System.Net.Http;
using System.Reflection;
using ArkKeeper.App.Services;
using ArkKeeper.Core.Settings;
using ArkKeeper.Discord;
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

    public string CurrentVersion { get; } =
        (Assembly.GetExecutingAssembly().GetName().Version ?? new Version(1, 0, 0, 0)).ToString();

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
