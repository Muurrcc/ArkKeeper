using System.Linq;
using ArkKeeper.Core.Settings;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Styling;
using FluentAvalonia.Styling;

namespace ArkKeeper.App.Services;

/// <summary>Wraps FluentAvaloniaTheme so ViewModels can switch theme/accent color without taking
/// a direct dependency on Avalonia.Styling internals.</summary>
public static class ThemeService
{
    // Card/surface tint per theme, layered over the literal window background set in
    // ApplyWindowBackground — Mica's own tinting can't reliably hit true black or navy (it always
    // blends with wallpaper/accent), so OledBlack/Navy turn Mica off and paint both the window and
    // these surface brushes directly instead.
    private static readonly Color OledCardColor = Color.Parse("#141414");
    private static readonly Color NavyCardColor = Color.Parse("#142149");
    private static readonly Color OledWindowColor = Color.Parse("#000000");
    private static readonly Color NavyWindowColor = Color.Parse("#0B1330");

    private static FluentAvaloniaTheme? Theme =>
        Application.Current?.Styles.OfType<FluentAvaloniaTheme>().FirstOrDefault();

    private static Window? MainWindow =>
        (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;

    public static void SetThemeKind(AppThemeKind kind)
    {
        if (Application.Current is not { } app)
        {
            return;
        }

        app.RequestedThemeVariant = kind == AppThemeKind.Light ? ThemeVariant.Light : ThemeVariant.Dark;

        switch (kind)
        {
            case AppThemeKind.OledBlack:
                SetSurfaceBrushes(app, OledCardColor, OledWindowColor);
                ApplyWindowBackground(OledWindowColor);
                break;
            case AppThemeKind.Navy:
                SetSurfaceBrushes(app, NavyCardColor, NavyWindowColor);
                ApplyWindowBackground(NavyWindowColor);
                break;
            default:
                foreach (var key in SurfaceResourceKeys)
                {
                    app.Resources.Remove(key);
                }

                ApplyWindowBackground(null);
                break;
        }
    }

    private static readonly string[] SurfaceResourceKeys =
    [
        "ApplicationPageBackgroundThemeBrush", "SolidBackgroundFillColorBaseBrush",
        // FluentAvalonia's NavigationView paints these as semi-transparent overlays meant to sit
        // over Mica (a subtle depth tint) — with Mica turned off for OledBlack/Navy they were the
        // reason the page still read as a flat neutral grey instead of the literal target color:
        // blending NavigationViewContentBackground's default ~30%-alpha grey over black comes out
        // to almost exactly the grey that was showing. Overridden to the same opaque target color
        // instead of just cleared, so pane and content read as one seamless surface.
        "NavigationViewContentBackground", "NavigationViewDefaultPaneBackground",
        "NavigationViewExpandedPaneBackground",
    ];

    /// <summary>Overrides every resource key that could still be painting an opaque or
    /// semi-transparent background on top of the flat color <see cref="ApplyWindowBackground"/>
    /// puts on the window itself — Window.Background alone wasn't enough, and FluentAvaloniaTheme
    /// swaps in an opaque fallback the moment transparency is turned off (a sensible default for
    /// platforms without Mica, just not what a literal "OLED black"/"Navy" theme wants).</summary>
    private static void SetSurfaceBrushes(Application app, Color cardColor, Color pageColor)
    {
        app.Resources["CardBackgroundFillColorDefaultBrush"] = new SolidColorBrush(cardColor);
        var pageBrush = new SolidColorBrush(pageColor);
        foreach (var key in SurfaceResourceKeys)
        {
            app.Resources[key] = pageBrush;
        }
    }

    /// <summary>Null restores the default Mica-backed look (transparent window, OS-tinted
    /// backdrop); a color switches to a flat, literal background instead — Mica can't be forced to
    /// an exact color, and OledBlack/Navy both need to actually look like the color they're named
    /// after regardless of wallpaper or accent.</summary>
    private static void ApplyWindowBackground(Color? flatColor)
    {
        if (MainWindow is not { } window)
        {
            return;
        }

        if (flatColor is { } color)
        {
            window.TransparencyLevelHint = [WindowTransparencyLevel.None];
            window.Background = new SolidColorBrush(color);
        }
        else
        {
            window.TransparencyLevelHint = [WindowTransparencyLevel.Mica];
            window.Background = Brushes.Transparent;
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
