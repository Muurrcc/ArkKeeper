using Avalonia.Controls;

namespace ArkKeeper.App.Views;

public partial class ModsView : UserControl
{
    public ModsView()
    {
        InitializeComponent();
    }

    /// <summary>Keeps the download log scrolled to the newest line as it grows.</summary>
    private void OnProgressLogSizeChanged(object? sender, Avalonia.Controls.SizeChangedEventArgs e) =>
        ProgressLogScroll.ScrollToEnd();
}
