using System.Globalization;
using ArkKeeper.Core.Settings;
using Avalonia.Data.Converters;

namespace ArkKeeper.App.Converters;

public sealed class ThemeKindDisplayConverter : IValueConverter
{
    public static readonly ThemeKindDisplayConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value switch
    {
        AppThemeKind.Light => "Light",
        AppThemeKind.OledBlack => "OLED Black",
        AppThemeKind.Navy => "Navy Blue",
        _ => value?.ToString(),
    };

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
