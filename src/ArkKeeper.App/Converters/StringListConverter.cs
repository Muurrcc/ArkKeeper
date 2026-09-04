using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia.Data.Converters;

namespace ArkKeeper.App.Converters;

/// <summary>Round-trips a <see cref="List{T}"/> of strings (one of ArkKeeper's "advanced
/// override list" profile settings, each entry a raw ini value like
/// <c>(ClassName="Rex_Character_BP_C",Multiplier=1.5)</c>) to/from a multi-line TextBox, one
/// entry per line.</summary>
public sealed class StringListConverter : IValueConverter
{
    public static readonly StringListConverter Instance = new();

    public object? Convert(object? value, System.Type targetType, object? parameter, CultureInfo culture) =>
        value is List<string> list ? string.Join('\n', list) : string.Empty;

    public object? ConvertBack(object? value, System.Type targetType, object? parameter, CultureInfo culture) =>
        (value as string ?? string.Empty)
            .Split('\n')
            .Select(line => line.Trim())
            .Where(line => line.Length > 0)
            .ToList();
}
