namespace ArkKeeper.Core.Settings;

/// <summary>The three selectable app themes. Deliberately not just a light/dark bool — OledBlack
/// and Navy are both "dark" in the light/dark-variant sense but need different literal background
/// colors (true black vs. navy), which only the UI layer (<c>ArkKeeper.App.Services.ThemeService</c>)
/// knows how to apply.</summary>
public enum AppThemeKind
{
    Light,
    OledBlack,
    Navy,
}
