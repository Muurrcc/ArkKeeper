using System.Globalization;
using Avalonia.Data.Converters;

namespace ArkKeeper.App.Converters;

public sealed class BoolToNewOrEditTitleConverter : IValueConverter
{
    public static readonly BoolToNewOrEditTitleConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true ? "New server" : "Edit server";

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
