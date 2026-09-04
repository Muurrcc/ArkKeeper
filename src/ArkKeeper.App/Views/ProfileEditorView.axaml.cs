using Avalonia.Controls;
using Avalonia.Platform.Storage;
using ArkKeeper.App.ViewModels;

namespace ArkKeeper.App.Views;

public partial class ProfileEditorView : UserControl
{
    public ProfileEditorView()
    {
        InitializeComponent();
    }

    private async void OnBrowseInstallDirectoryClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is not ProfileEditorViewModel viewModel)
        {
            return;
        }

        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null)
        {
            return;
        }

        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select the server install directory",
            AllowMultiple = false,
        });

        if (folders.Count > 0 && folders[0].TryGetLocalPath() is { } path)
        {
            viewModel.Profile.InstallDirectory = path;
        }
    }

    /// <summary>Keeps the install log scrolled to the newest line as it grows — without this the
    /// user has to manually scroll down every time a new line arrives during a long install.</summary>
    private void OnInstallLogSizeChanged(object? sender, Avalonia.Controls.SizeChangedEventArgs e) =>
        InstallLogScroll.ScrollToEnd();
}
