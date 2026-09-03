using Avalonia.Controls;
using ArkKeeper.App.ViewModels;
using FluentAvalonia.UI.Controls;

namespace ArkKeeper.App.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Opened += OnOpened;
    }

    private async void OnOpened(object? sender, EventArgs e)
    {
        if (DataContext is MainViewModel viewModel)
        {
            await viewModel.InitializeAsync();
        }
    }

    private void OnNavSelectionChanged(object? sender, FANavigationViewSelectionChangedEventArgs e)
    {
        if (DataContext is not MainViewModel viewModel)
        {
            return;
        }

        if (e.IsSettingsSelected)
        {
            viewModel.SelectedPage = viewModel.SettingsPage;
            return;
        }

        if (e.SelectedItemContainer is Control { Tag: ViewModelBase page })
        {
            viewModel.SelectedPage = page;
        }
    }
}
