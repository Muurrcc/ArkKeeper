using Avalonia.Controls;
using Avalonia.Input;
using ArkKeeper.App.ViewModels;

namespace ArkKeeper.App.Views;

public partial class RconConsoleView : UserControl
{
    public RconConsoleView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is RconConsoleViewModel viewModel)
        {
            viewModel.Log.CollectionChanged += (_, _) => LogScrollViewer.ScrollToEnd();
        }
    }

    private void OnCommandTextKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter || DataContext is not RconConsoleViewModel viewModel)
        {
            return;
        }

        if (viewModel.SendCommand.CanExecute(null))
        {
            viewModel.SendCommand.Execute(null);
        }

        e.Handled = true;
    }
}
