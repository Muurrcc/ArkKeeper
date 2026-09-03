using System.Globalization;
using Avalonia.Data.Converters;

namespace ArkKeeper.App.Converters;

/// <summary>True when a bound string is non-null/non-empty — used to show an error message
/// block only while there's actually an error to show.</summary>
public sealed class StringNotEmptyConverter : IValueConverter
{
    public static readonly StringNotEmptyConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        !string.IsNullOrEmpty(value as string);

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
