using ArkKeeper.App.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArkKeeper.App.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    public SettingsViewModel()
    {
        _isDarkTheme = ThemeService.IsDark;
    }

    public IReadOnlyList<AccentSwatch> AccentSwatches => AccentSwatch.Presets;

    [ObservableProperty]
    private bool _isDarkTheme;

    partial void OnIsDarkThemeChanged(bool value) => ThemeService.SetDark(value);

    [RelayCommand]
    private void ApplyAccent(AccentSwatch swatch) => ThemeService.SetAccentColor(swatch.Color);
}
