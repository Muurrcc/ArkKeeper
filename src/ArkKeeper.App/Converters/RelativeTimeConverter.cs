using System.Globalization;
using Avalonia.Data.Converters;

namespace ArkKeeper.App.Converters;

/// <summary>Formats a DateTimeOffset as "just now" / "N minutes ago" / etc. for the Dashboard's
/// activity feed — re-evaluated each time the bound value changes rather than needing its own
/// timer, since the feed's own list changes often enough (every server start/stop/backup) to keep
/// timestamps reasonably fresh on screen.</summary>
public sealed class RelativeTimeConverter : IValueConverter
{
    public static readonly RelativeTimeConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not DateTimeOffset timestamp)
        {
            return string.Empty;
        }

        var elapsed = DateTimeOffset.Now - timestamp;
        if (elapsed < TimeSpan.FromSeconds(45))
        {
            return "just now";
        }

        if (elapsed < TimeSpan.FromMinutes(60))
        {
            var minutes = Math.Max(1, (int)elapsed.TotalMinutes);
            return $"{minutes} minute{(minutes == 1 ? "" : "s")} ago";
        }

        if (elapsed < TimeSpan.FromHours(24))
        {
            var hours = (int)elapsed.TotalHours;
            return $"{hours} hour{(hours == 1 ? "" : "s")} ago";
        }

        var days = (int)elapsed.TotalDays;
        return $"{days} day{(days == 1 ? "" : "s")} ago";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
