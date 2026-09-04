using System.Net.Http;
using ArkKeeper.App.Services;
using ArkKeeper.Core.Settings;
using ArkKeeper.Discord;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArkKeeper.App.ViewModels;

/// <summary>App-level settings — theme/accent (now actually persisted; previously reset to
/// defaults on every launch since nothing wrote <see cref="AppSettingsStore"/> at all) plus the
/// new default install directory, SteamCMD directory, and Discord webhook fields.</summary>
public partial class SettingsViewModel : ViewModelBase
{
    private readonly AppSettingsStore _store;
    private AppSettings _settings = new();

    public SettingsViewModel(AppSettingsStore store)
    {
        _store = store;
    }

    public IReadOnlyList<AccentSwatch> AccentSwatches => AccentSwatch.Presets;

    [ObservableProperty]
    public partial bool IsDarkTheme { get; set; } = true;

    [ObservableProperty]
    public partial string DefaultInstallDirectory { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string SteamCmdDirectory { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string DiscordWebhookUrl { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? StatusMessage { get; set; }

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    /// <summary>Loads settings and applies theme/accent immediately. Called from
    /// <see cref="MainViewModel.InitializeAsync"/> before profiles load, so a configured Discord
    /// webhook is ready by the time any server gets tracked.</summary>
    public async Task InitializeAsync()
    {
        _settings = await _store.LoadAsync();

        IsDarkTheme = _settings.DarkTheme;
        DefaultInstallDirectory = _settings.DefaultInstallDirectory;
        SteamCmdDirectory = _settings.SteamCmdDirectory;
        DiscordWebhookUrl = _settings.DiscordWebhookUrl ?? string.Empty;

        ThemeService.SetDark(IsDarkTheme);
        if (Avalonia.Media.Color.TryParse(_settings.AccentColorHex, out var color))
        {
            ThemeService.SetAccentColor(color);
        }
    }

    partial void OnIsDarkThemeChanged(bool value)
    {
        ThemeService.SetDark(value);
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
        _settings.DarkTheme = IsDarkTheme;
        _settings.DefaultInstallDirectory = DefaultInstallDirectory;
        _settings.SteamCmdDirectory = SteamCmdDirectory;
        _settings.DiscordWebhookUrl = string.IsNullOrWhiteSpace(DiscordWebhookUrl) ? null : DiscordWebhookUrl;
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
}
