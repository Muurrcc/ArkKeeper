using Avalonia.Controls;
using Avalonia.Platform.Storage;
using ArkKeeper.App.ViewModels;

namespace ArkKeeper.App.Views;

public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();
    }

    private async void OnBrowseDefaultInstallDirectoryClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (await PickFolderAsync() is { } path && DataContext is SettingsViewModel viewModel)
        {
            viewModel.DefaultInstallDirectory = path;
        }
    }

    private async void OnBrowseSteamCmdDirectoryClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (await PickFolderAsync() is { } path && DataContext is SettingsViewModel viewModel)
        {
            viewModel.SteamCmdDirectory = path;
        }
    }

    private async Task<string?> PickFolderAsync()
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null)
        {
            return null;
        }

        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            AllowMultiple = false,
        });

        return folders.Count > 0 ? folders[0].TryGetLocalPath() : null;
    }
}
