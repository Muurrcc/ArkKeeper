using Avalonia.Controls;
using Avalonia.Input;
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

    /// <summary>Lets the user drag the window by the custom title-bar strip, and double-click it
    /// to toggle maximize — neither happens for free once <c>ExtendClientAreaToDecorationsHint</c>
    /// removes the OS title bar.</summary>
    private void OnTitleBarPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
            return;
        }

        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            BeginMoveDrag(e);
        }
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
