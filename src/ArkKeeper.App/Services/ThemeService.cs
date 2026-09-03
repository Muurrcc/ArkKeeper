using System.Linq;
using Avalonia;
using Avalonia.Media;
using Avalonia.Styling;
using FluentAvalonia.Styling;

namespace ArkKeeper.App.Services;

/// <summary>Wraps FluentAvaloniaTheme so ViewModels can toggle light/dark and the accent color
/// without taking a direct dependency on Avalonia.Styling internals.</summary>
public static class ThemeService
{
    private static FluentAvaloniaTheme? Theme =>
        Application.Current?.Styles.OfType<FluentAvaloniaTheme>().FirstOrDefault();

    public static bool IsDark => Application.Current?.ActualThemeVariant == ThemeVariant.Dark;

    public static void SetDark(bool isDark)
    {
        if (Application.Current is { } app)
        {
            app.RequestedThemeVariant = isDark ? ThemeVariant.Dark : ThemeVariant.Light;
        }
    }

    public static void SetAccentColor(Color color)
    {
        if (Theme is { } theme)
        {
            theme.CustomAccentColor = color;
        }
    }
}
