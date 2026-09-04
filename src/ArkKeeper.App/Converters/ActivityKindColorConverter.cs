using System.Globalization;
using ArkKeeper.App.Services;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace ArkKeeper.App.Converters;

/// <summary>Maps an <see cref="ActivityKind"/> to the dot color shown next to it in the
/// Dashboard's Activity feed.</summary>
public sealed class ActivityKindColorConverter : IValueConverter
{
    public static readonly ActivityKindColorConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value switch
    {
        ActivityKind.Backup => Color.Parse("#22C55E"),
        _ => Color.Parse("#0FC2C0"),
    };

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
