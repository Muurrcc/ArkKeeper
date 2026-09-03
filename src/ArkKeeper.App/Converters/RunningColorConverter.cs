using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace ArkKeeper.App.Converters;

public sealed class RunningColorConverter : IValueConverter
{
    public static readonly RunningColorConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true ? Colors.LimeGreen : Colors.Gray;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
