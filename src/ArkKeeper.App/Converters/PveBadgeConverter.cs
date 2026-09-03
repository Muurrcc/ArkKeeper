using System.Globalization;
using Avalonia.Data.Converters;

namespace ArkKeeper.App.Converters;

public sealed class PveBadgeConverter : IValueConverter
{
    public static readonly PveBadgeConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true ? "PvE" : "PvP";

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
